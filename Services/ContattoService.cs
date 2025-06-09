using System.Net.Http;
using System.Net.Http.Json;
using WebApplicationCentralino.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;

namespace WebApplicationCentralino.Services
{
    public class ContattoService : IContattoService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ContattoService> _logger;
        private readonly IMemoryCache _cache;
        private const string CACHE_KEY = "all_contacts";
        private static readonly TimeSpan CACHE_DURATION = TimeSpan.FromMinutes(30);

        public ContattoService(
            IHttpClientFactory httpClientFactory, 
            IConfiguration configuration, 
            ILogger<ContattoService> logger,
            IMemoryCache cache)
        {
            _httpClient = httpClientFactory.CreateClient("ContattoService");
            _logger = logger;
            _cache = cache;
        }

        public async Task<(List<Contatto> Contatti, int TotalCount)> GetPaginatedContattiAsync(int pageNumber, int pageSize, string? searchTerm = null)
        {
            try
            {
                // Try to get from cache first
                if (!_cache.TryGetValue(CACHE_KEY, out List<Contatto> allContatti))
                {
                    // If not in cache, get from API
                    allContatti = await _httpClient.GetFromJsonAsync<List<Contatto>>("api/call/all-contacts") ?? new List<Contatto>();
                    
                    // Store in cache
                    var cacheOptions = new MemoryCacheEntryOptions()
                        .SetAbsoluteExpiration(CACHE_DURATION);
                    _cache.Set(CACHE_KEY, allContatti, cacheOptions);
                }

                // Apply search filter if provided
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();
                    allContatti = allContatti.Where(c => 
                        (c.NumeroContatto?.ToLower().Contains(searchTerm) ?? false) ||
                        (c.RagioneSociale?.ToLower().Contains(searchTerm) ?? false) ||
                        (c.Citta?.ToLower().Contains(searchTerm) ?? false)
                    ).ToList();
                }

                // Calculate pagination
                var totalCount = allContatti.Count;
                var paginatedContatti = allContatti
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return (paginatedContatti, totalCount);
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("401"))
            {
                _logger.LogError(ex, "Token scaduto o non valido");
                throw; // Let the middleware handle the 401
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il recupero dei contatti");
                return (new List<Contatto>(), 0);
            }
        }

        public async Task<List<Contatto>> GetAllAsync()
        {
            try
            {
                // Try to get from cache first
                if (_cache.TryGetValue(CACHE_KEY, out List<Contatto> cachedContatti))
                {
                    return cachedContatti;
                }

                // If not in cache, get from API
                var contatti = await _httpClient.GetFromJsonAsync<List<Contatto>>("api/call/all-contacts") ?? new List<Contatto>();
                
                // Store in cache
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(CACHE_DURATION);
                _cache.Set(CACHE_KEY, contatti, cacheOptions);

                return contatti;
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("401"))
            {
                _logger.LogError(ex, "Token scaduto o non valido");
                throw; // Let the middleware handle the 401
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il recupero dei contatti");
                return new List<Contatto>();
            }
        }

        public async Task<List<Contatto>> GetFilteredContattiAsync()
        {
            try
            {
                var contatti = await GetAllAsync();
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
            var url = $"api/call/search?{query}";

            return await _httpClient.GetFromJsonAsync<List<Contatto>>(url) ?? new List<Contatto>();
        }

        public async Task<List<Contatto>> GetIncompleteAsync()
        {
            return await _httpClient.GetFromJsonAsync<List<Contatto>>("api/call/incompleti") ?? new List<Contatto>();
        }

        public async Task<bool> AddAsync(Contatto contatto)
        {
            var response = await _httpClient.PostAsJsonAsync("api/call/add-contact", contatto);
            if (response.IsSuccessStatusCode)
            {
                // Invalidate cache after successful add
                _cache.Remove(CACHE_KEY);
            }
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateAsync(string numero, Contatto contatto)
        {
            var response = await _httpClient.PutAsJsonAsync($"api/call/update-contact/{numero}", contatto);
            if (response.IsSuccessStatusCode)
            {
                // Invalidate cache after successful update
                _cache.Remove(CACHE_KEY);
            }
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteAsync(string numero)
        {
            var response = await _httpClient.DeleteAsync($"api/call/delete-contact?phoneNumber={Uri.EscapeDataString(numero)}");
            if (response.IsSuccessStatusCode)
            {
                // Invalidate cache after successful delete
                _cache.Remove(CACHE_KEY);
            }
            return response.IsSuccessStatusCode;
        }

        public async Task<List<Chiamata>> GetCallsByNumberAsync(string numero)
        {
            return await _httpClient.GetFromJsonAsync<List<Chiamata>>($"api/call/{numero}/chiamate") ?? new List<Chiamata>();
        }

        // Metodo per forzare il refresh della cache
        public void InvalidateCache()
        {
            _cache.Remove(CACHE_KEY);
        }
    }
}
