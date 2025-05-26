using Microsoft.AspNetCore.Mvc;
using WebApplicationCentralino.Models;
using WebApplicationCentralino.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authorization;

namespace WebApplicationCentralino.Controllers
{
    [Authorize]
    public class GestioneContattiController : Controller
    {
        private readonly ContattoService _contattoService;
        private readonly ILogger<GestioneContattiController> _logger;
        private readonly IConfiguration _configuration;

        public GestioneContattiController(
            ContattoService contattoService, 
            ILogger<GestioneContattiController> logger,
            IConfiguration configuration)
        {
            _contattoService = contattoService;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<IActionResult> Index(string? id, string? numero)
        {   
            try
            {
                if (!string.IsNullOrEmpty(numero))
                {
                    // Se viene fornito un numero, carica il contatto esistente
                    var contatti = await _contattoService.GetAllAsync();
                    var contatto = contatti.FirstOrDefault(c => c.NumeroContatto == numero);
                    
                    if (contatto != null)
                    {
                        return View(contatto);
                    }
                }
                else if (!string.IsNullOrEmpty(id))
                {
                    // Se viene fornito un ID, carica il contatto esistente
                    var contatti = await _contattoService.GetAllAsync();
                    var contatto = contatti.FirstOrDefault(c => c.Id == id);
                    
                    if (contatto != null)
                    {
                        return View(contatto);
                    }
                }
                
                // Se non viene fornito né un ID né un numero, o il contatto non viene trovato, mostra il form vuoto
                return View(new Contatto());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il recupero dei contatti");
                TempData["ErrorMessage"] = "Errore durante il recupero dei contatti";
                return View(new Contatto());
            }
        }

        [HttpPost]
        public async Task<IActionResult> Salva(Contatto contatto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var contatti = await _contattoService.GetAllAsync();
                    ViewBag.Contatti = contatti;
                    return View("Index", contatto);
                }

                bool success;
                if (string.IsNullOrEmpty(contatto.Id))
                {
                    // Nuovo contatto
                    success = await _contattoService.AddAsync(contatto);
                    if (success)
                        TempData["SuccessMessage"] = "Contatto aggiunto con successo";
                }
                else
                {
                    // Aggiornamento contatto esistente
                    success = await _contattoService.UpdateAsync(contatto.NumeroContatto, contatto);
                    if (success)
                        TempData["SuccessMessage"] = "Contatto aggiornato con successo";
                }

                if (!success)
                {
                    TempData["ErrorMessage"] = "Errore durante il salvataggio del contatto";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il salvataggio del contatto");
                TempData["ErrorMessage"] = "Errore durante il salvataggio del contatto";
                var contatti = await _contattoService.GetAllAsync();
                ViewBag.Contatti = contatti;
                return View("Index", contatto);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Modifica(string numero)
        {
            try
            {
                var contatti = await _contattoService.GetAllAsync();
                var contatto = contatti.FirstOrDefault(c => c.NumeroContatto == numero);
                
                if (contatto == null)
                {
                    TempData["ErrorMessage"] = "Contatto non trovato";
                    return RedirectToAction(nameof(Index));
                }

                ViewBag.Contatti = contatti;
                return View("Index", contatto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il recupero del contatto");
                TempData["ErrorMessage"] = "Errore durante il recupero del contatto";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        public async Task<IActionResult> Elimina(string numero, string password)
        {
            try
            {
                // Verifica la password di eliminazione
                var deletePassword = _configuration["Security:DeleteContactPassword"];
                if (string.IsNullOrEmpty(deletePassword))
                {
                    _logger.LogWarning("Password di eliminazione contatti non configurata");
                    TempData["ErrorMessage"] = "Operazione non disponibile";
                    return RedirectToAction(nameof(Index));
                }

                if (password != deletePassword)
                {
                    TempData["ErrorMessage"] = "Password non valida per l'eliminazione del contatto";
                    return RedirectToAction(nameof(Index));
                }

                var success = await _contattoService.DeleteAsync(numero);
                if (success)
                    TempData["SuccessMessage"] = "Contatto eliminato con successo";
                else
                    TempData["ErrorMessage"] = "Errore durante l'eliminazione del contatto";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante l'eliminazione del contatto");
                TempData["ErrorMessage"] = "Errore durante l'eliminazione del contatto";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}


