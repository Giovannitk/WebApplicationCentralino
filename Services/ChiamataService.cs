using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using WebApplicationCentralino.Models;
using WebApplicationCentralino.Services;
using Microsoft.Extensions.Logging;
using System.Linq;
using Microsoft.Extensions.Caching.Memory;
using WebApplicationCentralino.Managers;
using WebApplicationCentralino.Extensions;

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
        /// Determina se una chiamata è interna basandosi sui criteri specificati
        /// </summary>
        private bool IsChiamataInterna(Chiamata chiamata, HashSet<string> numeriInterni, bool useTipoChiamata = false)
        {
            if (useTipoChiamata)
            {
                // Usa il campo TipoChiamata per identificare le chiamate interne
                return chiamata.TipoChiamata?.Equals("Interna", StringComparison.OrdinalIgnoreCase) == true;
            }
            else
            {
                // Usa la logica tradizionale: controlla se sia chiamante che chiamato sono interni
                return numeriInterni.Contains(chiamata.NumeroChiamante) && numeriInterni.Contains(chiamata.NumeroChiamato);
            }
        }


        private Contatto? TrovaContatto(string? numero, List<Contatto> contatti)
        {
            if (string.IsNullOrWhiteSpace(numero))
                return null;

            var numeriContatto = contatti
              .Where(c => !string.IsNullOrWhiteSpace(c.NumeroContatto))
              .Select(c => c.NumeroContatto!)
              .ToHashSet();

            // Prova tutte le versioni del numero da confrontare
            var possibiliNumeri = new List<string> { numero };

            // Rimuove le prime n cifre (max 3)
            if (numero.Length > 1)
                possibiliNumeri.Add(numero.Substring(1));
            if (numero.Length > 2)
                possibiliNumeri.Add(numero.Substring(2));
            if (numero.Length > 3)
                possibiliNumeri.Add(numero.Substring(3));

            foreach (var variante in possibiliNumeri)
            {
                var contatto = contatti.FirstOrDefault(c => c.NumeroContatto == variante);
                if (contatto != null)
                    return contatto;
            }

            return null;
        }


        private string EstraiLocazioneDaRagioneSociale(string? ragioneSociale)
        {
            if (string.IsNullOrWhiteSpace(ragioneSociale))
                return string.Empty;

            var parts = ragioneSociale.Split('-', 2);
            return parts.Length == 2 ? parts[1].Trim() : ragioneSociale.Trim();
        }


        /// <summary>
        /// Recupera tutte le chiamate dal servizio API
        /// </summary>
        public async Task<List<Chiamata>> GetAllChiamateAsync()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<Chiamata>>("api/call/get-all-calls");
                var chiamate = response ?? new List<Chiamata>();

                var contatti = await _contattoService.GetAllAsync();

                foreach (var chiamata in chiamate)
                {
                    // --- CHIAMATO ---
                    if (string.IsNullOrWhiteSpace(chiamata.RagioneSocialeChiamato) || chiamata.RagioneSocialeChiamato == "Non registrato")
                    {
                        var contatto = TrovaContatto(chiamata.NumeroChiamato, contatti);
                        if (contatto != null)
                        {
                            chiamata.RagioneSocialeChiamato = contatto.RagioneSociale;

                            if (!string.Equals(chiamata.NumeroChiamato, contatto.NumeroContatto, StringComparison.Ordinal))
                            {
                                //_logger.LogInformation("NumeroChiamato aggiornato da {VecchioNumero} a {NuovoNumero}",
                                  //chiamata.NumeroChiamato, contatto.NumeroContatto);

                                chiamata.NumeroChiamato = contatto.NumeroContatto;
                            }

                            var locazione = EstraiLocazioneDaRagioneSociale(contatto.RagioneSociale);
                            if (!string.IsNullOrWhiteSpace(locazione))
                            {
                                chiamata.LocazioneChiamato = locazione;
                            }

                            //_logger.LogInformation("Aggiornata chiamata (chiamato): {Numero} → {RagioneSociale}, Locazione: {Locazione}",
                              //chiamata.NumeroChiamato, contatto.RagioneSociale, chiamata.LocazioneChiamato);
                        }
                    }

                    // --- CHIAMANTE ---
                    if (string.IsNullOrWhiteSpace(chiamata.RagioneSocialeChiamante) || chiamata.RagioneSocialeChiamante == "Non registrato")
                    {
                        var contatto = TrovaContatto(chiamata.NumeroChiamante, contatti);
                        if (contatto != null)
                        {
                            chiamata.RagioneSocialeChiamante = contatto.RagioneSociale;

                            if (!string.Equals(chiamata.NumeroChiamante, contatto.NumeroContatto, StringComparison.Ordinal))
                            {
                                //_logger.LogInformation("NumeroChiamante aggiornato da {VecchioNumero} a {NuovoNumero}",
                                  //chiamata.NumeroChiamante, contatto.NumeroContatto);

                                chiamata.NumeroChiamante = contatto.NumeroContatto;
                            }

                            var locazione = EstraiLocazioneDaRagioneSociale(contatto.RagioneSociale);
                            if (!string.IsNullOrWhiteSpace(locazione))
                            {
                                chiamata.Locazione = locazione;
                            }

                            //_logger.LogInformation("Aggiornata chiamata (chiamante): {Numero} → {RagioneSociale}, Locazione: {Locazione}",
                              //chiamata.NumeroChiamante, contatto.RagioneSociale, chiamata.Locazione);
                        }
                    }


                }

                return ChiamataHelper.UnisciChiamateTrasferimento(chiamate, contatti,_logger);
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("401"))
            {
                _logger.LogError(ex, "Token scaduto o non valido");
                throw;
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
        public async Task<List<Chiamata>> GetFilteredChiamateAsync(DateTime? fromDate = null, DateTime? toDate = null, double minDuration = 5, bool includeInterni = false, bool useTipoChiamataForInterni = true)
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
                        (includeInterni || !IsChiamataInterna(c, numeriInterni, useTipoChiamataForInterni))
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
        public async Task<CallStatistics> GetCallStatisticsAsync(bool useTipoChiamataForInterni = true)
        {
            try
            {
                var chiamate = await GetAllChiamateAsync();
                var contatti = await _contattoService.GetAllAsync();
                var oggi = DateTime.Today;
                var inizioSettimana = oggi.AddDays(-(int)oggi.DayOfWeek);

                // Ottieni tutti i numeri che sono interni (Interno != Entrata)
                var numeriInterni = contatti
                    .Where(c => c.Interno != 0)
                    .Select(c => c.NumeroContatto)
                    .ToHashSet();

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
        public async Task<DetailedCallStatistics> GetDetailedStatisticsAsync(DateTime? dateFrom, DateTime? dateTo, bool includeInterni, string? comune = null, string? searchContatto = null, int topCount = 10, bool useTipoChiamataForInterni = true)
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

                // Calcola le chiamate interne usando il metodo appropriato
                var chiamateInterne = chiamate.Where(c => IsChiamataInterna(c, numeriInterni, useTipoChiamataForInterni)).ToList();

                // Filtra per chiamate interne se necessario
                if (!includeInterni)
                {
                    chiamate = chiamate.Where(c => !IsChiamataInterna(c, numeriInterni, useTipoChiamataForInterni)).ToList();
                }

                // Filtra per comune se specificato
                if (!string.IsNullOrWhiteSpace(comune))
                {
                    // Rimuovi "Comune di " dal nome del comune se presente
                    var comuneToSearch = comune.Replace("Comune di ", "").Trim();

                    // Ottieni il valore del database dal ComuniManager
                    var dbValue = ComuniManager.GetDatabaseValue(comuneToSearch);

                    // Crea una lista di possibili varianti del nome del comune
                    var comuneVariants = new List<string>
                    {
                        comuneToSearch,                    // Nome originale
                        dbValue,                           // Valore nel database
                        comuneToSearch.ToUpper(),          // Nome in maiuscolo
                        dbValue.ToUpper(),                 // Valore DB in maiuscolo
                        $"COMUNE DI {comuneToSearch.ToUpper()}",  // COMUNE DI + nome maiuscolo
                        $"Comune di {comuneToSearch}",     // Comune di + nome originale
                        $"COMUNE DI {dbValue.ToUpper()}",  // COMUNE DI + valore DB maiuscolo
                        $"Comune di {dbValue}",            // Comune di + valore DB
                        $"- COMUNE DI {comuneToSearch.ToUpper()}", // - COMUNE DI + nome maiuscolo
                        $"- COMUNE DI {dbValue.ToUpper()}",        // - COMUNE DI + valore DB maiuscolo
                        $"- Comune di {comuneToSearch}",           // - Comune di + nome originale
                        $"- Comune di {dbValue}"                   // - Comune di + valore DB
                    };

                    // Rimuovi eventuali duplicati mantenendo l'ordine
                    comuneVariants = comuneVariants.Distinct().ToList();

                    chiamate = chiamate.Where(c =>
                        (!string.IsNullOrEmpty(c.RagioneSocialeChiamante) &&
                         comuneVariants.Any(v => c.RagioneSocialeChiamante.Contains(v, StringComparison.OrdinalIgnoreCase))) ||
                        (!string.IsNullOrEmpty(c.RagioneSocialeChiamato) &&
                         comuneVariants.Any(v => c.RagioneSocialeChiamato.Contains(v, StringComparison.OrdinalIgnoreCase))) ||
                        (!string.IsNullOrEmpty(c.Locazione) &&
                         comuneVariants.Any(v => c.Locazione.Contains(v, StringComparison.OrdinalIgnoreCase)))
                    ).ToList();
                }

                // Filtra per ricerca contatto se specificato
                if (!string.IsNullOrWhiteSpace(searchContatto))
                {
                    // Se searchContatto contiene il separatore "|", significa che è un identificatore completo (numero|ragioneSociale)
                    if (searchContatto.Contains("|"))
                    {
                        var parts = searchContatto.Split('|');
                        var numeroContatto = parts[0];
                        var ragioneSociale = parts.Length > 1 ? parts[1] : "";

                        // Trova il contatto specifico
                        var contattoSpecifico = contatti
                            .FirstOrDefault(c =>
                                c.NumeroContatto == numeroContatto &&
                                c.RagioneSociale == ragioneSociale);

                        if (contattoSpecifico != null)
                        {
                            // Filtra le chiamate dove il numero corrisponde E la ragione sociale corrisponde (con logica flessibile)
                            chiamate = chiamate.Where(c =>
                                // Chiamate in entrata (il contatto è il chiamato)
                                (c.NumeroChiamato == numeroContatto &&
                                 (string.IsNullOrEmpty(c.RagioneSocialeChiamato) ||
                                  c.RagioneSocialeChiamato.Contains(ragioneSociale, StringComparison.OrdinalIgnoreCase) ||
                                  ragioneSociale.Contains(c.RagioneSocialeChiamato, StringComparison.OrdinalIgnoreCase))) ||
                                // Chiamate in uscita (il contatto è il chiamante)
                                (c.NumeroChiamante == numeroContatto &&
                                 (string.IsNullOrEmpty(c.RagioneSocialeChiamante) ||
                                  c.RagioneSocialeChiamante.Contains(ragioneSociale, StringComparison.OrdinalIgnoreCase) ||
                                  ragioneSociale.Contains(c.RagioneSocialeChiamante, StringComparison.OrdinalIgnoreCase)))
                            ).ToList();
                        }
                        else
                        {
                            // Se il contatto specifico non viene trovato, non mostrare nessuna chiamata
                            chiamate = new List<Chiamata>();
                        }
                    }
                    else
                    {
                        // Ricerca tradizionale per numero o ragione sociale (per compatibilità)
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
                    }

                    // Calcola le statistiche specifiche per il contatto
                    var chiamateContatto = chiamate.ToList();

                    // Aggiorna le statistiche per il contatto
                    statistiche.TotaleChiamate = chiamateContatto.Count;
                    statistiche.ChiamateInEntrata = chiamateContatto.Count(c => NormalizzaTipoChiamata(c.TipoChiamata) == "entrata");
                    statistiche.ChiamateInUscita = chiamateContatto.Count(c => NormalizzaTipoChiamata(c.TipoChiamata) == "uscita");
                    statistiche.ChiamatePerse = chiamateContatto.Count(c => NormalizzaTipoChiamata(c.TipoChiamata) == "persa");
                    statistiche.ChiamateNonRisposta = chiamateContatto.Count(c => NormalizzaTipoChiamata(c.TipoChiamata) == "non risposta");
                    statistiche.ChiamateManuali = chiamateContatto.Count(c => c.CampoExtra1 == "Manuale");
                    statistiche.ChiamateAutomatiche = chiamateContatto.Count(c => c.CampoExtra1 != "Manuale");

                    // Imposta ChiamateInterne solo se non stiamo usando TipoChiamata per identificare le interne
                    // Altrimenti le chiamate interne sono già contate nel raggruppamento per TipoChiamata
                    //if (!useTipoChiamataForInterni)
                    //{
                    //    statistiche.ChiamateInterne = chiamateInterne.Count;
                    //}
                    //else
                    //{
                    //    // Se usiamo TipoChiamata, le chiamate interne sono già contate nel raggruppamento per tipo
                    //    statistiche.ChiamateInterne = 0;
                    //}
                    statistiche.ChiamateInterne = chiamateInterne.Count;

                    statistiche.DurataTotaleChiamate = chiamateContatto.Sum(c => c.Durata.TotalSeconds);

                    // Calcola la durata media
                    statistiche.DurataMediaChiamate = statistiche.TotaleChiamate > 0
                        ? statistiche.DurataTotaleChiamate / statistiche.TotaleChiamate
                        : 0;

                    // Calcola le durate per chiamate in entrata
                    var chiamateInEntrataContatto = chiamateContatto.Where(c => NormalizzaTipoChiamata(c.TipoChiamata) == "entrata");
                    statistiche.DurataTotaleInEntrata = chiamateInEntrataContatto.Sum(c => c.Durata.TotalSeconds);
                    statistiche.DurataMediaInEntrata = statistiche.ChiamateInEntrata > 0
                        ? statistiche.DurataTotaleInEntrata / statistiche.ChiamateInEntrata
                        : 0;

                    // Calcola le durate per chiamate in uscita
                    var chiamateInUscitaContatto = chiamateContatto.Where(c => NormalizzaTipoChiamata(c.TipoChiamata) == "uscita");
                    statistiche.DurataTotaleInUscita = chiamateInUscitaContatto.Sum(c => c.Durata.TotalSeconds);
                    statistiche.DurataMediaInUscita = statistiche.ChiamateInUscita > 0
                        ? statistiche.DurataTotaleInUscita / statistiche.ChiamateInUscita
                        : 0;

                    // Raggruppa per tipo di chiamata
                    statistiche.ChiamatePerTipo = chiamateContatto
                        .GroupBy(c => NormalizzaTipoChiamata(c.TipoChiamata) ?? "Non specificato")
                        .ToDictionary(g => g.Key, g => g.Count());

                    // Raggruppa per locazione
                    statistiche.ChiamatePerLocazione = chiamateContatto
                        .GroupBy(c => c.Locazione ?? "Non specificato")
                        .ToDictionary(g => g.Key, g => g.Count());

                    // Nuove statistiche basate sulla locazione del chiamante
                    statistiche.ChiamatePerLocazioneChiamante = chiamateContatto
                        .Where(c => !string.IsNullOrEmpty(c.Locazione))
                        .GroupBy(c => c.Locazione ?? "Non specificato")
                        .ToDictionary(g => g.Key, g => g.Count());

                    // Nuove statistiche basate sulla locazione del chiamato
                    statistiche.ChiamatePerLocazioneChiamato = chiamateContatto
                        .Where(c => !string.IsNullOrEmpty(c.LocazioneChiamato))
                        .GroupBy(c => c.LocazioneChiamato ?? "Non specificato")
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
                        .Take(topCount)
                        .ToList();

                    // Top chiamati per il contatto
                    statistiche.TopChiamati = chiamateContatto
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
                        .Take(topCount)
                        .ToList();

                    // Nuove statistiche: Top chiamanti per locazione
                    statistiche.TopChiamantiPerLocazione = chiamateContatto
                        .Where(c => !string.IsNullOrEmpty(c.NumeroChiamante) && !string.IsNullOrEmpty(c.Locazione))
                        .GroupBy(c => new { c.NumeroChiamante, c.RagioneSocialeChiamante, c.Locazione })
                        .Select(g => new TopChiamantePerLocazione
                        {
                            Numero = g.Key.NumeroChiamante ?? "",
                            RagioneSociale = g.Key.RagioneSocialeChiamante ?? "",
                            Locazione = g.Key.Locazione ?? "",
                            NumeroChiamate = g.Count(),
                            DurataTotale = g.Sum(c => c.Durata.TotalSeconds)
                        })
                        .OrderByDescending(x => x.NumeroChiamate)
                        .Take(topCount)
                        .ToList();

                    // Nuove statistiche: Top chiamati per locazione
                    statistiche.TopChiamatiPerLocazione = chiamateContatto
                        .Where(c => !string.IsNullOrEmpty(c.NumeroChiamato) && !string.IsNullOrEmpty(c.LocazioneChiamato))
                        .GroupBy(c => new { c.NumeroChiamato, c.RagioneSocialeChiamato, c.LocazioneChiamato })
                        .Select(g => new TopChiamatoPerLocazione
                        {
                            Numero = g.Key.NumeroChiamato ?? "",
                            RagioneSociale = g.Key.RagioneSocialeChiamato ?? "",
                            Locazione = g.Key.LocazioneChiamato ?? "",
                            NumeroChiamate = g.Count(),
                            DurataTotale = g.Sum(c => c.Durata.TotalSeconds)
                        })
                        .OrderByDescending(x => x.NumeroChiamate)
                        .Take(topCount)
                        .ToList();

                    return statistiche;
                }

                // Se non ci sono filtri di comune o ricerca, usa tutte le chiamate filtrate solo per data e interni
                statistiche.TotaleChiamate = chiamate.Count;
                statistiche.ChiamateInEntrata = chiamate.Count(c => NormalizzaTipoChiamata(c.TipoChiamata) == "entrata");
                statistiche.ChiamateInUscita = chiamate.Count(c => NormalizzaTipoChiamata(c.TipoChiamata) == "uscita");
                statistiche.ChiamatePerse = chiamate.Count(c => NormalizzaTipoChiamata(c.TipoChiamata) == "persa");
                statistiche.ChiamateNonRisposta = chiamate.Count(c => NormalizzaTipoChiamata(c.TipoChiamata) == "non risposta");
                statistiche.ChiamateManuali = chiamate.Count(c => c.CampoExtra1 == "Manuale");
                statistiche.ChiamateAutomatiche = chiamate.Count(c => c.CampoExtra1 != "Manuale");

                // Imposta ChiamateInterne solo se non stiamo usando TipoChiamata per identificare le interne
                // Altrimenti le chiamate interne sono già contate nel raggruppamento per TipoChiamata
                if (!useTipoChiamataForInterni)
                {
                    statistiche.ChiamateInterne = chiamateInterne.Count;
                }
                else
                {
                    // Se usiamo TipoChiamata, le chiamate interne sono già contate nel raggruppamento per tipo
                    statistiche.ChiamateInterne = 0;
                }

                statistiche.DurataTotaleChiamate = chiamate.Sum(c => c.Durata.TotalSeconds);

                // Calcola la durata media
                statistiche.DurataMediaChiamate = statistiche.TotaleChiamate > 0
                    ? statistiche.DurataTotaleChiamate / statistiche.TotaleChiamate
                    : 0;

                // Calcola le durate per chiamate in entrata
                var chiamateInEntrata = chiamate.Where(c => NormalizzaTipoChiamata(c.TipoChiamata) == "entrata");
                statistiche.DurataTotaleInEntrata = chiamateInEntrata.Sum(c => c.Durata.TotalSeconds);
                statistiche.DurataMediaInEntrata = statistiche.ChiamateInEntrata > 0
                    ? statistiche.DurataTotaleInEntrata / statistiche.ChiamateInEntrata
                    : 0;

                // Calcola le durate per chiamate in uscita
                var chiamateInUscita = chiamate.Where(c => NormalizzaTipoChiamata(c.TipoChiamata) == "uscita");
                statistiche.DurataTotaleInUscita = chiamateInUscita.Sum(c => c.Durata.TotalSeconds);
                statistiche.DurataMediaInUscita = statistiche.ChiamateInUscita > 0
                    ? statistiche.DurataTotaleInUscita / statistiche.ChiamateInUscita
                    : 0;

                // Raggruppa per tipo di chiamata
                statistiche.ChiamatePerTipo = chiamate
                    .GroupBy(c => NormalizzaTipoChiamata(c.TipoChiamata) ?? "Non specificato")
                    .ToDictionary(g => g.Key, g => g.Count());

                // Raggruppa per locazione
                statistiche.ChiamatePerLocazione = chiamate
                    .GroupBy(c => c.Locazione ?? "Non specificato")
                    .ToDictionary(g => g.Key, g => g.Count());

                // Nuove statistiche basate sulla locazione del chiamante
                statistiche.ChiamatePerLocazioneChiamante = chiamate
                    .Where(c => !string.IsNullOrEmpty(c.Locazione))
                    .GroupBy(c => c.Locazione ?? "Non specificato")
                    .ToDictionary(g => g.Key, g => g.Count());

                // Nuove statistiche basate sulla locazione del chiamato
                statistiche.ChiamatePerLocazioneChiamato = chiamate
                    .Where(c => !string.IsNullOrEmpty(c.LocazioneChiamato))
                    .GroupBy(c => c.LocazioneChiamato ?? "Non specificato")
                    .ToDictionary(g => g.Key, g => g.Count());

                // Raggruppa per giorno
                statistiche.ChiamatePerGiorno = chiamate
                    .GroupBy(c => c.DataArrivoChiamata.ToString("dd/MM/yyyy"))
                    .ToDictionary(g => g.Key, g => g.Count());

                // Raggruppa per ora
                statistiche.ChiamatePerOra = chiamate
                    .GroupBy(c => c.DataArrivoChiamata.ToString("HH:00"))
                    .ToDictionary(g => g.Key, g => g.Count());

                // Top chiamanti
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
                    .Take(topCount)
                    .ToList();

                // Top chiamati
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
                    .Take(topCount)
                    .ToList();

                // Nuove statistiche: Top chiamanti per locazione
                statistiche.TopChiamantiPerLocazione = chiamate
                    .Where(c => !string.IsNullOrEmpty(c.NumeroChiamante) && !string.IsNullOrEmpty(c.Locazione))
                    .GroupBy(c => new { c.NumeroChiamante, c.RagioneSocialeChiamante, c.Locazione })
                    .Select(g => new TopChiamantePerLocazione
                    {
                        Numero = g.Key.NumeroChiamante ?? "",
                        RagioneSociale = g.Key.RagioneSocialeChiamante ?? "",
                        Locazione = g.Key.Locazione ?? "",
                        NumeroChiamate = g.Count(),
                        DurataTotale = g.Sum(c => c.Durata.TotalSeconds)
                    })
                    .OrderByDescending(x => x.NumeroChiamate)
                    .Take(topCount)
                    .ToList();

                // Nuove statistiche: Top chiamati per locazione
                statistiche.TopChiamatiPerLocazione = chiamate
                    .Where(c => !string.IsNullOrEmpty(c.NumeroChiamato) && !string.IsNullOrEmpty(c.LocazioneChiamato))
                    .GroupBy(c => new { c.NumeroChiamato, c.RagioneSocialeChiamato, c.LocazioneChiamato })
                    .Select(g => new TopChiamatoPerLocazione
                    {
                        Numero = g.Key.NumeroChiamato ?? "",
                        RagioneSociale = g.Key.RagioneSocialeChiamato ?? "",
                        Locazione = g.Key.LocazioneChiamato ?? "",
                        NumeroChiamate = g.Count(),
                        DurataTotale = g.Sum(c => c.Durata.TotalSeconds)
                    })
                    .OrderByDescending(x => x.NumeroChiamate)
                    .Take(topCount)
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
        public async Task<ContactStatistics> GetContactStatisticsAsync(string numeroContatto, DateTime? fromDate = null, DateTime? toDate = null, bool useTipoChiamataForInterni = true)
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
                        NormalizzaTipoChiamata(c.TipoChiamata) == "persa"),
                    ChiamateNonRisposta = chiamateContatto.Count(c =>
                        (c.NumeroChiamato == numeroContatto || c.NumeroChiamante == numeroContatto) &&
                        NormalizzaTipoChiamata(c.TipoChiamata) == "non risposta"),
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

                // Nuove statistiche basate sulla locazione per contatto specifico
                // Statistiche per locazione del chiamante (quando il contatto è chiamato)
                statistiche.ChiamatePerLocazioneChiamante = chiamateContatto
                    .Where(c => c.NumeroChiamato == numeroContatto && !string.IsNullOrEmpty(c.Locazione))
                    .GroupBy(c => c.Locazione ?? "Non specificato")
                    .ToDictionary(g => g.Key, g => g.Count());

                // Statistiche per locazione del chiamato (quando il contatto è chiamante)
                statistiche.ChiamatePerLocazioneChiamato = chiamateContatto
                    .Where(c => c.NumeroChiamante == numeroContatto && !string.IsNullOrEmpty(c.LocazioneChiamato))
                    .GroupBy(c => c.LocazioneChiamato ?? "Non specificato")
                    .ToDictionary(g => g.Key, g => g.Count());

                // Top chiamanti per locazione (quando il contatto è chiamato)
                statistiche.TopChiamantiPerLocazione = chiamateContatto
                    .Where(c => c.NumeroChiamato == numeroContatto &&
                               !string.IsNullOrEmpty(c.NumeroChiamante) &&
                               !string.IsNullOrEmpty(c.Locazione))
                    .GroupBy(c => new { c.NumeroChiamante, c.RagioneSocialeChiamante, c.Locazione })
                    .Select(g => new TopChiamantePerLocazione
                    {
                        Numero = g.Key.NumeroChiamante ?? "",
                        RagioneSociale = g.Key.RagioneSocialeChiamante ?? "",
                        Locazione = g.Key.Locazione ?? "",
                        NumeroChiamate = g.Count(),
                        DurataTotale = g.Sum(c => c.Durata.TotalSeconds)
                    })
                    .OrderByDescending(x => x.NumeroChiamate)
                    .Take(10)
                    .ToList();

                // Top chiamati per locazione (quando il contatto è chiamante)
                statistiche.TopChiamatiPerLocazione = chiamateContatto
                    .Where(c => c.NumeroChiamante == numeroContatto &&
                               !string.IsNullOrEmpty(c.NumeroChiamato) &&
                               !string.IsNullOrEmpty(c.LocazioneChiamato))
                    .GroupBy(c => new { c.NumeroChiamato, c.RagioneSocialeChiamato, c.LocazioneChiamato })
                    .Select(g => new TopChiamatoPerLocazione
                    {
                        Numero = g.Key.NumeroChiamato ?? "",
                        RagioneSociale = g.Key.RagioneSocialeChiamato ?? "",
                        Locazione = g.Key.LocazioneChiamato ?? "",
                        NumeroChiamate = g.Count(),
                        DurataTotale = g.Sum(c => c.Durata.TotalSeconds)
                    })
                    .OrderByDescending(x => x.NumeroChiamate)
                    .Take(10)
                    .ToList();

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

        // Normalizza il tipo chiamata per statistiche e raggruppamenti
        private string NormalizzaTipoChiamata(string? tipo)
        {
            if (string.IsNullOrEmpty(tipo)) return "non specificato";
            tipo = tipo.ToLower();
            if (tipo == "entrata+trasferimento") return "entrata";
            if (tipo == "uscita+trasferimento") return "uscita";
            return tipo;
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
