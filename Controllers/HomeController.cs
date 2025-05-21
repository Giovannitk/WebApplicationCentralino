using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WebApplicationCentralino.Models;
using WebApplicationCentralino.Services;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace WebApplicationCentralino.Controllers
{
    /// <summary>
    /// Controller principale dell'applicazione che gestisce la home page e le statistiche
    /// </summary>
    public class HomeController : Controller
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
        public async Task<IActionResult> StatisticheDettagliate(string? dateFrom = null, string? dateTo = null, bool includeInterni = false)
        {
            try
            {
                DateTime? fromDateParsed = null;
                DateTime? toDateParsed = null;

                // Parsing dei parametri di data
                if (!string.IsNullOrEmpty(dateFrom) && DateTime.TryParse(dateFrom, out DateTime fromDate))
                {
                    fromDateParsed = fromDate;
                }
                else
                {
                    // Se non specificato, usa l'inizio del mese corrente
                    fromDateParsed = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                    dateFrom = fromDateParsed.Value.ToString("yyyy-MM-dd");
                }

                if (!string.IsNullOrEmpty(dateTo) && DateTime.TryParse(dateTo, out DateTime toDate))
                {
                    toDateParsed = toDate.AddDays(1).AddSeconds(-1);
                }
                else
                {
                    // Se non specificato, usa oggi
                    toDateParsed = DateTime.Today.AddDays(1).AddSeconds(-1);
                    dateTo = DateTime.Today.ToString("yyyy-MM-dd");
                }

                // Recupera le statistiche dettagliate
                var statistiche = await _chiamataService.GetDetailedStatisticsAsync(fromDateParsed, toDateParsed, includeInterni);
                
                // Passa i valori alla vista per mantenere i filtri
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

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
