using Microsoft.AspNetCore.Mvc;
using WebApplicationCentralino.Models;
using WebApplicationCentralino.Services;

namespace WebApplicationCentralino.Controllers
{
    public class ContattiController : Controller
    {
        private readonly ContattoService _service;

        public ContattiController(ContattoService service)
        {
            _service = service;
        }

        public async Task<IActionResult> Index()
        {
            // Ottieni le chiamate filtrate
            var contatti = await _service.GetFilteredContattiAsync();

            ViewBag.UltimoAggiornamento = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

            return View(contatti);
        }
    }

}
