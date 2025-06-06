﻿using System.Net.Http;
using System.Net.Http.Json;
using WebApplicationCentralino.Models;
using Microsoft.Extensions.Logging;

namespace WebApplicationCentralino.Services
{
    public class ContattoService : IContattoService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ContattoService> _logger;

        public ContattoService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<ContattoService> logger)
        {
            _httpClient = httpClientFactory.CreateClient("ContattoService");
            _logger = logger;
        }

        public async Task<List<Contatto>> GetAllAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<Contatto>>("api/call/all-contacts") ?? new List<Contatto>();
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
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateAsync(string numero, Contatto contatto)
        {
            var response = await _httpClient.PutAsJsonAsync($"api/call/update-contact/{numero}", contatto);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteAsync(string numero)
        {
            var response = await _httpClient.DeleteAsync($"api/call/delete-contact?phoneNumber={Uri.EscapeDataString(numero)}");
            return response.IsSuccessStatusCode;
        }

        public async Task<List<Chiamata>> GetCallsByNumberAsync(string numero)
        {
            return await _httpClient.GetFromJsonAsync<List<Chiamata>>($"api/call/{numero}/chiamate") ?? new List<Chiamata>();
        }
    }
}
