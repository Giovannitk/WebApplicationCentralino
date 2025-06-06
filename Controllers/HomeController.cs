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
                        dateFrom = fromDate.ToString("yyyy-MM-dd");
                    }
                    fromDateParsed = fromDate;
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
                        dateTo = toDate.ToString("yyyy-MM-dd");
                    }
                    toDateParsed = toDate.AddDays(1).AddSeconds(-1);
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
                        dateFrom = fromDate.ToString("yyyy-MM-dd");
                    }
                    fromDateParsed = fromDate;
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
                        dateTo = toDate.ToString("yyyy-MM-dd");
                    }
                    toDateParsed = toDate.AddDays(1).AddSeconds(-1);
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

        [HttpGet]
        public async Task<IActionResult> GetTopChiamanti(string searchContatto, int count = 10, string? dateFrom = null, string? dateTo = null)
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
                }

                if (!string.IsNullOrEmpty(dateTo) && DateTimeExtensions.TryParseWithYearHandling(dateTo, out DateTime toDate))
                {
                    if (toDate.Year < 2020)
                    {
                        toDate = new DateTime(oggi.Year, toDate.Month, toDate.Day);
                    }
                    toDateParsed = toDate.AddDays(1).AddSeconds(-1);
                }

                var statistiche = await _chiamataService.GetDetailedStatisticsAsync(fromDateParsed, toDateParsed, true, null, searchContatto);
                var topChiamanti = statistiche.TopChiamanti.Take(count).Select(c => new {
                    numero = c.Numero,
                    ragioneSociale = c.RagioneSociale,
                    numeroChiamate = c.NumeroChiamate,
                    durataTotale = c.DurataTotale,
                    durataMedia = c.DurataMedia
                });

                return Json(topChiamanti);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il recupero dei top chiamanti");
                return Json(new List<object>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetTopChiamati(string searchContatto, int count = 10, string? dateFrom = null, string? dateTo = null)
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
                }

                if (!string.IsNullOrEmpty(dateTo) && DateTimeExtensions.TryParseWithYearHandling(dateTo, out DateTime toDate))
                {
                    if (toDate.Year < 2020)
                    {
                        toDate = new DateTime(oggi.Year, toDate.Month, toDate.Day);
                    }
                    toDateParsed = toDate.AddDays(1).AddSeconds(-1);
                }

                var statistiche = await _chiamataService.GetDetailedStatisticsAsync(fromDateParsed, toDateParsed, true, null, searchContatto);
                var topChiamati = statistiche.TopChiamati.Take(count).Select(c => new {
                    numero = c.Numero,
                    ragioneSociale = c.RagioneSociale,
                    numeroChiamate = c.NumeroChiamate,
                    durataTotale = c.DurataTotale,
                    durataMedia = c.DurataMedia
                });

                return Json(topChiamati);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il recupero dei top chiamati");
                return Json(new List<object>());
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
