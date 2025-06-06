using System.Net.Http;
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
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration, 
            ContattoService contattoService, 
            ILogger<ChiamataService> logger,
            IMemoryCache cache)
        {
            _httpClient = httpClientFactory.CreateClient("ChiamataService");
            _configuration = configuration;
            _contattoService = contattoService;
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
                var response = await _httpClient.GetFromJsonAsync<List<Chiamata>>("api/call/get-all-calls");
                return response ?? new List<Chiamata>();
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("401"))
            {
                _logger.LogError(ex, "Token scaduto o non valido");
                throw; // Let the middleware handle the 401
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
        public async Task<List<Chiamata>> GetFilteredChiamateAsync(DateTime? fromDate = null, DateTime? toDate = null, double minDuration = 5, bool includeInterni = false)
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
                        // Escludiamo le chiamate interne solo se includeInterni è false
                        (includeInterni || !(numeriInterni.Contains(c.NumeroChiamante) && numeriInterni.Contains(c.NumeroChiamato)))
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
        public async Task<DetailedCallStatistics> GetDetailedStatisticsAsync(DateTime? dateFrom, DateTime? dateTo, bool includeInterni, string? comune = null, string? searchContatto = null)
        {
            try
            {
                var chiamate = await GetAllChiamateAsync();
                var contatti = await _contattoService.GetAllAsync();
                var statistiche = new DetailedCallStatistics();

                // Filtra le chiamate per data
                chiamate = chiamate.Where(c => 
                    (dateFrom == null || c.DataArrivoChiamata >= dateFrom) &&
                    (dateTo == null || c.DataArrivoChiamata <= dateTo)
                ).ToList();

                // Ottieni tutti i numeri che sono interni (Interno != Entrata)
                var numeriInterni = contatti
                    .Where(c => c.Interno != 0)
                    .Select(c => c.NumeroContatto)
                    .ToHashSet();

                // Calcola le chiamate interne
                var chiamateInterne = chiamate.Where(c => 
                    numeriInterni.Contains(c.NumeroChiamante) && numeriInterni.Contains(c.NumeroChiamato)
                ).ToList();

                // Filtra per chiamate interne se necessario
                if (!includeInterni)
                {
                    chiamate = chiamate.Where(c => 
                        !(numeriInterni.Contains(c.NumeroChiamante) && numeriInterni.Contains(c.NumeroChiamato))
                    ).ToList();
                }

                // Filtra per comune se specificato
                if (!string.IsNullOrWhiteSpace(comune))
                {
                    // Ottieni i numeri dei contatti che hanno il comune nella ragione sociale
                    var numeriContattiComune = contatti
                        .Where(c => c.RagioneSociale != null && c.RagioneSociale.Contains(comune, StringComparison.OrdinalIgnoreCase))
                        .Select(c => c.NumeroContatto)
                        .ToList();

                    // Filtra le chiamate dove il chiamante o il chiamato è uno dei contatti del comune
                    chiamate = chiamate.Where(c => 
                        (c.NumeroChiamante != null && numeriContattiComune.Contains(c.NumeroChiamante)) ||
                        (c.NumeroChiamato != null && numeriContattiComune.Contains(c.NumeroChiamato))
                    ).ToList();
                }

                // Filtra per ricerca contatto se specificato
                if (!string.IsNullOrWhiteSpace(searchContatto))
                {
                    // Ottieni i numeri dei contatti che corrispondono esattamente alla ricerca
                    var numeriContattiRicerca = contatti
                        .Where(c => 
                            (c.NumeroContatto != null && c.NumeroContatto.Equals(searchContatto, StringComparison.OrdinalIgnoreCase)) ||
                            (c.RagioneSociale != null && c.RagioneSociale.Equals(searchContatto, StringComparison.OrdinalIgnoreCase))
                        )
                        .Select(c => c.NumeroContatto)
                        .ToList();

                    // Filtra le chiamate dove il chiamante o il chiamato è uno dei contatti trovati
                    chiamate = chiamate.Where(c => 
                        (c.NumeroChiamante != null && numeriContattiRicerca.Contains(c.NumeroChiamante)) ||
                        (c.NumeroChiamato != null && numeriContattiRicerca.Contains(c.NumeroChiamato))
                    ).ToList();

                    // Calcola le statistiche specifiche per il contatto
                    var chiamateContatto = chiamate.Where(c => 
                        numeriContattiRicerca.Contains(c.NumeroChiamante) || 
                        numeriContattiRicerca.Contains(c.NumeroChiamato)
                    ).ToList();

                    // Aggiorna le statistiche per il contatto
                    statistiche.TotaleChiamate = chiamateContatto.Count;
                    statistiche.ChiamateInEntrata = chiamateContatto.Count(c => numeriContattiRicerca.Contains(c.NumeroChiamato));
                    statistiche.ChiamateInUscita = chiamateContatto.Count(c => numeriContattiRicerca.Contains(c.NumeroChiamante));
                    statistiche.ChiamatePerse = chiamateContatto.Count(c => 
                        (numeriContattiRicerca.Contains(c.NumeroChiamato) || numeriContattiRicerca.Contains(c.NumeroChiamante)) && 
                        c.TipoChiamata?.ToLower() == "persa");
                    statistiche.ChiamateNonRisposta = chiamateContatto.Count(c => 
                        (numeriContattiRicerca.Contains(c.NumeroChiamato) || numeriContattiRicerca.Contains(c.NumeroChiamante)) && 
                        c.TipoChiamata?.ToLower() == "non risposta");
                    statistiche.ChiamateManuali = chiamateContatto.Count(c => c.CampoExtra1 == "Manuale");
                    statistiche.ChiamateAutomatiche = chiamateContatto.Count(c => c.CampoExtra1 != "Manuale");
                    statistiche.ChiamateInterne = chiamateInterne.Count;
                    statistiche.DurataTotaleChiamate = chiamateContatto.Sum(c => c.Durata.TotalSeconds);

                    // Calcola la durata media
                    statistiche.DurataMediaChiamate = statistiche.TotaleChiamate > 0 
                        ? statistiche.DurataTotaleChiamate / statistiche.TotaleChiamate 
                        : 0;

                    // Raggruppa per tipo di chiamata
                    statistiche.ChiamatePerTipo = chiamateContatto
                        .GroupBy(c => c.TipoChiamata ?? "Non specificato")
                        .ToDictionary(g => g.Key, g => g.Count());

                    // Raggruppa per locazione
                    statistiche.ChiamatePerLocazione = chiamateContatto
                        .GroupBy(c => c.Locazione ?? "Non specificato")
                        .ToDictionary(g => g.Key, g => g.Count());

                    // Raggruppa per giorno
                    statistiche.ChiamatePerGiorno = chiamateContatto
                        .GroupBy(c => c.DataArrivoChiamata.ToString("dd/MM/yyyy"))
                        .ToDictionary(g => g.Key, g => g.Count());

                    // Raggruppa per ora
                    statistiche.ChiamatePerOra = chiamateContatto
                        .GroupBy(c => c.DataArrivoChiamata.ToString("HH:00"))
                        .ToDictionary(g => g.Key, g => g.Count());

                    // Top chiamanti per il contatto
                    statistiche.TopChiamanti = chiamateContatto
                        .Where(c => numeriContattiRicerca.Contains(c.NumeroChiamato) && !string.IsNullOrEmpty(c.NumeroChiamante))
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

                    // Top chiamati per il contatto
                    statistiche.TopChiamati = chiamateContatto
                        .Where(c => numeriContattiRicerca.Contains(c.NumeroChiamante) && !string.IsNullOrEmpty(c.NumeroChiamato))
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

                // Se non ci sono filtri di comune o ricerca, usa tutte le chiamate filtrate solo per data e interni
                statistiche.TotaleChiamate = chiamate.Count;
                statistiche.ChiamateInEntrata = chiamate.Count(c => c.TipoChiamata?.ToLower() == "in entrata");
                statistiche.ChiamateInUscita = chiamate.Count(c => c.TipoChiamata?.ToLower() == "in uscita");
                statistiche.ChiamatePerse = chiamate.Count(c => c.TipoChiamata?.ToLower() == "persa");
                statistiche.ChiamateNonRisposta = chiamate.Count(c => c.TipoChiamata?.ToLower() == "non risposta");
                statistiche.ChiamateManuali = chiamate.Count(c => c.CampoExtra1 == "Manuale");
                statistiche.ChiamateAutomatiche = chiamate.Count(c => c.CampoExtra1 != "Manuale");
                statistiche.ChiamateInterne = chiamateInterne.Count;
                statistiche.DurataTotaleChiamate = chiamate.Sum(c => c.Durata.TotalSeconds);

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

        /// <summary>
        /// Calcola le statistiche dettagliate per un contatto specifico
        /// </summary>
        public async Task<ContactStatistics> GetContactStatisticsAsync(string numeroContatto, DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var chiamate = await GetAllChiamateAsync();
                var contatti = await _contattoService.GetAllAsync();
                
                // Trova il contatto
                var contatto = contatti.FirstOrDefault(c => c.NumeroContatto == numeroContatto);
                if (contatto == null)
                {
                    throw new Exception($"Contatto non trovato: {numeroContatto}");
                }

                // Filtra le chiamate per il periodo specificato
                if (fromDate.HasValue)
                {
                    chiamate = chiamate.Where(c => c.DataArrivoChiamata >= fromDate.Value).ToList();
                }
                if (toDate.HasValue)
                {
                    chiamate = chiamate.Where(c => c.DataArrivoChiamata <= toDate.Value).ToList();
                }

                // Filtra le chiamate per il contatto specifico
                var chiamateContatto = chiamate.Where(c => 
                    c.NumeroChiamante == numeroContatto || 
                    c.NumeroChiamato == numeroContatto
                ).ToList();

                var statistiche = new ContactStatistics
                {
                    Numero = numeroContatto,
                    RagioneSociale = contatto.RagioneSociale,
                    ChiamateInEntrata = chiamateContatto.Count(c => c.NumeroChiamato == numeroContatto),
                    ChiamateInUscita = chiamateContatto.Count(c => c.NumeroChiamante == numeroContatto),
                    ChiamatePerse = chiamateContatto.Count(c => 
                        (c.NumeroChiamato == numeroContatto || c.NumeroChiamante == numeroContatto) && 
                        c.TipoChiamata?.ToLower() == "persa"),
                    ChiamateNonRisposta = chiamateContatto.Count(c => 
                        (c.NumeroChiamato == numeroContatto || c.NumeroChiamante == numeroContatto) && 
                        c.TipoChiamata?.ToLower() == "non risposta"),
                    DurataTotaleChiamate = chiamateContatto.Sum(c => c.Durata.TotalSeconds),
                };

                // Raggruppa per giorno
                statistiche.ChiamatePerGiorno = chiamateContatto
                    .GroupBy(c => c.DataArrivoChiamata.ToString("dd/MM/yyyy"))
                    .ToDictionary(g => g.Key, g => g.Count());

                // Raggruppa per ora
                statistiche.ChiamatePerOra = chiamateContatto
                    .GroupBy(c => c.DataArrivoChiamata.ToString("HH:00"))
                    .ToDictionary(g => g.Key, g => g.Count());

                // Ultime 10 chiamate
                statistiche.UltimeChiamate = chiamateContatto
                    .OrderByDescending(c => c.DataArrivoChiamata)
                    .Take(10)
                    .ToList();

                return statistiche;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il calcolo delle statistiche del contatto");
                throw;
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
