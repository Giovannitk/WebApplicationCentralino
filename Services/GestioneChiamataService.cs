using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using WebApplicationCentralino.Models;
using System.Collections.Generic;
using System;
using System.Text.Json;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace WebApplicationCentralino.Services
{
    public class GestioneChiamataService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GestioneChiamataService> _logger;

        public GestioneChiamataService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<GestioneChiamataService> logger)
        {
            _httpClient = httpClientFactory.CreateClient("GestioneChiamataService");
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> AggiungiChiamataAsync(Chiamata chiamata)
        {
            try
            {
                // Assicurati che almeno uno tra numero e ragione sociale sia presente per chiamante e chiamato
                if (string.IsNullOrEmpty(chiamata.NumeroChiamante) &&
                    !string.IsNullOrEmpty(chiamata.RagioneSocialeChiamante))
                {
                    chiamata.NumeroChiamante = "Non specificato";
                }

                if (string.IsNullOrEmpty(chiamata.NumeroChiamato) &&
                    !string.IsNullOrEmpty(chiamata.RagioneSocialeChiamato))
                {
                    chiamata.NumeroChiamato = "Non specificato";
                }

                // Assicurati che i campi obbligatori siano compilati
                if (string.IsNullOrEmpty(chiamata.RagioneSocialeChiamante))
                    chiamata.RagioneSocialeChiamante = chiamata.NumeroChiamante;

                if (string.IsNullOrEmpty(chiamata.RagioneSocialeChiamato))
                    chiamata.RagioneSocialeChiamato = chiamata.NumeroChiamato;

                if (string.IsNullOrEmpty(chiamata.UniqueID))
                    chiamata.UniqueID = Guid.NewGuid().ToString();

                // Prepara il payload JSON con esplicitamente tutti i campi
                var payload = new
                {
                    numeroChiamante = chiamata.NumeroChiamante ?? "",
                    numeroChiamato = chiamata.NumeroChiamato ?? "",
                    ragioneSocialeChiamante = chiamata.RagioneSocialeChiamante,
                    ragioneSocialeChiamato = chiamata.RagioneSocialeChiamato,
                    dataArrivoChiamata = chiamata.DataArrivoChiamata,
                    dataFineChiamata = chiamata.DataFineChiamata,
                    tipoChiamata = chiamata.TipoChiamata, 
                    locazione = chiamata.Locazione ?? "Non specificato",
                    locazioneChiamato = chiamata.LocazioneChiamato ?? "Non specificato",
                    uniqueID = chiamata.UniqueID,
                    campoExtra1 = chiamata.CampoExtra1
                };

                // Usa le opzioni di serializzazione per gestire date e valori null
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };

                var jsonContent = JsonSerializer.Serialize(payload, options);
                Console.WriteLine($"-- Payload inviato: {jsonContent}");

                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("api/call/add-call", content);
                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("401"))
            {
                _logger.LogError(ex, "Token scaduto o non valido");
                throw; // Let the middleware handle the 401
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nell'aggiunta della chiamata");
                return false;
            }
        }

        public async Task<bool> AggiornaChiamataAsync(Chiamata chiamata)
        {
            try
            {
                var payload = new
                {
                    id = chiamata.Id,
                    numeroChiamante = chiamata.NumeroChiamante,
                    numeroChiamato = chiamata.NumeroChiamato,
                    ragioneSocialeChiamante = chiamata.RagioneSocialeChiamante,
                    ragioneSocialeChiamato = chiamata.RagioneSocialeChiamato,
                    dataArrivoChiamata = chiamata.DataArrivoChiamata,
                    dataFineChiamata = chiamata.DataFineChiamata,
                    tipoChiamata = chiamata.TipoChiamata,
                    locazione = chiamata.Locazione,
                    locazioneChiamato = chiamata.LocazioneChiamato,
                    uniqueID = chiamata.UniqueID,
                    campoExtra1 = chiamata.CampoExtra1
                };

                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(payload, options),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PutAsync("api/call/update-call", content);
                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("401"))
            {
                _logger.LogError(ex, "Token scaduto o non valido");
                throw; // Let the middleware handle the 401
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nell'aggiornamento della chiamata");
                return false;
            }
        }

        public async Task<bool> EliminaChiamataAsync(int id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"api/call/delete-call-by-id?id={id}");
                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("401"))
            {
                _logger.LogError(ex, "Token scaduto o non valido");
                throw; // Let the middleware handle the 401
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nell'eliminazione della chiamata");
                return false;
            }
        }

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

        public async Task<int?> TrovaIdChiamataEsistenteAsync(DateTime dataArrivo, DateTime dataFine, string? numeroChiamante = null, string? numeroChiamato = null)
        {
            try
            {
                var chiamate = await GetAllChiamateAsync();

                // Normalizza le date rimuovendo millisecondi per un confronto più affidabile
                var dataArrivoNormalizzata = new DateTime(dataArrivo.Year, dataArrivo.Month, dataArrivo.Day,
                                                        dataArrivo.Hour, dataArrivo.Minute, dataArrivo.Second);
                var dataFineNormalizzata = new DateTime(dataFine.Year, dataFine.Month, dataFine.Day,
                                                      dataFine.Hour, dataFine.Minute, dataFine.Second);

                Console.WriteLine($"Cercando chiamata con date normalizzate: {dataArrivoNormalizzata} - {dataFineNormalizzata}");

                var chiamataTrovata = chiamate.FirstOrDefault(c =>
                {
                    // Normalizza le date dalla chiamata esistente
                    var cDataArrivoNorm = new DateTime(c.DataArrivoChiamata.Year, c.DataArrivoChiamata.Month, c.DataArrivoChiamata.Day,
                                                     c.DataArrivoChiamata.Hour, c.DataArrivoChiamata.Minute, c.DataArrivoChiamata.Second);
                    var cDataFineNorm = new DateTime(c.DataFineChiamata.Year, c.DataFineChiamata.Month, c.DataFineChiamata.Day,
                                                   c.DataFineChiamata.Hour, c.DataFineChiamata.Minute, c.DataFineChiamata.Second);

                    // Debug
                    //Console.WriteLine($"Confronto con ID {c.Id}: {cDataArrivoNorm} - {cDataFineNorm}");

                    // Confronta le date normalizzate e gli altri parametri
                    return cDataArrivoNorm == dataArrivoNormalizzata &&
                           cDataFineNorm == dataFineNormalizzata &&
                           (string.IsNullOrEmpty(numeroChiamante) || c.NumeroChiamante == numeroChiamante) &&
                           (string.IsNullOrEmpty(numeroChiamato) || c.NumeroChiamato == numeroChiamato);
                });

                if (chiamataTrovata != null)
                {
                    Console.WriteLine($"Trovata chiamata con ID: {chiamataTrovata.Id}");
                }
                else
                {
                    Console.WriteLine("Nessuna chiamata trovata con i criteri specificati");
                }

                return chiamataTrovata?.Id;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore durante la ricerca della chiamata: {ex.Message}");
                return null;
            }
        }

        public async Task<Chiamata> GetChiamataByIdAsync(int id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/call/find-call?callId={id}");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Errore API: {StatusCode}, Richiesta: api/call/find-call?callId={Id}", response.StatusCode, id);
                    return null;
                }

                var chiamata = await response.Content.ReadFromJsonAsync<Chiamata>();
                if (chiamata != null)
                {
                    chiamata.Id = id; // Assicurati che l'ID sia impostato correttamente
                    _logger.LogInformation($"Chiamata recuperata con ID {id}: {chiamata.NumeroChiamante}, {chiamata.Locazione}");
                }
                return chiamata;
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("401"))
            {
                _logger.LogError(ex, "Token scaduto o non valido");
                throw; // Let the middleware handle the 401
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il recupero della chiamata {Id}", id);
                throw new ApplicationException($"Errore durante il recupero della chiamata {id}", ex);
            }
        }

        // Metodo helper per la conversione di TipoChiamata
        // da Entrata e Uscita a "1" e "0".
        private string ConvertiTipoChiamataANumerico(string tipoChiamata)
        {
            if (string.IsNullOrEmpty(tipoChiamata))
                return "0"; // Default a "Entrata"

            return tipoChiamata.Trim().ToLower() switch
            {
                "entrata" => "0",
                "uscita" => "1",
                _ => "0" // Default se il valore non è riconosciuto
            };
        }
    }
}