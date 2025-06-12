using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using WebApplicationCentralino.Models;
using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace WebApplicationCentralino.Controllers
{
    [Authorize]
    public class ProfileController : BaseController
    {
        private readonly ILogger<ProfileController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public ProfileController(
            ILogger<ProfileController> logger,
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
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
                if (string.IsNullOrEmpty(userEmail))
                {
                    return RedirectToAction("Login", "Auth");
                }

                _logger.LogInformation("Tentativo recupero profilo per utente: {Email}", userEmail);

                var client = _httpClientFactory.CreateClient("ApiClient");
                var response = await client.GetAsync("api/profile/profile");

                _logger.LogInformation("Risposta API profilo - Status: {Status}", response.StatusCode);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("Risposta API profilo - Content: {Content}", content);
                    
                    var user = JsonSerializer.Deserialize<UserInfo>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return View(user);
                }

                _logger.LogError("Errore nel recupero del profilo utente: {StatusCode}", response.StatusCode);
                return View(new UserInfo());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il recupero del profilo utente");
                return View(new UserInfo());
            }
        }

        public IActionResult ChangePassword()
        {
            return View(new ChangePasswordModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
                    if (string.IsNullOrEmpty(userEmail))
                    {
                        return RedirectToAction("Login", "Auth");
                    }

                    var client = _httpClientFactory.CreateClient("ApiClient");
                    
                    var changePasswordRequest = new
                    {
                        currentPassword = model.CurrentPassword,
                        newPassword = model.NewPassword,
                        confirmPassword = model.ConfirmPassword
                    };
                    
                    var jsonContent = JsonSerializer.Serialize(changePasswordRequest);
                    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                    _logger.LogInformation("Tentativo cambio password per utente: {Email}", userEmail);
                    _logger.LogInformation("Request body: {Json}", jsonContent);

                    var response = await client.PostAsync("api/profile/change-password", content);

                    _logger.LogInformation("Risposta API - Status: {Status}", response.StatusCode);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        _logger.LogInformation("Risposta API - Content: {Content}", responseContent);
                        
                        TempData["SuccessMessage"] = "Password modificata con successo";
                        return RedirectToAction(nameof(Index));
                    }

                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Errore nella modifica della password - Status: {Status}, Content: {Content}", 
                        response.StatusCode, errorContent);
                    ModelState.AddModelError(string.Empty, $"Errore nella modifica della password: {errorContent}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Errore durante la modifica della password");
                    ModelState.AddModelError(string.Empty, "Si è verificato un errore durante la modifica della password.");
                }
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> GetUserName()
        {
            try
            {
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
                if (string.IsNullOrEmpty(userEmail))
                {
                    return Json(new { nome = User.Identity.Name });
                }

                var client = _httpClientFactory.CreateClient("ApiClient");
                var response = await client.GetAsync("api/profile/profile");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var user = JsonSerializer.Deserialize<UserInfo>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    
                    return Json(new { nome = user.nome });
                }

                // Fallback: restituisce l'email se non riesce a recuperare il nome
                return Json(new { nome = User.Identity.Name });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il recupero del nome utente");
                // Fallback: restituisce l'email in caso di errore
                return Json(new { nome = User.Identity.Name });
            }
        }
    }
} 