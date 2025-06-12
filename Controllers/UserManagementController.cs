using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using WebApplicationCentralino.Models;
using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Logging;

namespace WebApplicationCentralino.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UserManagementController : BaseController
    {
        private readonly ILogger<UserManagementController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public UserManagementController(
            ILogger<UserManagementController> logger,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("ApiClient");
                var response = await client.GetAsync("api/users");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var users = JsonSerializer.Deserialize<List<UserInfo>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return View(users);
                }

                _logger.LogError("Errore nel recupero degli utenti: {StatusCode}", response.StatusCode);
                return View(new List<UserInfo>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il recupero degli utenti");
                return View(new List<UserInfo>());
            }
        }

        public IActionResult Create()
        {
            return View(new UserCreateModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserCreateModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var client = _httpClientFactory.CreateClient("ApiClient");
                    var jsonContent = JsonSerializer.Serialize(model);
                    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync("api/users", content);

                    if (response.IsSuccessStatusCode)
                    {
                        return RedirectToAction(nameof(Index));
                    }

                    var errorContent = await response.Content.ReadAsStringAsync();
                    ModelState.AddModelError(string.Empty, $"Errore nella creazione dell'utente: {errorContent}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Errore durante la creazione dell'utente");
                    ModelState.AddModelError(string.Empty, "Si è verificato un errore durante la creazione dell'utente.");
                }
            }
            return View(model);
        }

        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("ApiClient");
                var response = await client.GetAsync($"api/users/{id}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var user = JsonSerializer.Deserialize<UserInfo>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    var editModel = new UserEditModel
                    {
                        Id = user.id,
                        Nome = user.nome,
                        Email = user.email,
                        Ruolo = user.ruolo
                    };

                    return View(editModel);
                }

                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il recupero dell'utente");
                return NotFound();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, UserEditModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Prevent role modification for admin user (id = 1)
                    if (id == 1)
                    {
                        var adminClient = _httpClientFactory.CreateClient("ApiClient");
                        var adminResponse = await adminClient.GetAsync($"api/users/{id}");
                        if (adminResponse.IsSuccessStatusCode)
                        {
                            var adminContent = await adminResponse.Content.ReadAsStringAsync();
                            var user = JsonSerializer.Deserialize<UserInfo>(adminContent, new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });
                            model.Ruolo = user.ruolo; // Keep original role
                        }
                    }

                    var client = _httpClientFactory.CreateClient("ApiClient");
                    
                    // Se è stata fornita una nuova password, includila nella richiesta
                    var updateData = new
                    {
                        id = model.Id,
                        nome = model.Nome,
                        email = model.Email,
                        ruolo = model.Ruolo,
                        password = model.Password // Sarà null se non fornita
                    };
                    
                    var jsonContent = JsonSerializer.Serialize(updateData);
                    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                    var response = await client.PutAsync($"api/users/{id}", content);

                    if (response.IsSuccessStatusCode)
                    {
                        TempData["SuccessMessage"] = "Utente aggiornato con successo" + 
                            (!string.IsNullOrEmpty(model.Password) ? " (password modificata)" : "");
                        return RedirectToAction(nameof(Index));
                    }

                    var errorContent = await response.Content.ReadAsStringAsync();
                    ModelState.AddModelError(string.Empty, $"Errore nell'aggiornamento dell'utente: {errorContent}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Errore durante l'aggiornamento dell'utente");
                    ModelState.AddModelError(string.Empty, "Si è verificato un errore durante l'aggiornamento dell'utente.");
                }
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("ApiClient");
                var response = await client.DeleteAsync($"api/users/{id}");

                if (response.IsSuccessStatusCode)
                {
                    return RedirectToAction(nameof(Index));
                }

                _logger.LogError("Errore nell'eliminazione dell'utente: {StatusCode}", response.StatusCode);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante l'eliminazione dell'utente");
                return RedirectToAction(nameof(Index));
            }
        }
    }
}