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
    }
}
