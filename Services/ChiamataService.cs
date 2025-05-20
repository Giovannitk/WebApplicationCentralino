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
    public class ChiamataService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ContattoService _contattoService;
        private readonly ILogger<ChiamataService> _logger;
        private readonly IMemoryCache _cache;
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

        public async Task<List<Chiamata>> GetAllChiamateAsync()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<Chiamata>>("api/Call/get-all-calls");
                return response ?? new List<Chiamata>();
            }
            catch (Exception ex)
            {
                // Log dell'errore
                Console.WriteLine($"Errore durante il recupero delle chiamate: {ex.Message}");
                return new List<Chiamata>();
            }
        }

        // Services/ChiamataService.cs
        public async Task<List<Chiamata>> GetFilteredChiamateAsync(DateTime? fromDate = null, DateTime? toDate = null, double minDuration = 5)
        {
            try
            {
                var chiamate = await GetAllChiamateAsync();
                var contatti = await _contattoService.GetAllAsync();

                //_logger.LogInformation("caio1");

                // Ottieni tutti i numeri che sono interni (Interno != Entrata)
                var numeriInterni = contatti
                    .Where(c => c.Interno != 0)
                    .Select(c => c.NumeroContatto)
                    .ToHashSet();

                //_logger.LogInformation("caio2");

                // Filtra le chiamate combinando tutti i criteri
                var chiamateFiltrate = chiamate
                    .Where(c =>
                        (fromDate == null || c.DataArrivoChiamata >= fromDate) &&
                        (toDate == null || c.DataArrivoChiamata <= toDate) &&
                        (c.Durata.TotalSeconds >= minDuration) &&
                        // Qui sta la modifica principale: invece di escludere chiamate dove 
                        // ALMENO uno dei numeri è interno, escludiamo solo quelle dove ENTRAMBI sono interni
                        !(numeriInterni.Contains(c.NumeroChiamante) && numeriInterni.Contains(c.NumeroChiamato))
                    )
                    .ToList();

                //_logger.LogInformation("caio3");

                return chiamateFiltrate;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore durante il filtraggio delle chiamate: {ex.Message}");
                return new List<Chiamata>();
            }
        }

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

        public List<Contatto> GetIncompleteContacts()
        {
            if (_cache.TryGetValue(INCOMPLETE_CONTACTS_CACHE_KEY, out List<Contatto> contattiIncompleti))
            {
                return contattiIncompleti;
            }
            return new List<Contatto>();
        }
    }

    public class CallStatistics
    {
        public int ChiamateOggi { get; set; }
        public int ChiamateSettimana { get; set; }
        public int ContattiAttivi { get; set; }
        public int ContattiInattivi { get; set; }
        public int ContattiIncompleti { get; set; }
    }
}
