using System.Net.Http;
using System.Net.Http.Json;
using WebApplicationCentralino.Models;

namespace WebApplicationCentralino.Services
{
    public class ContattoService : IContattoService
    {
        private readonly HttpClient _httpClient;

        public ContattoService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri(configuration["ApiSettings:BaseUrl"]);
        }

        public async Task<List<Contatto>> GetAllAsync()
        {
            try 
            {
                return await _httpClient.GetFromJsonAsync<List<Contatto>>("api/Call/all-contacts") ?? new List<Contatto>();
            }
            catch (Exception ex)
            {
                // Log dell'errore
                Console.WriteLine($"Errore durante il recupero dei contatti: {ex.Message}");
                return new List<Contatto>();
            }
        }

        public async Task<List<Contatto>> GetFilteredContattiAsync()
        {
            try
            {
                var contatti = await GetAllAsync();

                // Filtraggio per data e durata
                return contatti;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore durante il filtraggio delle chiamate: {ex.Message}");
                return new List<Contatto>();
            }
        }



        public async Task<List<Contatto>> SearchAsync(string? numero, string? ragione)
        {
            var queryParams = new List<string>();
            if (!string.IsNullOrWhiteSpace(numero))
                queryParams.Add($"numero={Uri.EscapeDataString(numero)}");
            if (!string.IsNullOrWhiteSpace(ragione))
                queryParams.Add($"ragione={Uri.EscapeDataString(ragione)}");

            var query = string.Join("&", queryParams);
            var url = $"api/contatti/search?{query}";

            return await _httpClient.GetFromJsonAsync<List<Contatto>>(url) ?? new List<Contatto>();
        }

        public async Task<List<Contatto>> GetIncompleteAsync()
        {
            return await _httpClient.GetFromJsonAsync<List<Contatto>>("api/contatti/incompleti") ?? new List<Contatto>();
        }

        public async Task<bool> AddAsync(Contatto contatto)
        {
            var response = await _httpClient.PostAsJsonAsync("api/contatti/add-contact", contatto);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateAsync(string numero, Contatto contatto)
        {
            var response = await _httpClient.PutAsJsonAsync($"api/contatti/{numero}", contatto);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteAsync(string numero)
        {
            var response = await _httpClient.DeleteAsync($"api/contatti/{numero}");
            return response.IsSuccessStatusCode;
        }

        public async Task<List<Chiamata>> GetCallsByNumberAsync(string numero)
        {
            return await _httpClient.GetFromJsonAsync<List<Chiamata>>($"api/contatti/{numero}/chiamate") ?? new List<Chiamata>();
        }
    }
}
