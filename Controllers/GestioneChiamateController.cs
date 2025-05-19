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

                ViewBag.Comuni = GetComuni();

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
                ViewBag.Comuni = GetComuni();

                // Carica le chiamate esistenti per visualizzazione
                var chiamate = await _gestioneChiamataService.GetAllChiamateAsync();
                ViewBag.Chiamate = chiamate;

                // Carica i contatti per le combobox di autocomplete
                var contatti = await _contattoService.GetAllAsync();
                ViewBag.Contatti = contatti;

                // Carica i contatti per le combobox di autocomplete
                //var contatti = await _contattoService.GetAllAsync();

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
                        _logger.LogInformation($"Chiamata trovata: {chiamataEsistente.Id}, {chiamataEsistente.NumeroChiamante}, {chiamataEsistente.Locazione}");
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
        public async Task<IActionResult> Salva(Chiamata chiamata, string LocazionePredefinita)
        {
            // Assicuriamo che TipoChiamata sia un valore valido (0 o 1)
            if (chiamata.TipoChiamata != "Entrata")
            {
                chiamata.TipoChiamata = "Uscita"; // Default a "Uscita" se valore non valido
            }

            if (LocazionePredefinita != "Altro")
            {
                chiamata.Locazione = LocazionePredefinita;
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

            // Validazione date
            var now = DateTime.Now;

            // Controllo che le date non siano nel futuro
            if (chiamata.DataArrivoChiamata > now)
            {
                ModelState.AddModelError("DataArrivoChiamata", "La data di arrivo non può essere nel futuro");
                chiamata.DataArrivoChiamata = now;
            }

            if (chiamata.DataFineChiamata > now)
            {
                ModelState.AddModelError("DataFineChiamata", "La data di fine non può essere nel futuro");
                chiamata.DataFineChiamata = now;
            }

            // Controllo che data fine sia successiva a data arrivo
            if (chiamata.DataFineChiamata < chiamata.DataArrivoChiamata)
            {
                //_logger.LogInformation($"data arrivo {chiamata.DataArrivoChiamata.ToString()} - data fine: {chiamata.DataFineChiamata.ToString()}");
                ModelState.AddModelError("DataFineChiamata", "La data di fine deve essere successiva alla data di arrivo");
                // Imposta data fine a 5 minuti dopo data arrivo
                chiamata.DataFineChiamata = chiamata.DataArrivoChiamata.AddMinutes(5);
            }

            // Controllo che la data non sia troppo nel passato (es. più di 10 anni fa)
            if (chiamata.DataArrivoChiamata < now.AddYears(-10))
            {
                ModelState.AddModelError("DataArrivoChiamata", "La data di arrivo non può essere più vecchia di 10 anni");
                chiamata.DataArrivoChiamata = now;
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Il modello non è valido");

                ViewBag.Comuni = GetComuni();
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

                ViewBag.ContattiJson = System.Text.Json.JsonSerializer.Serialize(contatti);
                ViewBag.Chiamate = await _gestioneChiamataService.GetAllChiamateAsync();

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
                bool isNew = false;

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
                    _logger.LogInformation($"prova {chiamata.NumeroChiamante} - {chiamata.RagioneSocialeChiamante}");
                    success = await _gestioneChiamataService.AggiornaChiamataAsync(chiamata);
                    message = $"Chiamata aggiornata con ID: {chiamata.Id}";
                }
                else
                {
                    // Non esiste: aggiungi nuova
                    _logger.LogInformation("Inserimento nuova chiamata");
                    success = await _gestioneChiamataService.AggiungiChiamataAsync(chiamata);
                    message = "Nuova chiamata inserita con successo.";
                    isNew = true;
                }

                _logger.LogInformation($"provs{chiamata.NumeroChiamante} - {chiamata.RagioneSocialeChiamante}");
                
                if (success)
                {
                    // Usa TempData per mantenere il messaggio tra le richieste
                    TempData["SuccessMessage"] = message;

                    // Se è una nuova chiamata, reindirizza alla pagina Index con un parametro di successo
                    if (isNew)
                    {
                        return RedirectToAction("Index", new { success = true });
                    }
                    else
                    {
                        // Per gli aggiornamenti, puoi decidere se reindirizzare o rimanere sulla stessa pagina
                        return RedirectToAction("Index");
                    }
                }
                else
                {
                    _logger.LogError("Operazione fallita");
                    TempData["ErrorMessage"] = "Errore durante il salvataggio della chiamata";
                    ModelState.AddModelError("", "Errore durante il salvataggio della chiamata.");
                    ViewBag.Chiamate = await _gestioneChiamataService.GetAllChiamateAsync();
                    ViewBag.Contatti = await _contattoService.GetAllAsync();
                    return View("Index", chiamata);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il salvataggio della chiamata");
                TempData["ErrorMessage"] = $"Errore durante il salvataggio: {ex.Message}";
                ModelState.AddModelError("", $"Errore: {ex.Message}");
                ViewBag.Chiamate = await _gestioneChiamataService.GetAllChiamateAsync();
                ViewBag.Contatti = await _contattoService.GetAllAsync();
                return View("Index", chiamata);
            }
        }

        private async Task CaricaViewBag()
        {
            ViewBag.Comuni = GetComuni();

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

            ViewBag.ContattiJson = System.Text.Json.JsonSerializer.Serialize(contatti);
            ViewBag.Chiamate = await _gestioneChiamataService.GetAllChiamateAsync();
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


        private List<SelectListItem> GetComuni()
        {
            var comuni = new List<string>
            {
                "Comune di Ali'",
                "Comune di Ali' Terme",
                "Comune di Antillo",
                "Comune di Barcellona Pozzo di Gotto",
                "Comune di Basico'",
                "Comune di Brolo",
                "Comune di Capizzi",
                "Comune di Capri Leone",
                "Comune di Capo D'Orlando",
                "Comune di Casalvecchio Siculo",
                "Comune di Falcone",
                "Comune di Forza d'Agro'",
                "Comune di Furci Siculo",
                "Comune di Furnari",
                "Comune di Gallodoro",
                "Comune di Itala",
                "Comune di Leni",
                "Comune di Letojanni",
                "Comune di Limina",
                "Comune di Longi",
                "Comune di Mandanici",
                "Comune di Mazzarra S. Andrea",
                "Comune di Messina",
                "Comune di Milazzo",
                "Comune di Mistretta",
                "Comune di Mongiuffi Melia",
                "Comune di Montalbano Elicona",
                "Comune di Motta Camastra",
                "Comune di Nizza di Sicilia",
                "Comune di Novara di Sicilia",
                "Comune di Oliveri",
                "Comune di Reitano",
                "Comune di Pace del Mela",
                "Comune di Patti",
                "Comune di Roccafiorita",
                "Comune di Roccalumera",
                "Comune di Sambuca",
                "Comune di Santa Lucia del Mela",
                "Comune di Saponara",
                "Comune di Scaletta Zanclea",
                "Comune di Spadafora",
                "Comune di Terme Vigliatore",
                "Comune di Torrenova",
                "Comune di Tripi",
                "Comune di Venetico"
            };

            return comuni.Select(c => new SelectListItem { Text = c, Value = c }).ToList();
        }

    }
}