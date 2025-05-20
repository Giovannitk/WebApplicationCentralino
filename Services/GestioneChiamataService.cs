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

namespace WebApplicationCentralino.Services
{
    public class GestioneChiamataService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public GestioneChiamataService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _httpClient.BaseAddress = new Uri(_configuration["ApiSettings:BaseUrl"]);
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
                    uniqueID = chiamata.UniqueID
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
                //Console.WriteLine($"-- Payload inviato content: {content}");

                var response = await _httpClient.PostAsync("api/call/add-call", content);

                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Risposta API: {response.StatusCode} - {responseContent}");

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Errore HTTP: {(int)response.StatusCode} - {response.StatusCode}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore nell'invio della chiamata: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Eccezione interna: {ex.InnerException.Message}");
                }
                return false;
            }
        }

        public async Task<bool> AggiornaChiamataAsync(Chiamata chiamata)
        {
            try
            {
                // Prepara il payload JSON con esplicitamente tutti i campi
                var payload = new
                {
                    id = chiamata.Id,
                    numeroChiamante = chiamata.NumeroChiamante ?? "",
                    numeroChiamato = chiamata.NumeroChiamato ?? "",
                    ragioneSocialeChiamante = chiamata.RagioneSocialeChiamante ?? "Non specificato",
                    ragioneSocialeChiamato = chiamata.RagioneSocialeChiamato ?? "Non specificato",
                    dataArrivoChiamata = chiamata.DataArrivoChiamata,
                    dataFineChiamata = chiamata.DataFineChiamata,
                    tipoChiamata = chiamata.TipoChiamata, // Ora è un intero (0 o 1)
                    locazione = chiamata.Locazione ?? "Non specificato",
                    uniqueID = chiamata.UniqueID ?? Guid.NewGuid().ToString()
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

                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Risposta API aggiornamento: {response.StatusCode} - {responseContent}");

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore nell'aggiornamento della chiamata: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> EliminaChiamataAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"api/call/delete-call-by-id?id={id}");
            return response.IsSuccessStatusCode;
        }

        public async Task<List<Chiamata>> GetAllChiamateAsync()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<Chiamata>>("api/call/get-all-calls");
                return response ?? new List<Chiamata>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore durante il recupero delle chiamate: {ex.Message}");
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
                // CORREZIONE: Modifica della URL per passare callId come parametro di query
                var response = await _httpClient.GetAsync($"api/call/find-call?callId={id}");

                if (!response.IsSuccessStatusCode)
                {
                    // Loggare l'errore è importante
                    Console.WriteLine($"Errore API: {response.StatusCode}, Richiesta: api/call/find-call?callId={id}");
                    return null;
                }

                return await response.Content.ReadFromJsonAsync<Chiamata>();
            }
            catch (Exception ex)
            {
                // Loggare l'eccezione
                Console.WriteLine($"Eccezione durante la chiamata API: {ex.Message}");
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

        //public async Task<bool> VerificaCorrispondenzaNumeroRagioneSocialeAsync(string numero, string ragioneSociale)
        //{
        //    try
        //    {
        //        if (string.IsNullOrEmpty(numero) || string.IsNullOrEmpty(ragioneSociale))
        //        {
        //            return false;
        //        }

        //        // Chiamata all'API per verificare la corrispondenza tra numero e ragione sociale
        //        var response = await _httpClient.GetAsync($"api/contatto/verifica-corrispondenza?numero={Uri.EscapeDataString(numero)}&ragioneSociale={Uri.EscapeDataString(ragioneSociale)}");

        //        if (!response.IsSuccessStatusCode)
        //        {
        //            // Se l'API non esiste, utilizziamo una verifica locale con i dati disponibili
        //            var contatti = await GetAllContattiAsync(); // Assumi che esista un metodo per ottenere tutti i contatti
        //            return contatti.Any(c => c.NumeroContatto == numero && c.RagioneSociale == ragioneSociale);
        //        }

        //        // Leggi il risultato dalla risposta API
        //        var result = await response.Content.ReadFromJsonAsync<bool>();
        //        return result;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Errore durante la verifica di corrispondenza contatto: {ex.Message}");
        //        return false; // In caso di errore, fallisci in modo sicuro
        //    }
        //}

        // Se necessario, aggiungi un metodo per ottenere tutti i contatti (se non esiste già)
        //private async Task<List<Contatto>> GetAllContattiAsync()
        //{
        //    try
        //    {
        //        var response = await _httpClient.GetFromJsonAsync<List<Contatto>>("api/contatto/get-all-contatti");
        //        return response ?? new List<Contatto>();
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Errore durante il recupero dei contatti: {ex.Message}");
        //        return new List<Contatto>();
        //    }
        //}
    }
}