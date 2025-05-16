using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using WebApplicationCentralino.Models;
using WebApplicationCentralino.Services;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace WebApplicationCentralino.Services
{
    public class ChiamataService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ContattoService _contattoService;
        private readonly ILogger<ChiamataService> _logger;

        public ChiamataService(HttpClient httpClient, IConfiguration configuration, ContattoService contattoService, ILogger<ChiamataService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _contattoService = contattoService;
            _httpClient.BaseAddress = new Uri(_configuration["ApiSettings:BaseUrl"]);

            _logger = logger;
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

                _logger.LogInformation("caio1");

                // Ottieni tutti i numeri che sono interni (Interno != Entrata)
                var numeriInterni = contatti
                    .Where(c => c.Interno != 0)
                    .Select(c => c.NumeroContatto)
                    .ToHashSet();

                _logger.LogInformation("caio2");

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

                _logger.LogInformation("caio3");

                return chiamateFiltrate;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore durante il filtraggio delle chiamate: {ex.Message}");
                return new List<Chiamata>();
            }
        }
    }
}
