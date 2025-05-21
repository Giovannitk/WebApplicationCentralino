﻿using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using WebApplicationCentralino.Models;
using WebApplicationCentralino.Services;
using Microsoft.Extensions.Logging;
using System.Linq;
using Microsoft.Extensions.Caching.Memory;

namespace WebApplicationCentralino.Services
{
    /// <summary>
    /// Servizio per la gestione delle chiamate e delle relative statistiche
    /// </summary>
    public class ChiamataService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ContattoService _contattoService;
        private readonly ILogger<ChiamataService> _logger;
        private readonly IMemoryCache _cache;
        // Chiave utilizzata per memorizzare i contatti incompleti nella cache
        private const string INCOMPLETE_CONTACTS_CACHE_KEY = "IncompleteContacts";

        public ChiamataService(
            HttpClient httpClient, 
            IConfiguration configuration, 
            ContattoService contattoService, 
            ILogger<ChiamataService> logger,
            IMemoryCache cache)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _contattoService = contattoService;
            _httpClient.BaseAddress = new Uri(_configuration["ApiSettings:BaseUrl"]);
            _logger = logger;
            _cache = cache;
        }

        /// <summary>
        /// Recupera tutte le chiamate dal servizio API
        /// </summary>
        public async Task<List<Chiamata>> GetAllChiamateAsync()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<Chiamata>>("api/Call/get-all-calls");
                return response ?? new List<Chiamata>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il recupero delle chiamate");
                return new List<Chiamata>();
            }
        }

        /// <summary>
        /// Recupera le chiamate filtrate in base a data e durata minima
        /// </summary>
        public async Task<List<Chiamata>> GetFilteredChiamateAsync(DateTime? fromDate = null, DateTime? toDate = null, double minDuration = 5)
        {
            try
            {
                var chiamate = await GetAllChiamateAsync();
                var contatti = await _contattoService.GetAllAsync();

                // Ottieni tutti i numeri che sono interni (Interno != Entrata)
                var numeriInterni = contatti
                    .Where(c => c.Interno != 0)
                    .Select(c => c.NumeroContatto)
                    .ToHashSet();

                // Filtra le chiamate combinando tutti i criteri
                var chiamateFiltrate = chiamate
                    .Where(c =>
                        (fromDate == null || c.DataArrivoChiamata >= fromDate) &&
                        (toDate == null || c.DataArrivoChiamata <= toDate) &&
                        (c.Durata.TotalSeconds >= minDuration) &&
                        // Escludiamo solo le chiamate dove entrambi i numeri sono interni
                        !(numeriInterni.Contains(c.NumeroChiamante) && numeriInterni.Contains(c.NumeroChiamato))
                    )
                    .ToList();

                return chiamateFiltrate;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il filtraggio delle chiamate");
                return new List<Chiamata>();
            }
        }

        /// <summary>
        /// Calcola le statistiche delle chiamate e dei contatti
        /// </summary>
        public async Task<CallStatistics> GetCallStatisticsAsync()
        {
            try
            {
                var chiamate = await GetAllChiamateAsync();
                var contatti = await _contattoService.GetAllAsync();
                var oggi = DateTime.Today;
                var inizioSettimana = oggi.AddDays(-(int)oggi.DayOfWeek);

                // Ottieni tutti i numeri che hanno fatto o ricevuto chiamate nell'ultima settimana
                var numeriAttivi = chiamate
                    .Where(c => c.DataArrivoChiamata >= inizioSettimana)
                    .SelectMany(c => new[] { c.NumeroChiamante, c.NumeroChiamato })
                    .Where(n => !string.IsNullOrEmpty(n))
                    .Distinct()
                    .ToHashSet();

                // Calcola i contatti incompleti e salvali in cache
                var contattiIncompleti = contatti.Where(c => 
                    string.IsNullOrEmpty(c.RagioneSociale) || 
                    c.RagioneSociale == "Non registrato" ||
                    c.RagioneSociale == c.NumeroContatto ||
                    string.IsNullOrEmpty(c.NumeroContatto)
                ).ToList();

                // Salva i contatti incompleti in cache per 1 ora
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromHours(1));
                _cache.Set(INCOMPLETE_CONTACTS_CACHE_KEY, contattiIncompleti, cacheOptions);

                var statistiche = new CallStatistics
                {
                    ChiamateOggi = chiamate.Count(c => c.DataArrivoChiamata.Date == oggi),
                    ChiamateSettimana = chiamate.Count(c => c.DataArrivoChiamata >= inizioSettimana),
                    ContattiAttivi = numeriAttivi.Count,
                    ContattiInattivi = contatti.Count - numeriAttivi.Count,
                    ContattiIncompleti = contattiIncompleti.Count
                };

                return statistiche;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il recupero delle statistiche");
                return new CallStatistics();
            }
        }

        /// <summary>
        /// Recupera la lista dei contatti incompleti dalla cache
        /// </summary>
        public List<Contatto> GetIncompleteContacts()
        {
            if (_cache.TryGetValue(INCOMPLETE_CONTACTS_CACHE_KEY, out List<Contatto> contattiIncompleti))
            {
                return contattiIncompleti;
            }
            return new List<Contatto>();
        }

        /// <summary>
        /// Calcola le statistiche dettagliate delle chiamate per un periodo specifico
        /// </summary>
        public async Task<DetailedCallStatistics> GetDetailedStatisticsAsync(DateTime? fromDate, DateTime? toDate, bool includeInterni = false)
        {
            try
            {
                var chiamate = await GetAllChiamateAsync();
                var contatti = await _contattoService.GetAllAsync();
                
                // Ottieni tutti i numeri che sono interni (Interno != 0)
                var numeriInterni = contatti
                    .Where(c => c.Interno != 0)
                    .Select(c => c.NumeroContatto)
                    .ToHashSet();

                // Applica i filtri di data e interni
                if (fromDate.HasValue)
                {
                    chiamate = chiamate.Where(c => c.DataArrivoChiamata >= fromDate.Value).ToList();
                }
                if (toDate.HasValue)
                {
                    chiamate = chiamate.Where(c => c.DataArrivoChiamata <= toDate.Value).ToList();
                }

                // Filtra le chiamate interne se richiesto
                if (!includeInterni)
                {
                    chiamate = chiamate.Where(c => 
                        !(numeriInterni.Contains(c.NumeroChiamante) && numeriInterni.Contains(c.NumeroChiamato))
                    ).ToList();
                }

                var statistiche = new DetailedCallStatistics
                {
                    TotaleChiamate = chiamate.Count,
                    ChiamateInEntrata = chiamate.Count(c => c.TipoChiamata?.ToLower() == "in entrata"),
                    ChiamateInUscita = chiamate.Count(c => c.TipoChiamata?.ToLower() == "in uscita"),
                    ChiamatePerse = chiamate.Count(c => c.TipoChiamata?.ToLower() == "persa"),
                    ChiamateNonRisposta = chiamate.Count(c => c.TipoChiamata?.ToLower() == "non risposta"),
                    ChiamateManuali = chiamate.Count(c => c.CampoExtra1 == "Manuale"),
                    ChiamateAutomatiche = chiamate.Count(c => c.CampoExtra1 != "Manuale"),
                    DurataTotaleChiamate = chiamate.Sum(c => c.Durata.TotalSeconds),
                };

                // Calcola la durata media
                statistiche.DurataMediaChiamate = statistiche.TotaleChiamate > 0 
                    ? statistiche.DurataTotaleChiamate / statistiche.TotaleChiamate 
                    : 0;

                // Raggruppa per tipo di chiamata
                statistiche.ChiamatePerTipo = chiamate
                    .GroupBy(c => c.TipoChiamata ?? "Non specificato")
                    .ToDictionary(g => g.Key, g => g.Count());

                // Raggruppa per locazione
                statistiche.ChiamatePerLocazione = chiamate
                    .GroupBy(c => c.Locazione ?? "Non specificato")
                    .ToDictionary(g => g.Key, g => g.Count());

                // Raggruppa per giorno
                statistiche.ChiamatePerGiorno = chiamate
                    .GroupBy(c => c.DataArrivoChiamata.ToString("dd/MM/yyyy"))
                    .ToDictionary(g => g.Key, g => g.Count());

                // Raggruppa per ora
                statistiche.ChiamatePerOra = chiamate
                    .GroupBy(c => c.DataArrivoChiamata.ToString("HH:00"))
                    .ToDictionary(g => g.Key, g => g.Count());

                // Top 10 chiamanti
                statistiche.TopChiamanti = chiamate
                    .Where(c => !string.IsNullOrEmpty(c.NumeroChiamante))
                    .GroupBy(c => new { c.NumeroChiamante, c.RagioneSocialeChiamante })
                    .Select(g => new TopChiamante
                    {
                        Numero = g.Key.NumeroChiamante ?? "",
                        RagioneSociale = g.Key.RagioneSocialeChiamante ?? "",
                        NumeroChiamate = g.Count(),
                        DurataTotale = g.Sum(c => c.Durata.TotalSeconds)
                    })
                    .OrderByDescending(x => x.NumeroChiamate)
                    .Take(10)
                    .ToList();

                // Top 10 chiamati
                statistiche.TopChiamati = chiamate
                    .Where(c => !string.IsNullOrEmpty(c.NumeroChiamato))
                    .GroupBy(c => new { c.NumeroChiamato, c.RagioneSocialeChiamato })
                    .Select(g => new TopChiamato
                    {
                        Numero = g.Key.NumeroChiamato ?? "",
                        RagioneSociale = g.Key.RagioneSocialeChiamato ?? "",
                        NumeroChiamate = g.Count(),
                        DurataTotale = g.Sum(c => c.Durata.TotalSeconds)
                    })
                    .OrderByDescending(x => x.NumeroChiamate)
                    .Take(10)
                    .ToList();

                return statistiche;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il calcolo delle statistiche dettagliate");
                return new DetailedCallStatistics();
            }
        }
    }

    /// <summary>
    /// Classe che contiene le statistiche delle chiamate e dei contatti
    /// </summary>
    public class CallStatistics
    {
        public int ChiamateOggi { get; set; }
        public int ChiamateSettimana { get; set; }
        public int ContattiAttivi { get; set; }
        public int ContattiInattivi { get; set; }
        public int ContattiIncompleti { get; set; }
    }
}
