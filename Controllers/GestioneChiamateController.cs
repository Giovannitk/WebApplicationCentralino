using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System;
using WebApplicationCentralino.Services;
using WebApplicationCentralino.Models;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace WebApplicationCentralino.Controllers
{
    public class GestioneChiamateController : Controller
    {
        private readonly GestioneChiamataService _gestioneChiamataService;
        private readonly ContattoService _contattoService; // Aggiunto il servizio contatti
        private readonly ILogger<GestioneChiamateController> _logger;

        public GestioneChiamateController(
            GestioneChiamataService gestioneChiamataService,
            ContattoService contattoService, // Iniettato il servizio contatti
            ILogger<GestioneChiamateController> logger)
        {
            _gestioneChiamataService = gestioneChiamataService;
            _contattoService = contattoService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                if (ViewBag.UniqueID == null)
                    ViewBag.UniqueID = Guid.NewGuid().ToString();

                // Carica le chiamate esistenti per visualizzazione
                var chiamate = await _gestioneChiamataService.GetAllChiamateAsync();
                ViewBag.Chiamate = chiamate;

                // Carica i contatti per le combobox di autocomplete
                var contatti = await _contattoService.GetAllAsync();
                ViewBag.Contatti = contatti;

                _logger.LogInformation($"contatto: {contatti[0].NumeroContatto}");

                return View(new Chiamata
                {
                    DataArrivoChiamata = DateTime.Now.AddSeconds(-DateTime.Now.Second).AddMilliseconds(-DateTime.Now.Millisecond),
                    DataFineChiamata = DateTime.Now.AddMinutes(5).AddSeconds(-DateTime.Now.Second).AddMilliseconds(-DateTime.Now.Millisecond),
                    TipoChiamata = "Uscita", // Imposta default su "Uscita" (0)
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
                // Carica i contatti per le combobox di autocomplete
                var contatti = await _contattoService.GetAllAsync();

                ViewBag.Contatti = contatti.Select(c => new SelectListItem
                {
                    Value = c.NumeroContatto,
                    Text = $"{c.NumeroContatto}"
                }).ToList();

                ViewBag.RagioniSociali = contatti.Select(c => new SelectListItem
                {
                    Value = c.RagioneSociale,
                    Text = $"{c.RagioneSociale}"
                }).DistinctBy(x => x.Value).ToList();

                // JSON per uso JavaScript
                ViewBag.ContattiJson = System.Text.Json.JsonSerializer.Serialize(contatti);

                //_logger.LogInformation($"ciao contatto: {contatti[0].NumeroContatto}");

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
                        TipoChiamata = "Uscita", // Imposta default su "Uscita" (0)
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
            // Assicuriamo che TipoChiamata sia un valore valido (0 o 1)
            if (chiamata.TipoChiamata != "Entrata")
            {
                chiamata.TipoChiamata = "Uscita"; // Default a "Uscita" se valore non valido
            }

            

            // Validazione personalizzata: sia numero che ragione sociale devono essere presenti per chiamante e chiamato
            bool validazioneChiamante = !string.IsNullOrEmpty(chiamata.NumeroChiamante) &&
                                        !string.IsNullOrEmpty(chiamata.RagioneSocialeChiamante);
            bool validazioneChiamato = !string.IsNullOrEmpty(chiamata.NumeroChiamato) &&
                                       !string.IsNullOrEmpty(chiamata.RagioneSocialeChiamato);

            if (!validazioneChiamante || !validazioneChiamato)
            {
                ModelState.AddModelError("", "È necessario inserire sia il numero che la ragione sociale per chiamante e chiamato.");
            }

            if (!validazioneChiamante)
            {
                ModelState.AddModelError("NumeroChiamante", "Inserire **sia** numero che ragione sociale del chiamante.");
                ModelState.AddModelError("RagioneSocialeChiamante", "Inserire **sia** numero che ragione sociale del chiamante.");
            }

            if (!validazioneChiamato)
            {
                ModelState.AddModelError("NumeroChiamato", "Inserire **sia** numero che ragione sociale del chiamato.");
                ModelState.AddModelError("RagioneSocialeChiamato", "Inserire **sia** numero che ragione sociale del chiamato.");
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Il modello non è valido");
                // Ricarichiamo le chiamate e i contatti per mostrare la lista nella stessa vista
                ViewBag.Chiamate = await _gestioneChiamataService.GetAllChiamateAsync();
                ViewBag.Contatti = await _contattoService.GetAllAsync();
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
                if (chiamata.DataArrivoChiamata != default(DateTime))
                {
                    chiamata.DataArrivoChiamata = new DateTime(
                        chiamata.DataArrivoChiamata.Year,
                        chiamata.DataArrivoChiamata.Month,
                        chiamata.DataArrivoChiamata.Day,
                        chiamata.DataArrivoChiamata.Hour,
                        chiamata.DataArrivoChiamata.Minute,
                        chiamata.DataArrivoChiamata.Second
                    );
                }

                if (chiamata.DataFineChiamata != default(DateTime))
                {
                    chiamata.DataFineChiamata = new DateTime(
                        chiamata.DataFineChiamata.Year,
                        chiamata.DataFineChiamata.Month,
                        chiamata.DataFineChiamata.Day,
                        chiamata.DataFineChiamata.Hour,
                        chiamata.DataFineChiamata.Minute,
                        chiamata.DataFineChiamata.Second
                    );
                }

                // Logga i dettagli della chiamata
                _logger.LogInformation($"Chiamata da salvare: ID={chiamata.Id}, " +
                                       $"NumeroChiamante={chiamata.NumeroChiamante}, " +
                                       $"NumeroChiamato={chiamata.NumeroChiamato}, " +
                                       $"DataArrivo={chiamata.DataArrivoChiamata} (Ticks: {chiamata.DataArrivoChiamata.Ticks}), " +
                                       $"DataFine={chiamata.DataFineChiamata} (Ticks: {chiamata.DataFineChiamata.Ticks}), " +
                                       $"TipoChiamata={chiamata.TipoChiamata} ({(chiamata.TipoChiamata == "Uscita" ? "Entrata" : "Uscita")}), " +
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
                    ViewBag.Chiamate = await _gestioneChiamataService.GetAllChiamateAsync();
                    ViewBag.Contatti = await _contattoService.GetAllAsync();
                    return View("Index", chiamata);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il salvataggio della chiamata");
                ModelState.AddModelError("", $"Errore: {ex.Message}");
                ViewBag.Chiamate = await _gestioneChiamataService.GetAllChiamateAsync();
                ViewBag.Contatti = await _contattoService.GetAllAsync();
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
                    TempData["msg"] = "<script>alert('Errore durante l\\'eliminazione della chiamata')</script>";
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