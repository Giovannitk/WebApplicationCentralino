using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WebApplicationCentralino.Models;
using WebApplicationCentralino.Services;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System.Collections.Generic;
using WebApplicationCentralino.Extensions;
using WebApplicationCentralino.Managers;
using ClosedXML.Excel;
using System.Text;
using System.Globalization;

namespace WebApplicationCentralino.Controllers
{
    /// <summary>
    /// Controller principale dell'applicazione che gestisce la home page e le statistiche
    /// </summary>
    [Authorize] // Questo consente di accedere alla Home solo previa autorizzazione tramite login
    public class HomeController : BaseController
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ChiamataService _chiamataService;
        private readonly ContattoService _contattoService;

        public HomeController(ILogger<HomeController> logger, ChiamataService chiamataService, ContattoService contattoService)
        {
            _logger = logger;
            _chiamataService = chiamataService;
            _contattoService = contattoService;
        }

        /// <summary>
        /// Visualizza la home page con le statistiche delle chiamate e dei contatti
        /// </summary>
        public async Task<IActionResult> Index()
        {
            try
            {
                // Recupera le statistiche delle chiamate e dei contatti
                var statistiche = await _chiamataService.GetCallStatisticsAsync();
                ViewBag.Statistiche = statistiche;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il recupero delle statistiche");
            }
            return View();
        }

        /// <summary>
        /// Visualizza la lista dei contatti incompleti (senza ragione sociale o con dati mancanti)
        /// </summary>
        public IActionResult ContattiIncompleti()
        {
            try
            {
                // Recupera i contatti incompleti dalla cache
                var contattiIncompleti = _chiamataService.GetIncompleteContacts();
                return View(contattiIncompleti);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il recupero dei contatti incompleti");
                return RedirectToAction("Index");
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        /// <summary>
        /// Visualizza le statistiche dettagliate delle chiamate con possibilità di filtraggio
        /// </summary>
        public async Task<IActionResult> StatisticheDettagliate(string? dateFrom = null, string? dateTo = null, bool includeInterni = false, string? comune = null, string? searchContatto = null)
        {
            try
            {
                DateTime? fromDateParsed = null;
                DateTime? toDateParsed = null;
                var oggi = DateTime.Today;

                // Parsing dei parametri di data
                if (!string.IsNullOrEmpty(dateFrom) && DateTimeExtensions.TryParseWithYearHandling(dateFrom, out DateTime fromDate))
                {
                    // Se l'anno è inferiore al 2020, usa l'anno corrente
                    if (fromDate.Year < 2020)
                    {
                        fromDate = new DateTime(oggi.Year, fromDate.Month, fromDate.Day, fromDate.Hour, fromDate.Minute, fromDate.Second);
                    }
                    fromDateParsed = fromDate;
                    dateFrom = fromDate.ToString("yyyy-MM-dd");
                }
                else
                {
                    // Se non specificato, usa l'inizio del mese corrente
                    fromDateParsed = new DateTime(oggi.Year, oggi.Month, 1);
                    dateFrom = fromDateParsed.Value.ToString("yyyy-MM-dd");
                }

                if (!string.IsNullOrEmpty(dateTo) && DateTimeExtensions.TryParseWithYearHandling(dateTo, out DateTime toDate))
                {
                    // Se l'anno è inferiore al 2020, usa l'anno corrente
                    if (toDate.Year < 2020)
                    {
                        toDate = new DateTime(oggi.Year, toDate.Month, toDate.Day, toDate.Hour, toDate.Minute, toDate.Second);
                    }
                    toDateParsed = toDate.AddDays(1).AddSeconds(-1);
                    dateTo = toDate.ToString("yyyy-MM-dd");
                }
                else
                {
                    // Se non specificato, usa oggi
                    toDateParsed = oggi.AddDays(1).AddSeconds(-1);
                    dateTo = oggi.ToString("yyyy-MM-dd");
                }

                // Recupera le statistiche dettagliate
                var statistiche = await _chiamataService.GetDetailedStatisticsAsync(fromDateParsed, toDateParsed, includeInterni, comune, searchContatto);
                
                // Lista predefinita dei comuni
                ViewBag.Comuni = ComuniManager.GetComuniList();
                ViewBag.SelectedComune = comune;
                ViewBag.SearchContatto = searchContatto;
                ViewBag.DateFrom = dateFrom;
                ViewBag.DateTo = dateTo;
                ViewBag.IncludeInterni = includeInterni;
                
                return View(statistiche);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il recupero delle statistiche dettagliate");
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Visualizza le statistiche dettagliate per i comuni
        /// </summary>
        public async Task<IActionResult> StatisticheComuni(string? dateFrom = null, string? dateTo = null, bool includeInterni = false, string? selectedComune = null)
        {
            try
            {
                DateTime? fromDateParsed = null;
                DateTime? toDateParsed = null;
                var oggi = DateTime.Today;

                // Parsing dei parametri di data
                if (!string.IsNullOrEmpty(dateFrom) && DateTimeExtensions.TryParseWithYearHandling(dateFrom, out DateTime fromDate))
                {
                    if (fromDate.Year < 2020)
                    {
                        fromDate = new DateTime(oggi.Year, fromDate.Month, fromDate.Day);
                    }
                    fromDateParsed = fromDate;
                    dateFrom = fromDate.ToString("yyyy-MM-dd");
                }
                else
                {
                    fromDateParsed = new DateTime(oggi.Year, oggi.Month, 1);
                    dateFrom = fromDateParsed.Value.ToString("yyyy-MM-dd");
                }

                if (!string.IsNullOrEmpty(dateTo) && DateTimeExtensions.TryParseWithYearHandling(dateTo, out DateTime toDate))
                {
                    if (toDate.Year < 2020)
                    {
                        toDate = new DateTime(oggi.Year, toDate.Month, toDate.Day);
                    }
                    toDateParsed = toDate.AddDays(1).AddSeconds(-1);
                    dateTo = toDate.ToString("yyyy-MM-dd");
                }
                else
                {
                    toDateParsed = oggi.AddDays(1).AddSeconds(-1);
                    dateTo = oggi.ToString("yyyy-MM-dd");
                }

                // Recupera le statistiche dettagliate
                var statistiche = await _chiamataService.GetDetailedStatisticsAsync(fromDateParsed, toDateParsed, includeInterni, selectedComune);
                
                // Lista predefinita dei comuni
                ViewBag.Comuni = ComuniManager.GetComuniList();
                ViewBag.SelectedComune = selectedComune;
                ViewBag.DateFrom = dateFrom;
                ViewBag.DateTo = dateTo;
                ViewBag.IncludeInterni = includeInterni;
                
                return View(statistiche);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il recupero delle statistiche dei comuni");
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Visualizza le statistiche dettagliate per un contatto specifico
        /// </summary>
        public async Task<IActionResult> StatisticheContatto(string numeroContatto, string? dateFrom = null, string? dateTo = null)
        {
            try
            {
                DateTime? fromDateParsed = null;
                DateTime? toDateParsed = null;
                var oggi = DateTime.Today;

                // Parsing dei parametri di data
                if (!string.IsNullOrEmpty(dateFrom) && DateTimeExtensions.TryParseWithYearHandling(dateFrom, out DateTime fromDate))
                {
                    // Se l'anno è inferiore al 2020, usa l'anno corrente
                    if (fromDate.Year < 2020)
                    {
                        fromDate = new DateTime(oggi.Year, fromDate.Month, fromDate.Day, fromDate.Hour, fromDate.Minute, fromDate.Second);
                    }
                    fromDateParsed = fromDate;
                    dateFrom = fromDate.ToString("yyyy-MM-dd");
                }
                else
                {
                    // Se non specificato, usa l'inizio del mese corrente
                    fromDateParsed = new DateTime(oggi.Year, oggi.Month, 1);
                    dateFrom = fromDateParsed.Value.ToString("yyyy-MM-dd");
                }

                if (!string.IsNullOrEmpty(dateTo) && DateTimeExtensions.TryParseWithYearHandling(dateTo, out DateTime toDate))
                {
                    // Se l'anno è inferiore al 2020, usa l'anno corrente
                    if (toDate.Year < 2020)
                    {
                        toDate = new DateTime(oggi.Year, toDate.Month, toDate.Day, toDate.Hour, toDate.Minute, toDate.Second);
                    }
                    toDateParsed = toDate.AddDays(1).AddSeconds(-1);
                    dateTo = toDate.ToString("yyyy-MM-dd");
                }
                else
                {
                    // Se non specificato, usa oggi
                    toDateParsed = oggi.AddDays(1).AddSeconds(-1);
                    dateTo = oggi.ToString("yyyy-MM-dd");
                }

                // Recupera le statistiche del contatto
                var statistiche = await _chiamataService.GetContactStatisticsAsync(numeroContatto, fromDateParsed, toDateParsed);
                
                // Passa i valori alla vista per mantenere i filtri
                ViewBag.DateFrom = dateFrom;
                ViewBag.DateTo = dateTo;
                ViewBag.NumeroContatto = numeroContatto;
                
                return View(statistiche);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il recupero delle statistiche del contatto");
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public async Task<IActionResult> VerificaContatto(string searchContatto)
        {
            try
            {
                var contatti = await _contattoService.GetAllAsync();
                var results = contatti
                    .Where(c => 
                        (c.NumeroContatto != null && c.NumeroContatto.Contains(searchContatto, StringComparison.OrdinalIgnoreCase)) ||
                        (c.RagioneSociale != null && c.RagioneSociale.Contains(searchContatto, StringComparison.OrdinalIgnoreCase)))
                    .Select(c => new {
                        id = c.NumeroContatto,
                        text = $"{c.NumeroContatto} - {c.RagioneSociale}"
                    })
                    .ToList();
                
                return Json(new { results = results });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la verifica del contatto");
                return Json(new { results = new List<object>() });
            }
        }

        private DateTime? ParseDate(string? dateStr)
        {
            if (string.IsNullOrEmpty(dateStr))
                return null;

            // Prova prima il formato dd/MM/yyyy
            if (DateTime.TryParseExact(dateStr, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime result))
                return result;

            // Se fallisce, prova il formato yyyy-MM-dd
            if (DateTime.TryParseExact(dateStr, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out result))
                return result;

            // Se entrambi falliscono, prova il parsing standard
            if (DateTime.TryParse(dateStr, out result))
                return result;

            return null;
        }

        public async Task<IActionResult> GetTopChiamanti(string searchContatto, int count = 10, int page = 1, string? dateFrom = null, string? dateTo = null, string? comune = null, bool includeInterni = false, string sortField = "numeroChiamate", string sortDirection = "desc")
        {
            try
            {
                var fromDateParsed = ParseDate(dateFrom);
                var toDateParsed = ParseDate(dateTo);
                if (toDateParsed.HasValue)
                {
                    toDateParsed = toDateParsed.Value.AddDays(1).AddSeconds(-1);
                }

                var statistiche = await _chiamataService.GetDetailedStatisticsAsync(fromDateParsed, toDateParsed, includeInterni, comune, searchContatto, count);

                if (statistiche == null)
                {
                    _logger.LogWarning("GetDetailedStatisticsAsync ha restituito null");
                    return Json(new { data = new List<object>(), totalPages = 1 });
                }

                // Applica l'ordinamento
                var topChiamanti = statistiche.TopChiamanti?.AsQueryable();
                if (topChiamanti == null)
                {
                    _logger.LogWarning("TopChiamanti è null");
                    return Json(new { data = new List<object>(), totalPages = 1 });
                }

                topChiamanti = ApplySorting(topChiamanti, sortField, sortDirection);

                // Applica la paginazione
                var pageSize = 10;
                var totalPages = (int)Math.Ceiling(count / (double)pageSize);
                var pagedResults = topChiamanti
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(c => new {
                        numero = c.Numero ?? "",
                        ragioneSociale = c.RagioneSociale ?? "",
                        numeroChiamate = c.NumeroChiamate,
                        durataTotale = c.DurataTotale,
                        durataMedia = c.DurataMedia
                    })
                    .ToList();

                return Json(new { data = pagedResults, totalPages });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il recupero dei top chiamanti. Parametri: searchContatto={SearchContatto}, count={Count}, page={Page}, dateFrom={DateFrom}, dateTo={DateTo}, comune={Comune}, includeInterni={IncludeInterni}, sortField={SortField}, sortDirection={SortDirection}",
                    searchContatto, count, page, dateFrom, dateTo, comune, includeInterni, sortField, sortDirection);
                return Json(new { error = "Errore durante il recupero dei dati", details = ex.Message });
            }
        }

        public async Task<IActionResult> GetTopChiamati(string searchContatto, int count = 10, int page = 1, string? dateFrom = null, string? dateTo = null, string? comune = null, bool includeInterni = false, string sortField = "numeroChiamate", string sortDirection = "desc")
        {
            try
            {
                var fromDateParsed = ParseDate(dateFrom);
                var toDateParsed = ParseDate(dateTo);
                if (toDateParsed.HasValue)
                {
                    toDateParsed = toDateParsed.Value.AddDays(1).AddSeconds(-1);
                }

                var statistiche = await _chiamataService.GetDetailedStatisticsAsync(fromDateParsed, toDateParsed, includeInterni, comune, searchContatto, count);

                if (statistiche == null)
                {
                    _logger.LogWarning("GetDetailedStatisticsAsync ha restituito null");
                    return Json(new { data = new List<object>(), totalPages = 1 });
                }

                // Applica l'ordinamento
                var topChiamati = statistiche.TopChiamati?.AsQueryable();
                if (topChiamati == null)
                {
                    _logger.LogWarning("TopChiamati è null");
                    return Json(new { data = new List<object>(), totalPages = 1 });
                }

                topChiamati = ApplySorting(topChiamati, sortField, sortDirection);

                // Applica la paginazione
                var pageSize = 10;
                var totalPages = (int)Math.Ceiling(count / (double)pageSize);
                var pagedResults = topChiamati
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(c => new {
                        numero = c.Numero ?? "",
                        ragioneSociale = c.RagioneSociale ?? "",
                        numeroChiamate = c.NumeroChiamate,
                        durataTotale = c.DurataTotale,
                        durataMedia = c.DurataMedia
                    })
                    .ToList();

                return Json(new { data = pagedResults, totalPages });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il recupero dei top chiamati. Parametri: searchContatto={SearchContatto}, count={Count}, page={Page}, dateFrom={DateFrom}, dateTo={DateTo}, comune={Comune}, includeInterni={IncludeInterni}, sortField={SortField}, sortDirection={SortDirection}",
                    searchContatto, count, page, dateFrom, dateTo, comune, includeInterni, sortField, sortDirection);
                return Json(new { error = "Errore durante il recupero dei dati", details = ex.Message });
            }
        }

        private IQueryable<T> ApplySorting<T>(IQueryable<T> query, string sortField, string sortDirection)
        {
            if (string.IsNullOrEmpty(sortField))
                return query;

            var propertyInfo = typeof(T).GetProperty(sortField);
            if (propertyInfo == null)
                return query;

            if (sortDirection.ToLower() == "asc")
            {
                return query.OrderBy(x => propertyInfo.GetValue(x, null));
            }
            else
            {
                return query.OrderByDescending(x => propertyInfo.GetValue(x, null));
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public async Task<IActionResult> ExportStatistics(DateTime? dateFrom, DateTime? dateTo, bool includeInterni, string? comune, string? searchContatto)
        {
            try
            {
                // Ottieni le statistiche con un numero molto alto per ottenere tutte le liste
                var statistics = await _chiamataService.GetDetailedStatisticsAsync(dateFrom, dateTo, includeInterni, comune, searchContatto, 999999);

                using (var workbook = new XLWorkbook())
                {
                    // Foglio Riepilogo
                    var wsSummary = workbook.Worksheets.Add("Riepilogo");
                    wsSummary.Cell("A1").Value = (XLCellValue)"Statistiche Generali";
                    wsSummary.Cell("A1").Style.Font.Bold = true;
                    wsSummary.Cell("A1").Style.Font.FontSize = 16;

                    // Informazioni sui filtri applicati
                    wsSummary.Cell("A3").Value = (XLCellValue)"Filtri Applicati";
                    wsSummary.Cell("A3").Style.Font.Bold = true;
                    wsSummary.Cell("A3").Style.Font.FontSize = 12;
                    
                    wsSummary.Cell("A4").Value = (XLCellValue)"Periodo:";
                    wsSummary.Cell("B4").Value = (XLCellValue)$"Da {dateFrom?.ToString("dd/MM/yyyy") ?? "Non specificato"} a {dateTo?.ToString("dd/MM/yyyy") ?? "Non specificato"}";
                    
                    wsSummary.Cell("A5").Value = (XLCellValue)"Chiamate Interne:";
                    wsSummary.Cell("B5").Value = (XLCellValue)(includeInterni ? "Incluse" : "Escluse");
                    
                    if (!string.IsNullOrEmpty(comune))
                    {
                        wsSummary.Cell("A6").Value = (XLCellValue)"Comune:";
                        wsSummary.Cell("B6").Value = (XLCellValue)comune;
                    }
                    
                    if (!string.IsNullOrEmpty(searchContatto))
                    {
                        wsSummary.Cell("A7").Value = (XLCellValue)"Contatto:";
                        wsSummary.Cell("B7").Value = (XLCellValue)searchContatto;
                    }

                    // Statistiche principali
                    int startRow = 9;
                    wsSummary.Cell($"A{startRow}").Value = (XLCellValue)"Statistiche Principali";
                    wsSummary.Cell($"A{startRow}").Style.Font.Bold = true;
                    wsSummary.Cell($"A{startRow}").Style.Font.FontSize = 12;

                    // Calcola le percentuali
                    double percentualeInEntrata = statistics.TotaleChiamate > 0 ? (statistics.ChiamateInEntrata * 100.0 / statistics.TotaleChiamate) : 0;
                    double percentualeInUscita = statistics.TotaleChiamate > 0 ? (statistics.ChiamateInUscita * 100.0 / statistics.TotaleChiamate) : 0;
                    double percentualePerse = statistics.TotaleChiamate > 0 ? (statistics.ChiamatePerse * 100.0 / statistics.TotaleChiamate) : 0;
                    double percentualeNonRisposta = statistics.TotaleChiamate > 0 ? (statistics.ChiamateNonRisposta * 100.0 / statistics.TotaleChiamate) : 0;

                    var summaryData = new (string Metric, object Value)[]
                    {
                        ("Numero Totale Chiamate", statistics.TotaleChiamate),
                        ("Chiamate In Entrata", $"{statistics.ChiamateInEntrata} ({percentualeInEntrata:F1}%)"),
                        ("Chiamate In Uscita", $"{statistics.ChiamateInUscita} ({percentualeInUscita:F1}%)"),
                        ("Chiamate Perse", $"{statistics.ChiamatePerse} ({percentualePerse:F1}%)"),
                        ("Chiamate Non Risposta", $"{statistics.ChiamateNonRisposta} ({percentualeNonRisposta:F1}%)"),
                        ("Chiamate Manuali", statistics.ChiamateManuali),
                        ("Chiamate Automatiche", statistics.ChiamateAutomatiche),
                        ("Durata Media", FormatDuration(statistics.DurataMediaChiamate)),
                        ("Durata Totale", FormatDuration(statistics.DurataTotaleChiamate)),
                        ("Durata Media In Entrata", FormatDuration(statistics.DurataMediaInEntrata)),
                        ("Durata Media In Uscita", FormatDuration(statistics.DurataMediaInUscita)),
                        ("Durata Totale In Entrata", FormatDuration(statistics.DurataTotaleInEntrata)),
                        ("Durata Totale In Uscita", FormatDuration(statistics.DurataTotaleInUscita))
                    };

                    wsSummary.Cell($"A{startRow + 2}").Value = (XLCellValue)"Metrica";
                    wsSummary.Cell($"B{startRow + 2}").Value = (XLCellValue)"Valore";
                    wsSummary.Cell($"A{startRow + 2}").Style.Font.Bold = true;
                    wsSummary.Cell($"B{startRow + 2}").Style.Font.Bold = true;
                    
                    for (int i = 0; i < summaryData.Length; i++)
                    {
                        wsSummary.Cell($"A{startRow + 3 + i}").Value = (XLCellValue)summaryData[i].Metric;
                        wsSummary.Cell($"B{startRow + 3 + i}").Value = (XLCellValue)summaryData[i].Value.ToString();
                    }

                    // Foglio Distribuzione Tipo
                    var wsTipo = workbook.Worksheets.Add("Distribuzione Tipo");
                    wsTipo.Cell("A1").Value = "Distribuzione per Tipo di Chiamata";
                    wsTipo.Cell("A1").Style.Font.Bold = true;
                    wsTipo.Cell("A1").Style.Font.FontSize = 14;
                    
                    wsTipo.Cell("A3").Value = "Tipo Chiamata";
                    wsTipo.Cell("B3").Value = "Numero Chiamate";
                    wsTipo.Cell("C3").Value = "Percentuale";
                    wsTipo.Cell("D3").Value = "Durata Totale";
                    wsTipo.Cell("E3").Value = "Durata Media";
                    
                    wsTipo.Cell("A3").Style.Font.Bold = true;
                    wsTipo.Cell("B3").Style.Font.Bold = true;
                    wsTipo.Cell("C3").Style.Font.Bold = true;
                    wsTipo.Cell("D3").Style.Font.Bold = true;
                    wsTipo.Cell("E3").Style.Font.Bold = true;

                    int row = 4;
                    foreach (var tipo in statistics.ChiamatePerTipo)
                    {
                        double percentuale = statistics.TotaleChiamate > 0 ? (tipo.Value * 100.0 / statistics.TotaleChiamate) : 0;
                        wsTipo.Cell($"A{row}").Value = tipo.Key;
                        wsTipo.Cell($"B{row}").Value = tipo.Value;
                        wsTipo.Cell($"C{row}").Value = $"{percentuale.ToString("F2", CultureInfo.InvariantCulture)}%";
                        
                        // Calcola durata per tipo (approssimativa)
                        double durataTotaleTipo = tipo.Value * statistics.DurataMediaChiamate;
                        double durataMediaTipo = statistics.DurataMediaChiamate;
                        wsTipo.Cell($"D{row}").Value = FormatDuration(durataTotaleTipo);
                        wsTipo.Cell($"E{row}").Value = FormatDuration(durataMediaTipo);
                        row++;
                    }

                    // Foglio Distribuzione Locazione
                    var wsLoc = workbook.Worksheets.Add("Distribuzione Locazione");
                    wsLoc.Cell("A1").Value = "Distribuzione per Locazione";
                    wsLoc.Cell("A1").Style.Font.Bold = true;
                    wsLoc.Cell("A1").Style.Font.FontSize = 14;
                    
                    wsLoc.Cell("A3").Value = "Locazione";
                    wsLoc.Cell("B3").Value = "Numero Chiamate";
                    wsLoc.Cell("C3").Value = "Percentuale";
                    wsLoc.Cell("D3").Value = "Durata Totale";
                    wsLoc.Cell("E3").Value = "Durata Media";
                    
                    wsLoc.Cell("A3").Style.Font.Bold = true;
                    wsLoc.Cell("B3").Style.Font.Bold = true;
                    wsLoc.Cell("C3").Style.Font.Bold = true;
                    wsLoc.Cell("D3").Style.Font.Bold = true;
                    wsLoc.Cell("E3").Style.Font.Bold = true;

                    row = 4;
                    foreach (var loc in statistics.ChiamatePerLocazione)
                    {
                        double percentuale = statistics.TotaleChiamate > 0 ? (loc.Value * 100.0 / statistics.TotaleChiamate) : 0;
                        wsLoc.Cell($"A{row}").Value = loc.Key;
                        wsLoc.Cell($"B{row}").Value = loc.Value;
                        wsLoc.Cell($"C{row}").Value = $"{percentuale.ToString("F2", CultureInfo.InvariantCulture)}%";
                        
                        // Calcola durata per locazione (approssimativa)
                        double durataTotaleLoc = loc.Value * statistics.DurataMediaChiamate;
                        double durataMediaLoc = statistics.DurataMediaChiamate;
                        wsLoc.Cell($"D{row}").Value = FormatDuration(durataTotaleLoc);
                        wsLoc.Cell($"E{row}").Value = FormatDuration(durataMediaLoc);
                        row++;
                    }

                    // Foglio Distribuzione Giornaliera
                    var wsDaily = workbook.Worksheets.Add("Distribuzione Giornaliera");
                    wsDaily.Cell("A1").Value = "Distribuzione Giornaliera";
                    wsDaily.Cell("A1").Style.Font.Bold = true;
                    wsDaily.Cell("A1").Style.Font.FontSize = 14;
                    
                    wsDaily.Cell("A3").Value = "Data";
                    wsDaily.Cell("B3").Value = "Numero Chiamate";
                    wsDaily.Cell("C3").Value = "Durata Totale";
                    wsDaily.Cell("D3").Value = "Durata Media";
                    wsDaily.Cell("E3").Value = "Chiamate In Entrata";
                    wsDaily.Cell("F3").Value = "Chiamate In Uscita";
                    
                    wsDaily.Cell("A3").Style.Font.Bold = true;
                    wsDaily.Cell("B3").Style.Font.Bold = true;
                    wsDaily.Cell("C3").Style.Font.Bold = true;
                    wsDaily.Cell("D3").Style.Font.Bold = true;
                    wsDaily.Cell("E3").Style.Font.Bold = true;
                    wsDaily.Cell("F3").Style.Font.Bold = true;

                    row = 4;
                    foreach (var daily in statistics.ChiamatePerGiorno)
                    {
                        DateTime date = DateTime.ParseExact(daily.Key, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                        wsDaily.Cell($"A{row}").Value = date;
                        wsDaily.Cell($"A{row}").Style.DateFormat.Format = "dd/MM/yyyy";
                        wsDaily.Cell($"B{row}").Value = daily.Value;
                        
                        // Calcola durata per giorno (approssimativa)
                        double durataTotaleGiorno = daily.Value * statistics.DurataMediaChiamate;
                        double durataMediaGiorno = statistics.DurataMediaChiamate;
                        wsDaily.Cell($"C{row}").Value = FormatDuration(durataTotaleGiorno);
                        wsDaily.Cell($"D{row}").Value = FormatDuration(durataMediaGiorno);
                        
                        // Approssimazione chiamate in entrata/uscita per giorno
                        int chiamateInEntrataGiorno = (int)(daily.Value * (statistics.ChiamateInEntrata / (double)statistics.TotaleChiamate));
                        int chiamateInUscitaGiorno = daily.Value - chiamateInEntrataGiorno;
                        wsDaily.Cell($"E{row}").Value = chiamateInEntrataGiorno;
                        wsDaily.Cell($"F{row}").Value = chiamateInUscitaGiorno;
                        row++;
                    }

                    // Foglio Distribuzione Oraria (se disponibile)
                    if (statistics.ChiamatePerOra != null && statistics.ChiamatePerOra.Any())
                    {
                        var wsHourly = workbook.Worksheets.Add("Distribuzione Oraria");
                        wsHourly.Cell("A1").Value = "Distribuzione Oraria";
                        wsHourly.Cell("A1").Style.Font.Bold = true;
                        wsHourly.Cell("A1").Style.Font.FontSize = 14;
                        
                        wsHourly.Cell("A3").Value = "Ora";
                        wsHourly.Cell("B3").Value = "Numero Chiamate";
                        wsHourly.Cell("C3").Value = "Percentuale";
                        wsHourly.Cell("D3").Value = "Durata Totale";
                        wsHourly.Cell("E3").Value = "Durata Media";
                        
                        wsHourly.Cell("A3").Style.Font.Bold = true;
                        wsHourly.Cell("B3").Style.Font.Bold = true;
                        wsHourly.Cell("C3").Style.Font.Bold = true;
                        wsHourly.Cell("D3").Style.Font.Bold = true;
                        wsHourly.Cell("E3").Style.Font.Bold = true;

                        row = 4;
                        foreach (var hourly in statistics.ChiamatePerOra.OrderBy(x => x.Key))
                        {
                            double percentuale = statistics.TotaleChiamate > 0 ? (hourly.Value * 100.0 / statistics.TotaleChiamate) : 0;
                            wsHourly.Cell($"A{row}").Value = $"{hourly.Key}:00";
                            wsHourly.Cell($"B{row}").Value = hourly.Value;
                            wsHourly.Cell($"C{row}").Value = $"{percentuale.ToString("F2", CultureInfo.InvariantCulture)}%";
                            
                            // Calcola durata per ora (approssimativa)
                            double durataTotaleOra = hourly.Value * statistics.DurataMediaChiamate;
                            double durataMediaOra = statistics.DurataMediaChiamate;
                            wsHourly.Cell($"D{row}").Value = FormatDuration(durataTotaleOra);
                            wsHourly.Cell($"E{row}").Value = FormatDuration(durataMediaOra);
                            row++;
                        }
                    }

                    // Foglio Top 10 Chiamanti
                    var wsTop10Callers = workbook.Worksheets.Add("Top 10 Chiamanti");
                    wsTop10Callers.Cell("A1").Value = "Top 10 Chiamanti";
                    wsTop10Callers.Cell("A1").Style.Font.Bold = true;
                    wsTop10Callers.Cell("A1").Style.Font.FontSize = 14;
                    
                    wsTop10Callers.Cell("A3").Value = "Posizione";
                    wsTop10Callers.Cell("B3").Value = "Numero";
                    wsTop10Callers.Cell("C3").Value = "Ragione Sociale";
                    wsTop10Callers.Cell("D3").Value = "Numero Chiamate";
                    wsTop10Callers.Cell("E3").Value = "Durata Totale";
                    wsTop10Callers.Cell("F3").Value = "Durata Media";
                    wsTop10Callers.Cell("G3").Value = "Percentuale Chiamate";
                    
                    wsTop10Callers.Cell("A3").Style.Font.Bold = true;
                    wsTop10Callers.Cell("B3").Style.Font.Bold = true;
                    wsTop10Callers.Cell("C3").Style.Font.Bold = true;
                    wsTop10Callers.Cell("D3").Style.Font.Bold = true;
                    wsTop10Callers.Cell("E3").Style.Font.Bold = true;
                    wsTop10Callers.Cell("F3").Style.Font.Bold = true;
                    wsTop10Callers.Cell("G3").Style.Font.Bold = true;

                    row = 4;
                    int position = 1;
                    foreach (var caller in statistics.TopChiamanti.Take(10))
                    {
                        double percentuale = statistics.TotaleChiamate > 0 ? (caller.NumeroChiamate * 100.0 / statistics.TotaleChiamate) : 0;
                        wsTop10Callers.Cell($"A{row}").Value = position;
                        wsTop10Callers.Cell($"B{row}").Value = caller.Numero ?? "";
                        wsTop10Callers.Cell($"C{row}").Value = caller.RagioneSociale ?? "";
                        wsTop10Callers.Cell($"D{row}").Value = caller.NumeroChiamate;
                        wsTop10Callers.Cell($"E{row}").Value = FormatDuration(caller.DurataTotale);
                        wsTop10Callers.Cell($"F{row}").Value = FormatDuration(caller.DurataMedia);
                        wsTop10Callers.Cell($"G{row}").Value = $"{percentuale.ToString("F2", CultureInfo.InvariantCulture)}%";
                        row++;
                        position++;
                    }

                    // Foglio Top 10 Chiamati
                    var wsTop10Called = workbook.Worksheets.Add("Top 10 Chiamati");
                    wsTop10Called.Cell("A1").Value = "Top 10 Chiamati";
                    wsTop10Called.Cell("A1").Style.Font.Bold = true;
                    wsTop10Called.Cell("A1").Style.Font.FontSize = 14;
                    
                    wsTop10Called.Cell("A3").Value = "Posizione";
                    wsTop10Called.Cell("B3").Value = "Numero";
                    wsTop10Called.Cell("C3").Value = "Ragione Sociale";
                    wsTop10Called.Cell("D3").Value = "Numero Chiamate";
                    wsTop10Called.Cell("E3").Value = "Durata Totale";
                    wsTop10Called.Cell("F3").Value = "Durata Media";
                    wsTop10Called.Cell("G3").Value = "Percentuale Chiamate";
                    
                    wsTop10Called.Cell("A3").Style.Font.Bold = true;
                    wsTop10Called.Cell("B3").Style.Font.Bold = true;
                    wsTop10Called.Cell("C3").Style.Font.Bold = true;
                    wsTop10Called.Cell("D3").Style.Font.Bold = true;
                    wsTop10Called.Cell("E3").Style.Font.Bold = true;
                    wsTop10Called.Cell("F3").Style.Font.Bold = true;
                    wsTop10Called.Cell("G3").Style.Font.Bold = true;

                    row = 4;
                    position = 1;
                    foreach (var called in statistics.TopChiamati.Take(10))
                    {
                        double percentuale = statistics.TotaleChiamate > 0 ? (called.NumeroChiamate * 100.0 / statistics.TotaleChiamate) : 0;
                        wsTop10Called.Cell($"A{row}").Value = position;
                        wsTop10Called.Cell($"B{row}").Value = called.Numero ?? "";
                        wsTop10Called.Cell($"C{row}").Value = called.RagioneSociale ?? "";
                        wsTop10Called.Cell($"D{row}").Value = called.NumeroChiamate;
                        wsTop10Called.Cell($"E{row}").Value = FormatDuration(called.DurataTotale);
                        wsTop10Called.Cell($"F{row}").Value = FormatDuration(called.DurataMedia);
                        wsTop10Called.Cell($"G{row}").Value = $"{percentuale.ToString("F2", CultureInfo.InvariantCulture)}%";
                        row++;
                        position++;
                    }

                    // Foglio Classifica Completa Chiamanti
                    var wsTopCallers = workbook.Worksheets.Add("Classifica Completa Chiamanti");
                    wsTopCallers.Cell("A1").Value = "Classifica Completa Chiamanti";
                    wsTopCallers.Cell("A1").Style.Font.Bold = true;
                    wsTopCallers.Cell("A1").Style.Font.FontSize = 14;
                    
                    wsTopCallers.Cell("A3").Value = "Posizione";
                    wsTopCallers.Cell("B3").Value = "Numero";
                    wsTopCallers.Cell("C3").Value = "Ragione Sociale";
                    wsTopCallers.Cell("D3").Value = "Numero Chiamate";
                    wsTopCallers.Cell("E3").Value = "Durata Totale";
                    wsTopCallers.Cell("F3").Value = "Durata Media";
                    wsTopCallers.Cell("G3").Value = "Percentuale Chiamate";
                    
                    wsTopCallers.Cell("A3").Style.Font.Bold = true;
                    wsTopCallers.Cell("B3").Style.Font.Bold = true;
                    wsTopCallers.Cell("C3").Style.Font.Bold = true;
                    wsTopCallers.Cell("D3").Style.Font.Bold = true;
                    wsTopCallers.Cell("E3").Style.Font.Bold = true;
                    wsTopCallers.Cell("F3").Style.Font.Bold = true;
                    wsTopCallers.Cell("G3").Style.Font.Bold = true;

                    row = 4;
                    position = 1;
                    foreach (var caller in statistics.TopChiamanti)
                    {
                        double percentuale = statistics.TotaleChiamate > 0 ? (caller.NumeroChiamate * 100.0 / statistics.TotaleChiamate) : 0;
                        wsTopCallers.Cell($"A{row}").Value = position;
                        wsTopCallers.Cell($"B{row}").Value = caller.Numero ?? "";
                        wsTopCallers.Cell($"C{row}").Value = caller.RagioneSociale ?? "";
                        wsTopCallers.Cell($"D{row}").Value = caller.NumeroChiamate;
                        wsTopCallers.Cell($"E{row}").Value = FormatDuration(caller.DurataTotale);
                        wsTopCallers.Cell($"F{row}").Value = FormatDuration(caller.DurataMedia);
                        wsTopCallers.Cell($"G{row}").Value = $"{percentuale.ToString("F2", CultureInfo.InvariantCulture)}%";
                        row++;
                        position++;
                    }

                    // Foglio Classifica Completa Chiamati
                    var wsTopCalled = workbook.Worksheets.Add("Classifica Completa Chiamati");
                    wsTopCalled.Cell("A1").Value = "Classifica Completa Chiamati";
                    wsTopCalled.Cell("A1").Style.Font.Bold = true;
                    wsTopCalled.Cell("A1").Style.Font.FontSize = 14;
                    
                    wsTopCalled.Cell("A3").Value = "Posizione";
                    wsTopCalled.Cell("B3").Value = "Numero";
                    wsTopCalled.Cell("C3").Value = "Ragione Sociale";
                    wsTopCalled.Cell("D3").Value = "Numero Chiamate";
                    wsTopCalled.Cell("E3").Value = "Durata Totale";
                    wsTopCalled.Cell("F3").Value = "Durata Media";
                    wsTopCalled.Cell("G3").Value = "Percentuale Chiamate";
                    
                    wsTopCalled.Cell("A3").Style.Font.Bold = true;
                    wsTopCalled.Cell("B3").Style.Font.Bold = true;
                    wsTopCalled.Cell("C3").Style.Font.Bold = true;
                    wsTopCalled.Cell("D3").Style.Font.Bold = true;
                    wsTopCalled.Cell("E3").Style.Font.Bold = true;
                    wsTopCalled.Cell("F3").Style.Font.Bold = true;
                    wsTopCalled.Cell("G3").Style.Font.Bold = true;

                    row = 4;
                    position = 1;
                    foreach (var called in statistics.TopChiamati)
                    {
                        double percentuale = statistics.TotaleChiamate > 0 ? (called.NumeroChiamate * 100.0 / statistics.TotaleChiamate) : 0;
                        wsTopCalled.Cell($"A{row}").Value = position;
                        wsTopCalled.Cell($"B{row}").Value = called.Numero ?? "";
                        wsTopCalled.Cell($"C{row}").Value = called.RagioneSociale ?? "";
                        wsTopCalled.Cell($"D{row}").Value = called.NumeroChiamate;
                        wsTopCalled.Cell($"E{row}").Value = FormatDuration(called.DurataTotale);
                        wsTopCalled.Cell($"F{row}").Value = FormatDuration(called.DurataMedia);
                        wsTopCalled.Cell($"G{row}").Value = $"{percentuale.ToString("F2", CultureInfo.InvariantCulture)}%";
                        row++;
                        position++;
                    }

                    // Foglio Statistiche Dettagliate
                    var wsDetailed = workbook.Worksheets.Add("Statistiche Dettagliate");
                    wsDetailed.Cell("A1").Value = "Statistiche Dettagliate";
                    wsDetailed.Cell("A1").Style.Font.Bold = true;
                    wsDetailed.Cell("A1").Style.Font.FontSize = 14;
                    
                    var detailedData = new (string Category, string Metric, object Value)[]
                    {
                        ("Chiamate per Direzione", "In Entrata", $"{statistics.ChiamateInEntrata} ({percentualeInEntrata:F1}%)"),
                        ("Chiamate per Direzione", "In Uscita", $"{statistics.ChiamateInUscita} ({percentualeInUscita:F1}%)"),
                        ("Chiamate per Direzione", "Perse", $"{statistics.ChiamatePerse} ({percentualePerse:F1}%)"),
                        ("Chiamate per Direzione", "Non Risposta", $"{statistics.ChiamateNonRisposta} ({percentualeNonRisposta:F1}%)"),
                        ("Chiamate per Tipo", "Manuali", statistics.ChiamateManuali),
                        ("Chiamate per Tipo", "Automatiche", statistics.ChiamateAutomatiche),
                        ("Durata", "Media Generale", FormatDuration(statistics.DurataMediaChiamate)),
                        ("Durata", "Totale Generale", FormatDuration(statistics.DurataTotaleChiamate)),
                        ("Durata", "Media In Entrata", FormatDuration(statistics.DurataMediaInEntrata)),
                        ("Durata", "Media In Uscita", FormatDuration(statistics.DurataMediaInUscita)),
                        ("Durata", "Totale In Entrata", FormatDuration(statistics.DurataTotaleInEntrata)),
                        ("Durata", "Totale In Uscita", FormatDuration(statistics.DurataTotaleInUscita))
                    };

                    wsDetailed.Cell("A3").Value = "Categoria";
                    wsDetailed.Cell("B3").Value = "Metrica";
                    wsDetailed.Cell("C3").Value = "Valore";
                    wsDetailed.Cell("A3").Style.Font.Bold = true;
                    wsDetailed.Cell("B3").Style.Font.Bold = true;
                    wsDetailed.Cell("C3").Style.Font.Bold = true;

                    row = 4;
                    foreach (var detail in detailedData)
                    {
                        wsDetailed.Cell($"A{row}").Value = detail.Category;
                        wsDetailed.Cell($"B{row}").Value = detail.Metric;
                        wsDetailed.Cell($"C{row}").Value = detail.Value.ToString();
                        row++;
                    }

                    // Formattazione avanzata
                    foreach (var ws in workbook.Worksheets)
                    {
                        // Intestazioni
                        var headerRange = ws.Range(ws.FirstCell(), ws.Cell(ws.LastRowUsed().RowNumber(), ws.LastColumnUsed().ColumnNumber()));
                        headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                        
                        // Evidenzia le intestazioni
                        var titleRow = ws.Row(1);
                        titleRow.Style.Fill.BackgroundColor = XLColor.LightBlue;
                        titleRow.Style.Font.Bold = true;
                        
                        // Evidenzia le intestazioni delle colonne
                        if (ws.LastRowUsed().RowNumber() >= 3)
                        {
                            var columnHeaderRow = ws.Row(3);
                            columnHeaderRow.Style.Fill.BackgroundColor = XLColor.LightGray;
                            columnHeaderRow.Style.Font.Bold = true;
                        }
                        
                        // Aggiusta larghezza colonne
                        ws.Columns().AdjustToContents();
                        
                        // Aggiunge filtri alle tabelle principali
                        if (ws.LastRowUsed().RowNumber() > 3)
                        {
                            var dataRange = ws.Range(ws.Cell(3, 1), ws.Cell(ws.LastRowUsed().RowNumber(), ws.LastColumnUsed().ColumnNumber()));
                            dataRange.SetAutoFilter();
                        }
                    }

                    // Genera il nome del file con timestamp e filtri
                    string fileName = $"Statistiche_{DateTime.Now:yyyyMMdd_HHmmss}";
                    if (!string.IsNullOrEmpty(comune))
                        fileName += $"_Comune_{comune.Replace(" ", "_")}";
                    if (!string.IsNullOrEmpty(searchContatto))
                        fileName += $"_Contatto_{searchContatto.Replace(" ", "_")}";
                    fileName += ".xlsx";

                    // Salva in memoria
                    using (var stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);
                        stream.Position = 0;

                        // Restituisci il file
                        return File(
                            stream.ToArray(),
                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                            fileName);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante l'esportazione delle statistiche");
                return RedirectToAction("StatisticheDettagliate");
            }
        }

        private string FormatDuration(double seconds)
        {
            if (seconds < 60)
            {
                return $"{seconds:N0} sec";
            }
            
            int totalSeconds = (int)seconds;
            int hours = totalSeconds / 3600;
            int minutes = (totalSeconds % 3600) / 60;
            int remainingSeconds = totalSeconds % 60;
            
            if (hours > 0)
            {
                return $"{hours}h {minutes}m {remainingSeconds}s";
            }
            else if (minutes > 0)
            {
                return $"{minutes}m {remainingSeconds}s";
            }
            else
            {
                return $"{remainingSeconds}s";
            }
        }

        public async Task<IActionResult> ExportStatisticsComuni(DateTime? dateFrom, DateTime? dateTo, bool includeInterni, string? selectedComune)
        {
            try
            {
                if (string.IsNullOrEmpty(selectedComune))
                {
                    return RedirectToAction("StatisticheComuni");
                }

                // Ottieni le statistiche con un numero molto alto per ottenere tutte le liste
                var statistics = await _chiamataService.GetDetailedStatisticsAsync(dateFrom, dateTo, includeInterni, selectedComune, null, 999999);

                using (var workbook = new XLWorkbook())
                {
                    // Foglio Riepilogo
                    var wsSummary = workbook.Worksheets.Add("Riepilogo");
                    wsSummary.Cell("A1").Value = (XLCellValue)$"Statistiche Comune: {selectedComune}";
                    wsSummary.Cell("A1").Style.Font.Bold = true;
                    wsSummary.Cell("A1").Style.Font.FontSize = 16;

                    // Informazioni sui filtri applicati
                    wsSummary.Cell("A3").Value = (XLCellValue)"Filtri Applicati";
                    wsSummary.Cell("A3").Style.Font.Bold = true;
                    wsSummary.Cell("A3").Style.Font.FontSize = 12;
                    
                    wsSummary.Cell("A4").Value = (XLCellValue)"Comune:";
                    wsSummary.Cell("B4").Value = (XLCellValue)selectedComune;
                    
                    wsSummary.Cell("A5").Value = (XLCellValue)"Periodo:";
                    wsSummary.Cell("B5").Value = (XLCellValue)$"Da {dateFrom?.ToString("dd/MM/yyyy") ?? "Non specificato"} a {dateTo?.ToString("dd/MM/yyyy") ?? "Non specificato"}";
                    
                    wsSummary.Cell("A6").Value = (XLCellValue)"Chiamate Interne:";
                    wsSummary.Cell("B6").Value = (XLCellValue)(includeInterni ? "Incluse" : "Escluse");

                    // Statistiche principali
                    int startRow = 8;
                    wsSummary.Cell($"A{startRow}").Value = (XLCellValue)"Statistiche Principali";
                    wsSummary.Cell($"A{startRow}").Style.Font.Bold = true;
                    wsSummary.Cell($"A{startRow}").Style.Font.FontSize = 12;

                    // Calcola le percentuali
                    double percentualeInEntrata = statistics.TotaleChiamate > 0 ? (statistics.ChiamateInEntrata * 100.0 / statistics.TotaleChiamate) : 0;
                    double percentualeInUscita = statistics.TotaleChiamate > 0 ? (statistics.ChiamateInUscita * 100.0 / statistics.TotaleChiamate) : 0;
                    double percentualePerse = statistics.TotaleChiamate > 0 ? (statistics.ChiamatePerse * 100.0 / statistics.TotaleChiamate) : 0;
                    double percentualeNonRisposta = statistics.TotaleChiamate > 0 ? (statistics.ChiamateNonRisposta * 100.0 / statistics.TotaleChiamate) : 0;

                    var summaryData = new (string Metric, object Value)[]
                    {
                        ("Numero Totale Chiamate", statistics.TotaleChiamate),
                        ("Chiamate In Entrata", $"{statistics.ChiamateInEntrata} ({percentualeInEntrata:F1}%)"),
                        ("Chiamate In Uscita", $"{statistics.ChiamateInUscita} ({percentualeInUscita:F1}%)"),
                        ("Chiamate Perse", $"{statistics.ChiamatePerse} ({percentualePerse:F1}%)"),
                        ("Chiamate Non Risposta", $"{statistics.ChiamateNonRisposta} ({percentualeNonRisposta:F1}%)"),
                        ("Chiamate Manuali", statistics.ChiamateManuali),
                        ("Chiamate Automatiche", statistics.ChiamateAutomatiche),
                        ("Durata Media", FormatDuration(statistics.DurataMediaChiamate)),
                        ("Durata Totale", FormatDuration(statistics.DurataTotaleChiamate)),
                        ("Durata Media In Entrata", FormatDuration(statistics.DurataMediaInEntrata)),
                        ("Durata Media In Uscita", FormatDuration(statistics.DurataMediaInUscita)),
                        ("Durata Totale In Entrata", FormatDuration(statistics.DurataTotaleInEntrata)),
                        ("Durata Totale In Uscita", FormatDuration(statistics.DurataTotaleInUscita))
                    };

                    wsSummary.Cell($"A{startRow + 2}").Value = (XLCellValue)"Metrica";
                    wsSummary.Cell($"B{startRow + 2}").Value = (XLCellValue)"Valore";
                    wsSummary.Cell($"A{startRow + 2}").Style.Font.Bold = true;
                    wsSummary.Cell($"B{startRow + 2}").Style.Font.Bold = true;
                    
                    for (int i = 0; i < summaryData.Length; i++)
                    {
                        wsSummary.Cell($"A{startRow + 3 + i}").Value = (XLCellValue)summaryData[i].Metric;
                        wsSummary.Cell($"B{startRow + 3 + i}").Value = (XLCellValue)summaryData[i].Value.ToString();
                    }

                    // Foglio Distribuzione Tipo
                    var wsTipo = workbook.Worksheets.Add("Distribuzione Tipo");
                    wsTipo.Cell("A1").Value = $"Distribuzione per Tipo di Chiamata - {selectedComune}";
                    wsTipo.Cell("A1").Style.Font.Bold = true;
                    wsTipo.Cell("A1").Style.Font.FontSize = 14;
                    
                    wsTipo.Cell("A3").Value = "Tipo Chiamata";
                    wsTipo.Cell("B3").Value = "Numero Chiamate";
                    wsTipo.Cell("C3").Value = "Percentuale";
                    wsTipo.Cell("D3").Value = "Durata Totale";
                    wsTipo.Cell("E3").Value = "Durata Media";
                    
                    wsTipo.Cell("A3").Style.Font.Bold = true;
                    wsTipo.Cell("B3").Style.Font.Bold = true;
                    wsTipo.Cell("C3").Style.Font.Bold = true;
                    wsTipo.Cell("D3").Style.Font.Bold = true;
                    wsTipo.Cell("E3").Style.Font.Bold = true;

                    int row = 4;
                    foreach (var tipo in statistics.ChiamatePerTipo)
                    {
                        double percentuale = statistics.TotaleChiamate > 0 ? (tipo.Value * 100.0 / statistics.TotaleChiamate) : 0;
                        wsTipo.Cell($"A{row}").Value = tipo.Key;
                        wsTipo.Cell($"B{row}").Value = tipo.Value;
                        wsTipo.Cell($"C{row}").Value = $"{percentuale.ToString("F2", CultureInfo.InvariantCulture)}%";
                        
                        // Calcola durata per tipo (approssimativa)
                        double durataTotaleTipo = tipo.Value * statistics.DurataMediaChiamate;
                        double durataMediaTipo = statistics.DurataMediaChiamate;
                        wsTipo.Cell($"D{row}").Value = FormatDuration(durataTotaleTipo);
                        wsTipo.Cell($"E{row}").Value = FormatDuration(durataMediaTipo);
                        row++;
                    }

                    // Foglio Distribuzione Locazione
                    var wsLoc = workbook.Worksheets.Add("Distribuzione Locazione");
                    wsLoc.Cell("A1").Value = $"Distribuzione per Locazione - {selectedComune}";
                    wsLoc.Cell("A1").Style.Font.Bold = true;
                    wsLoc.Cell("A1").Style.Font.FontSize = 14;
                    
                    wsLoc.Cell("A3").Value = "Locazione";
                    wsLoc.Cell("B3").Value = "Numero Chiamate";
                    wsLoc.Cell("C3").Value = "Percentuale";
                    wsLoc.Cell("D3").Value = "Durata Totale";
                    wsLoc.Cell("E3").Value = "Durata Media";
                    
                    wsLoc.Cell("A3").Style.Font.Bold = true;
                    wsLoc.Cell("B3").Style.Font.Bold = true;
                    wsLoc.Cell("C3").Style.Font.Bold = true;
                    wsLoc.Cell("D3").Style.Font.Bold = true;
                    wsLoc.Cell("E3").Style.Font.Bold = true;

                    row = 4;
                    foreach (var loc in statistics.ChiamatePerLocazione)
                    {
                        double percentuale = statistics.TotaleChiamate > 0 ? (loc.Value * 100.0 / statistics.TotaleChiamate) : 0;
                        wsLoc.Cell($"A{row}").Value = loc.Key;
                        wsLoc.Cell($"B{row}").Value = loc.Value;
                        wsLoc.Cell($"C{row}").Value = $"{percentuale.ToString("F2", CultureInfo.InvariantCulture)}%";
                        
                        // Calcola durata per locazione (approssimativa)
                        double durataTotaleLoc = loc.Value * statistics.DurataMediaChiamate;
                        double durataMediaLoc = statistics.DurataMediaChiamate;
                        wsLoc.Cell($"D{row}").Value = FormatDuration(durataTotaleLoc);
                        wsLoc.Cell($"E{row}").Value = FormatDuration(durataMediaLoc);
                        row++;
                    }

                    // Foglio Distribuzione Giornaliera
                    var wsDaily = workbook.Worksheets.Add("Distribuzione Giornaliera");
                    wsDaily.Cell("A1").Value = $"Distribuzione Giornaliera - {selectedComune}";
                    wsDaily.Cell("A1").Style.Font.Bold = true;
                    wsDaily.Cell("A1").Style.Font.FontSize = 14;
                    
                    wsDaily.Cell("A3").Value = "Data";
                    wsDaily.Cell("B3").Value = "Numero Chiamate";
                    wsDaily.Cell("C3").Value = "Durata Totale";
                    wsDaily.Cell("D3").Value = "Durata Media";
                    wsDaily.Cell("E3").Value = "Chiamate In Entrata";
                    wsDaily.Cell("F3").Value = "Chiamate In Uscita";
                    
                    wsDaily.Cell("A3").Style.Font.Bold = true;
                    wsDaily.Cell("B3").Style.Font.Bold = true;
                    wsDaily.Cell("C3").Style.Font.Bold = true;
                    wsDaily.Cell("D3").Style.Font.Bold = true;
                    wsDaily.Cell("E3").Style.Font.Bold = true;
                    wsDaily.Cell("F3").Style.Font.Bold = true;

                    row = 4;
                    foreach (var daily in statistics.ChiamatePerGiorno)
                    {
                        DateTime date = DateTime.ParseExact(daily.Key, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                        wsDaily.Cell($"A{row}").Value = date;
                        wsDaily.Cell($"A{row}").Style.DateFormat.Format = "dd/MM/yyyy";
                        wsDaily.Cell($"B{row}").Value = daily.Value;
                        
                        // Calcola durata per giorno (approssimativa)
                        double durataTotaleGiorno = daily.Value * statistics.DurataMediaChiamate;
                        double durataMediaGiorno = statistics.DurataMediaChiamate;
                        wsDaily.Cell($"C{row}").Value = FormatDuration(durataTotaleGiorno);
                        wsDaily.Cell($"D{row}").Value = FormatDuration(durataMediaGiorno);
                        
                        // Approssimazione chiamate in entrata/uscita per giorno
                        int chiamateInEntrataGiorno = (int)(daily.Value * (statistics.ChiamateInEntrata / (double)statistics.TotaleChiamate));
                        int chiamateInUscitaGiorno = daily.Value - chiamateInEntrataGiorno;
                        wsDaily.Cell($"E{row}").Value = chiamateInEntrataGiorno;
                        wsDaily.Cell($"F{row}").Value = chiamateInUscitaGiorno;
                        row++;
                    }

                    // Foglio Distribuzione Oraria (se disponibile)
                    if (statistics.ChiamatePerOra != null && statistics.ChiamatePerOra.Any())
                    {
                        var wsHourly = workbook.Worksheets.Add("Distribuzione Oraria");
                        wsHourly.Cell("A1").Value = $"Distribuzione Oraria - {selectedComune}";
                        wsHourly.Cell("A1").Style.Font.Bold = true;
                        wsHourly.Cell("A1").Style.Font.FontSize = 14;
                        
                        wsHourly.Cell("A3").Value = "Ora";
                        wsHourly.Cell("B3").Value = "Numero Chiamate";
                        wsHourly.Cell("C3").Value = "Percentuale";
                        wsHourly.Cell("D3").Value = "Durata Totale";
                        wsHourly.Cell("E3").Value = "Durata Media";
                        
                        wsHourly.Cell("A3").Style.Font.Bold = true;
                        wsHourly.Cell("B3").Style.Font.Bold = true;
                        wsHourly.Cell("C3").Style.Font.Bold = true;
                        wsHourly.Cell("D3").Style.Font.Bold = true;
                        wsHourly.Cell("E3").Style.Font.Bold = true;

                        row = 4;
                        foreach (var hourly in statistics.ChiamatePerOra.OrderBy(x => x.Key))
                        {
                            double percentuale = statistics.TotaleChiamate > 0 ? (hourly.Value * 100.0 / statistics.TotaleChiamate) : 0;
                            wsHourly.Cell($"A{row}").Value = $"{hourly.Key}:00";
                            wsHourly.Cell($"B{row}").Value = hourly.Value;
                            wsHourly.Cell($"C{row}").Value = $"{percentuale.ToString("F2", CultureInfo.InvariantCulture)}%";
                            
                            // Calcola durata per ora (approssimativa)
                            double durataTotaleOra = hourly.Value * statistics.DurataMediaChiamate;
                            double durataMediaOra = statistics.DurataMediaChiamate;
                            wsHourly.Cell($"D{row}").Value = FormatDuration(durataTotaleOra);
                            wsHourly.Cell($"E{row}").Value = FormatDuration(durataMediaOra);
                            row++;
                        }
                    }

                    // Foglio Top 10 Chiamanti
                    var wsTop10Callers = workbook.Worksheets.Add("Top 10 Chiamanti");
                    wsTop10Callers.Cell("A1").Value = $"Top 10 Chiamanti - {selectedComune}";
                    wsTop10Callers.Cell("A1").Style.Font.Bold = true;
                    wsTop10Callers.Cell("A1").Style.Font.FontSize = 14;
                    
                    wsTop10Callers.Cell("A3").Value = "Posizione";
                    wsTop10Callers.Cell("B3").Value = "Numero";
                    wsTop10Callers.Cell("C3").Value = "Ragione Sociale";
                    wsTop10Callers.Cell("D3").Value = "Numero Chiamate";
                    wsTop10Callers.Cell("E3").Value = "Durata Totale";
                    wsTop10Callers.Cell("F3").Value = "Durata Media";
                    wsTop10Callers.Cell("G3").Value = "Percentuale Chiamate";
                    
                    wsTop10Callers.Cell("A3").Style.Font.Bold = true;
                    wsTop10Callers.Cell("B3").Style.Font.Bold = true;
                    wsTop10Callers.Cell("C3").Style.Font.Bold = true;
                    wsTop10Callers.Cell("D3").Style.Font.Bold = true;
                    wsTop10Callers.Cell("E3").Style.Font.Bold = true;
                    wsTop10Callers.Cell("F3").Style.Font.Bold = true;
                    wsTop10Callers.Cell("G3").Style.Font.Bold = true;

                    row = 4;
                    int position = 1;
                    foreach (var caller in statistics.TopChiamanti.Take(10))
                    {
                        double percentuale = statistics.TotaleChiamate > 0 ? (caller.NumeroChiamate * 100.0 / statistics.TotaleChiamate) : 0;
                        wsTop10Callers.Cell($"A{row}").Value = position;
                        wsTop10Callers.Cell($"B{row}").Value = caller.Numero ?? "";
                        wsTop10Callers.Cell($"C{row}").Value = caller.RagioneSociale ?? "";
                        wsTop10Callers.Cell($"D{row}").Value = caller.NumeroChiamate;
                        wsTop10Callers.Cell($"E{row}").Value = FormatDuration(caller.DurataTotale);
                        wsTop10Callers.Cell($"F{row}").Value = FormatDuration(caller.DurataMedia);
                        wsTop10Callers.Cell($"G{row}").Value = $"{percentuale.ToString("F2", CultureInfo.InvariantCulture)}%";
                        row++;
                        position++;
                    }

                    // Foglio Top 10 Chiamati
                    var wsTop10Called = workbook.Worksheets.Add("Top 10 Chiamati");
                    wsTop10Called.Cell("A1").Value = $"Top 10 Chiamati - {selectedComune}";
                    wsTop10Called.Cell("A1").Style.Font.Bold = true;
                    wsTop10Called.Cell("A1").Style.Font.FontSize = 14;
                    
                    wsTop10Called.Cell("A3").Value = "Posizione";
                    wsTop10Called.Cell("B3").Value = "Numero";
                    wsTop10Called.Cell("C3").Value = "Ragione Sociale";
                    wsTop10Called.Cell("D3").Value = "Numero Chiamate";
                    wsTop10Called.Cell("E3").Value = "Durata Totale";
                    wsTop10Called.Cell("F3").Value = "Durata Media";
                    wsTop10Called.Cell("G3").Value = "Percentuale Chiamate";
                    
                    wsTop10Called.Cell("A3").Style.Font.Bold = true;
                    wsTop10Called.Cell("B3").Style.Font.Bold = true;
                    wsTop10Called.Cell("C3").Style.Font.Bold = true;
                    wsTop10Called.Cell("D3").Style.Font.Bold = true;
                    wsTop10Called.Cell("E3").Style.Font.Bold = true;
                    wsTop10Called.Cell("F3").Style.Font.Bold = true;
                    wsTop10Called.Cell("G3").Style.Font.Bold = true;

                    row = 4;
                    position = 1;
                    foreach (var called in statistics.TopChiamati.Take(10))
                    {
                        double percentuale = statistics.TotaleChiamate > 0 ? (called.NumeroChiamate * 100.0 / statistics.TotaleChiamate) : 0;
                        wsTop10Called.Cell($"A{row}").Value = position;
                        wsTop10Called.Cell($"B{row}").Value = called.Numero ?? "";
                        wsTop10Called.Cell($"C{row}").Value = called.RagioneSociale ?? "";
                        wsTop10Called.Cell($"D{row}").Value = called.NumeroChiamate;
                        wsTop10Called.Cell($"E{row}").Value = FormatDuration(called.DurataTotale);
                        wsTop10Called.Cell($"F{row}").Value = FormatDuration(called.DurataMedia);
                        wsTop10Called.Cell($"G{row}").Value = $"{percentuale.ToString("F2", CultureInfo.InvariantCulture)}%";
                        row++;
                        position++;
                    }

                    // Foglio Classifica Completa Chiamanti
                    var wsTopCallers = workbook.Worksheets.Add("Classifica Completa Chiamanti");
                    wsTopCallers.Cell("A1").Value = $"Classifica Completa Chiamanti - {selectedComune}";
                    wsTopCallers.Cell("A1").Style.Font.Bold = true;
                    wsTopCallers.Cell("A1").Style.Font.FontSize = 14;
                    
                    wsTopCallers.Cell("A3").Value = "Posizione";
                    wsTopCallers.Cell("B3").Value = "Numero";
                    wsTopCallers.Cell("C3").Value = "Ragione Sociale";
                    wsTopCallers.Cell("D3").Value = "Numero Chiamate";
                    wsTopCallers.Cell("E3").Value = "Durata Totale";
                    wsTopCallers.Cell("F3").Value = "Durata Media";
                    wsTopCallers.Cell("G3").Value = "Percentuale Chiamate";
                    
                    wsTopCallers.Cell("A3").Style.Font.Bold = true;
                    wsTopCallers.Cell("B3").Style.Font.Bold = true;
                    wsTopCallers.Cell("C3").Style.Font.Bold = true;
                    wsTopCallers.Cell("D3").Style.Font.Bold = true;
                    wsTopCallers.Cell("E3").Style.Font.Bold = true;
                    wsTopCallers.Cell("F3").Style.Font.Bold = true;
                    wsTopCallers.Cell("G3").Style.Font.Bold = true;

                    row = 4;
                    position = 1;
                    foreach (var caller in statistics.TopChiamanti)
                    {
                        double percentuale = statistics.TotaleChiamate > 0 ? (caller.NumeroChiamate * 100.0 / statistics.TotaleChiamate) : 0;
                        wsTopCallers.Cell($"A{row}").Value = position;
                        wsTopCallers.Cell($"B{row}").Value = caller.Numero ?? "";
                        wsTopCallers.Cell($"C{row}").Value = caller.RagioneSociale ?? "";
                        wsTopCallers.Cell($"D{row}").Value = caller.NumeroChiamate;
                        wsTopCallers.Cell($"E{row}").Value = FormatDuration(caller.DurataTotale);
                        wsTopCallers.Cell($"F{row}").Value = FormatDuration(caller.DurataMedia);
                        wsTopCallers.Cell($"G{row}").Value = $"{percentuale.ToString("F2", CultureInfo.InvariantCulture)}%";
                        row++;
                        position++;
                    }

                    // Foglio Classifica Completa Chiamati
                    var wsTopCalled = workbook.Worksheets.Add("Classifica Completa Chiamati");
                    wsTopCalled.Cell("A1").Value = $"Classifica Completa Chiamati - {selectedComune}";
                    wsTopCalled.Cell("A1").Style.Font.Bold = true;
                    wsTopCalled.Cell("A1").Style.Font.FontSize = 14;
                    
                    wsTopCalled.Cell("A3").Value = "Posizione";
                    wsTopCalled.Cell("B3").Value = "Numero";
                    wsTopCalled.Cell("C3").Value = "Ragione Sociale";
                    wsTopCalled.Cell("D3").Value = "Numero Chiamate";
                    wsTopCalled.Cell("E3").Value = "Durata Totale";
                    wsTopCalled.Cell("F3").Value = "Durata Media";
                    wsTopCalled.Cell("G3").Value = "Percentuale Chiamate";
                    
                    wsTopCalled.Cell("A3").Style.Font.Bold = true;
                    wsTopCalled.Cell("B3").Style.Font.Bold = true;
                    wsTopCalled.Cell("C3").Style.Font.Bold = true;
                    wsTopCalled.Cell("D3").Style.Font.Bold = true;
                    wsTopCalled.Cell("E3").Style.Font.Bold = true;
                    wsTopCalled.Cell("F3").Style.Font.Bold = true;
                    wsTopCalled.Cell("G3").Style.Font.Bold = true;

                    row = 4;
                    position = 1;
                    foreach (var called in statistics.TopChiamati)
                    {
                        double percentuale = statistics.TotaleChiamate > 0 ? (called.NumeroChiamate * 100.0 / statistics.TotaleChiamate) : 0;
                        wsTopCalled.Cell($"A{row}").Value = position;
                        wsTopCalled.Cell($"B{row}").Value = called.Numero ?? "";
                        wsTopCalled.Cell($"C{row}").Value = called.RagioneSociale ?? "";
                        wsTopCalled.Cell($"D{row}").Value = called.NumeroChiamate;
                        wsTopCalled.Cell($"E{row}").Value = FormatDuration(called.DurataTotale);
                        wsTopCalled.Cell($"F{row}").Value = FormatDuration(called.DurataMedia);
                        wsTopCalled.Cell($"G{row}").Value = $"{percentuale.ToString("F2", CultureInfo.InvariantCulture)}%";
                        row++;
                        position++;
                    }

                    // Foglio Statistiche Dettagliate
                    var wsDetailed = workbook.Worksheets.Add("Statistiche Dettagliate");
                    wsDetailed.Cell("A1").Value = $"Statistiche Dettagliate - {selectedComune}";
                    wsDetailed.Cell("A1").Style.Font.Bold = true;
                    wsDetailed.Cell("A1").Style.Font.FontSize = 14;
                    
                    var detailedData = new (string Category, string Metric, object Value)[]
                    {
                        ("Chiamate per Direzione", "In Entrata", $"{statistics.ChiamateInEntrata} ({percentualeInEntrata:F1}%)"),
                        ("Chiamate per Direzione", "In Uscita", $"{statistics.ChiamateInUscita} ({percentualeInUscita:F1}%)"),
                        ("Chiamate per Direzione", "Perse", $"{statistics.ChiamatePerse} ({percentualePerse:F1}%)"),
                        ("Chiamate per Direzione", "Non Risposta", $"{statistics.ChiamateNonRisposta} ({percentualeNonRisposta:F1}%)"),
                        ("Chiamate per Tipo", "Manuali", statistics.ChiamateManuali),
                        ("Chiamate per Tipo", "Automatiche", statistics.ChiamateAutomatiche),
                        ("Durata", "Media Generale", FormatDuration(statistics.DurataMediaChiamate)),
                        ("Durata", "Totale Generale", FormatDuration(statistics.DurataTotaleChiamate)),
                        ("Durata", "Media In Entrata", FormatDuration(statistics.DurataMediaInEntrata)),
                        ("Durata", "Media In Uscita", FormatDuration(statistics.DurataMediaInUscita)),
                        ("Durata", "Totale In Entrata", FormatDuration(statistics.DurataTotaleInEntrata)),
                        ("Durata", "Totale In Uscita", FormatDuration(statistics.DurataTotaleInUscita))
                    };

                    wsDetailed.Cell("A3").Value = "Categoria";
                    wsDetailed.Cell("B3").Value = "Metrica";
                    wsDetailed.Cell("C3").Value = "Valore";
                    wsDetailed.Cell("A3").Style.Font.Bold = true;
                    wsDetailed.Cell("B3").Style.Font.Bold = true;
                    wsDetailed.Cell("C3").Style.Font.Bold = true;

                    row = 4;
                    foreach (var detail in detailedData)
                    {
                        wsDetailed.Cell($"A{row}").Value = detail.Category;
                        wsDetailed.Cell($"B{row}").Value = detail.Metric;
                        wsDetailed.Cell($"C{row}").Value = detail.Value.ToString();
                        row++;
                    }

                    // Formattazione avanzata
                    foreach (var ws in workbook.Worksheets)
                    {
                        // Intestazioni
                        var headerRange = ws.Range(ws.FirstCell(), ws.Cell(ws.LastRowUsed().RowNumber(), ws.LastColumnUsed().ColumnNumber()));
                        headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                        
                        // Evidenzia le intestazioni
                        var titleRow = ws.Row(1);
                        titleRow.Style.Fill.BackgroundColor = XLColor.LightBlue;
                        titleRow.Style.Font.Bold = true;
                        
                        // Evidenzia le intestazioni delle colonne
                        if (ws.LastRowUsed().RowNumber() >= 3)
                        {
                            var columnHeaderRow = ws.Row(3);
                            columnHeaderRow.Style.Fill.BackgroundColor = XLColor.LightGray;
                            columnHeaderRow.Style.Font.Bold = true;
                        }
                        
                        // Aggiusta larghezza colonne
                        ws.Columns().AdjustToContents();
                        
                        // Aggiunge filtri alle tabelle principali
                        if (ws.LastRowUsed().RowNumber() > 3)
                        {
                            var dataRange = ws.Range(ws.Cell(3, 1), ws.Cell(ws.LastRowUsed().RowNumber(), ws.LastColumnUsed().ColumnNumber()));
                            dataRange.SetAutoFilter();
                        }
                    }

                    // Genera il nome del file con timestamp e comune
                    string fileName = $"Statistiche_Comune_{selectedComune.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                    // Salva in memoria
                    using (var stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);
                        stream.Position = 0;

                        // Restituisci il file
                        return File(
                            stream.ToArray(),
                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                            fileName);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante l'esportazione delle statistiche del comune");
                return RedirectToAction("StatisticheComuni");
            }
        }
    }
}
