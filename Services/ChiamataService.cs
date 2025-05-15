using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using WebApplicationCentralino.Models;

namespace WebApplicationCentralino.Services
{
    public class ChiamataService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public ChiamataService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _httpClient.BaseAddress = new Uri(_configuration["ApiSettings:BaseUrl"]);
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

                // Filtraggio per data e durata
                return chiamate.Where(c =>
                    (fromDate == null || c.DataArrivoChiamata >= fromDate) &&
                    (toDate == null || c.DataArrivoChiamata <= toDate) &&
                    (c.Durata.TotalSeconds >= minDuration)
                ).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore durante il filtraggio delle chiamate: {ex.Message}");
                return new List<Chiamata>(); 
            }
        }
    }
}
