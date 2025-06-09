using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using WebApplicationCentralino.Models;
using WebApplicationCentralino.Services;

namespace WebApplicationCentralino.Controllers
{
    [Authorize]
    public class ContattiController : Controller
    {
        private readonly ContattoService _service;
        private const int DEFAULT_PAGE_SIZE = 25;

        public ContattiController(ContattoService service)
        {
            _service = service;
        }

        public async Task<IActionResult> Index(int page = 1, string? searchTerm = null)
        {
            var (contatti, totalCount) = await _service.GetPaginatedContattiAsync(page, DEFAULT_PAGE_SIZE, searchTerm);
            
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)DEFAULT_PAGE_SIZE);
            ViewBag.SearchTerm = searchTerm;
            ViewBag.TotalCount = totalCount;
            ViewBag.UltimoAggiornamento = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

            return View(contatti);
        }

        [HttpPost]
        public async Task<IActionResult> Refresh()
        {
            // Invalida la cache
            _service.InvalidateCache();
            
            // Reindirizza alla pagina principale
            return RedirectToAction(nameof(Index));
        }
    }
}
