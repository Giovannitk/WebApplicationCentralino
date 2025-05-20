using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WebApplicationCentralino.Models;
using WebApplicationCentralino.Services;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace WebApplicationCentralino.Controllers
{
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

        public async Task<IActionResult> Index()
        {
            try
            {
                var statistiche = await _chiamataService.GetCallStatisticsAsync();
                ViewBag.Statistiche = statistiche;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il recupero delle statistiche");
            }
            return View();
        }

        public IActionResult ContattiIncompleti()
        {
            try
            {
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

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
