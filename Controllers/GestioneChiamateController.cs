using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System;
using WebApplicationCentralino.Services;
using WebApplicationCentralino.Models;

namespace WebApplicationCentralino.Controllers
{
    public class GestioneChiamateController : Controller
    {
        private readonly GestioneChiamataService _gestioneChiamataService;
        private readonly ILogger<GestioneChiamateController> _logger;

        public GestioneChiamateController(GestioneChiamataService gestioneChiamataService,
                                         ILogger<GestioneChiamateController> logger)
        {
            _gestioneChiamataService = gestioneChiamataService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                // Carica le chiamate esistenti per visualizzazione
                var chiamate = await _gestioneChiamataService.GetAllChiamateAsync();
                ViewBag.Chiamate = chiamate;

                return View(new Chiamata
                {
                    DataArrivoChiamata = DateTime.Now.AddSeconds(-DateTime.Now.Second).AddMilliseconds(-DateTime.Now.Millisecond),
                    DataFineChiamata = DateTime.Now.AddMinutes(5).AddSeconds(-DateTime.Now.Second).AddMilliseconds(-DateTime.Now.Millisecond),
                    UniqueID = Guid.NewGuid().ToString()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il caricamento della pagina principale");
                ViewBag.ErrorMessage = "Errore durante il caricamento delle chiamate";
                return View(new Chiamata());
            }
        }

        [HttpGet]
        public async Task<IActionResult> Nuova(int? id)
        {
            try
            {
                if (id.HasValue && id.Value > 0)
                {
                    // Modifica di una chiamata esistente
                    _logger.LogInformation($"Tentativo di recuperare la chiamata con ID: {id}");
                    var chiamataEsistente = await _gestioneChiamataService.GetChiamataByIdAsync(id.Value);

                    if (chiamataEsistente != null)
                    {
                        _logger.LogInformation($"Chiamata trovata: {chiamataEsistente.Id}, {chiamataEsistente.NumeroChiamante}");
                        return View("Index", chiamataEsistente);
                    }

                    _logger.LogWarning($"Chiamata con ID {id} non trovata");
                    return NotFound();
                }
                else
                {
                    // Nuova chiamata
                    var now = DateTime.Now;
                    var dataArrivo = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0);
                    var dataFine = dataArrivo.AddMinutes(5);
                    return View("Index", new Chiamata
                    {
                        DataArrivoChiamata = dataArrivo,
                        DataFineChiamata = dataFine,
                        UniqueID = Guid.NewGuid().ToString()
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Errore durante l'elaborazione della chiamata ID: {id}");
                throw;
            }
        }

        [HttpPost]
        public async Task<IActionResult> Salva(Chiamata chiamata)
        {
            // Validazione personalizzata: almeno uno tra numero e ragione sociale deve essere presente
            bool validazioneChiamante = !string.IsNullOrEmpty(chiamata.NumeroChiamante) ||
                                      !string.IsNullOrEmpty(chiamata.RagioneSocialeChiamante);
            bool validazioneChiamato = !string.IsNullOrEmpty(chiamata.NumeroChiamato) ||
                                     !string.IsNullOrEmpty(chiamata.RagioneSocialeChiamato);

            if (!validazioneChiamante)
            {
                ModelState.AddModelError("NumeroChiamante", "Inserire almeno uno tra Numero o Ragione Sociale del chiamante");
            }

            if (!validazioneChiamato)
            {
                ModelState.AddModelError("NumeroChiamato", "Inserire almeno uno tra Numero o Ragione Sociale del chiamato");
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Il modello non è valido");
                return View("Index", chiamata);
            }

            try
            {
                // Assicurati che tutte le proprietà necessarie siano presenti
                if (string.IsNullOrEmpty(chiamata.UniqueID))
                {
                    chiamata.UniqueID = Guid.NewGuid().ToString();
                }

                // Normalizza le date rimuovendo i millisecondi
                chiamata.DataArrivoChiamata = new DateTime(
                    chiamata.DataArrivoChiamata.Year,
                    chiamata.DataArrivoChiamata.Month,
                    chiamata.DataArrivoChiamata.Day,
                    chiamata.DataArrivoChiamata.Hour,
                    chiamata.DataArrivoChiamata.Minute,
                    chiamata.DataArrivoChiamata.Second
                );

                chiamata.DataFineChiamata = new DateTime(
                    chiamata.DataFineChiamata.Year,
                    chiamata.DataFineChiamata.Month,
                    chiamata.DataFineChiamata.Day,
                    chiamata.DataFineChiamata.Hour,
                    chiamata.DataFineChiamata.Minute,
                    chiamata.DataFineChiamata.Second
                );

                // Logga i dettagli della chiamata
                _logger.LogInformation($"Chiamata da salvare: ID={chiamata.Id}, " +
                                       $"NumeroChiamante={chiamata.NumeroChiamante}, " +
                                       $"NumeroChiamato={chiamata.NumeroChiamato}, " +
                                       $"DataArrivo={chiamata.DataArrivoChiamata} (Ticks: {chiamata.DataArrivoChiamata.Ticks}), " +
                                       $"DataFine={chiamata.DataFineChiamata} (Ticks: {chiamata.DataFineChiamata.Ticks}), " +
                                       $"UniqueID={chiamata.UniqueID}");

                string message;
                bool success;

                // Verifica se esiste già una chiamata con stessa data inizio/fine e (opzionale) stessi numeri
                var idEsistente = await _gestioneChiamataService.TrovaIdChiamataEsistenteAsync(
                    chiamata.DataArrivoChiamata,
                    chiamata.DataFineChiamata,
                    chiamata.NumeroChiamante,
                    chiamata.NumeroChiamato
                );

                if (idEsistente.HasValue)
                {
                    // Esiste già: aggiorna quella
                    chiamata.Id = idEsistente.Value;
                    _logger.LogInformation($"Aggiornamento chiamata esistente con ID {chiamata.Id}");
                    success = await _gestioneChiamataService.AggiornaChiamataAsync(chiamata);
                    message = $"Chiamata aggiornata con ID: {chiamata.Id}";
                }
                else
                {
                    // Non esiste: aggiungi nuova
                    _logger.LogInformation("Inserimento nuova chiamata");
                    success = await _gestioneChiamataService.AggiungiChiamataAsync(chiamata);
                    message = "Nuova chiamata inserita.";
                }

                if (success)
                {
                    TempData["msg"] = $"<script>alert('{message}')</script>";
                    return RedirectToAction("Index");
                }
                else
                {
                    _logger.LogError("Operazione fallita");
                    TempData["msg"] = "<script>alert('Errore durante il salvataggio della chiamata')</script>";
                    ModelState.AddModelError("", "Errore durante il salvataggio della chiamata.");
                    return View("Index", chiamata);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il salvataggio della chiamata");
                ModelState.AddModelError("", $"Errore: {ex.Message}");
                return View("Index", chiamata);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Elimina(int id)
        {
            try
            {
                _logger.LogInformation($"Eliminazione chiamata con ID {id}");
                var result = await _gestioneChiamataService.EliminaChiamataAsync(id);
                if (result)
                {
                    TempData["msg"] = "<script>alert('Chiamata eliminata con successo')</script>";
                }
                else
                {
                    TempData["msg"] = "<script>alert('Errore durante l\'eliminazione della chiamata')</script>";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Errore durante l'eliminazione della chiamata con ID {id}");
                TempData["msg"] = $"<script>alert('Errore: {ex.Message}')</script>";
            }

            return RedirectToAction("Index");
        }
    }
}