using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using WebApplicationCentralino.Models;

namespace WebApplicationCentralino.Controllers
{
    public class AuthController : Controller
    {
        private readonly ILogger<AuthController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public AuthController(ILogger<AuthController> logger, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginModel model, string returnUrl = null)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _logger.LogInformation("Tentativo di login per email: {Email}", model.Email);

                    // Crea il client HTTP
                    var client = _httpClientFactory.CreateClient("ApiClient");

                    // Prepara la richiesta
                    var loginRequest = new
                    {
                        Email = model.Email,
                        Password = model.Password
                    };

                    var jsonContent = JsonSerializer.Serialize(loginRequest);
                    _logger.LogInformation("Request body: {Json}", jsonContent);

                    var content = new StringContent(
                        jsonContent,
                        Encoding.UTF8,
                        "application/json");

                    // Chiama l'API di login
                    var apiUrl = "api/auth/login";
                    _logger.LogInformation("Chiamata API a: {Url}", apiUrl);

                    var response = await client.PostAsync(apiUrl, content);
                    _logger.LogInformation("Risposta API - Status: {Status}", response.StatusCode);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        _logger.LogInformation("Risposta API - Content: {Content}", responseContent);

                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        };

                        var authResponse = JsonSerializer.Deserialize<AuthResponse>(responseContent, options);

                        if (authResponse?.Success == true)
                        {
                            _logger.LogInformation("Login riuscito per utente: {Email}", model.Email);

                            // Store the JWT token in a cookie
                            var tokenCookie = new CookieOptions
                            {
                                HttpOnly = true,
                                Secure = true,
                                SameSite = SameSiteMode.Strict,
                                Expires = DateTime.Now.AddMinutes(60) // Match the token expiration
                            };

                            Response.Cookies.Append("JWTToken", authResponse.Token, tokenCookie);

                            // Create claims for the user
                            var claims = new List<Claim>
                            {
                                new Claim(ClaimTypes.Name, model.Email),
                                new Claim(ClaimTypes.Email, model.Email),
                                new Claim(ClaimTypes.Role, authResponse.Role),
                                new Claim("JWTToken", authResponse.Token)
                            };

                            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                            var authProperties = new AuthenticationProperties
                            {
                                IsPersistent = model.RememberMe
                            };

                            await HttpContext.SignInAsync(
                                CookieAuthenticationDefaults.AuthenticationScheme,
                                new ClaimsPrincipal(claimsIdentity),
                                authProperties);

                            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                            {
                                return Redirect(returnUrl);
                            }
                            return RedirectToAction("Index", "Home");
                        }
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        _logger.LogWarning("Login fallito - Status: {Status}, Content: {Content}",
                            response.StatusCode, errorContent);
                        ModelState.AddModelError(string.Empty, "Email o password non validi.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Errore durante il login");
                    ModelState.AddModelError(string.Empty, "Si è verificato un errore durante il login. Riprova più tardi.");
                }
            }
            else
            {
                _logger.LogWarning("ModelState non valido: {Errors}",
                    string.Join(", ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)));
            }

            return View(model);
        }

        public async Task<IActionResult> Logout()
        {
            // Remove the JWT token cookie
            Response.Cookies.Delete("JWTToken");
            
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }

    public class AuthResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("token")]
        public string Token { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("role")]
        public string Role { get; set; }
    }
}