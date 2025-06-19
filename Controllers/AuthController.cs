using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net.Http.Headers;
using WebApplicationCentralino.Models;
using WebApplicationCentralino.Services;

namespace WebApplicationCentralino.Controllers
{
    public class AuthController : Controller
    {
        private readonly ILogger<AuthController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILoginAttemptService _loginAttemptService;

        public AuthController(
            ILogger<AuthController> logger, 
            IHttpClientFactory httpClientFactory, 
            IConfiguration configuration,
            ILoginAttemptService loginAttemptService)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _loginAttemptService = loginAttemptService;
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
                    // Check if user is locked out
                    if (_loginAttemptService.IsUserLocked(model.Email))
                    {
                        var lockoutDuration = _configuration.GetValue<int>("Authentication:LockoutDuration", 10);
                        ModelState.AddModelError(string.Empty, 
                            $"Account bloccato per troppi tentativi falliti. Riprova tra {lockoutDuration} minuti.");
                        return View(model);
                    }

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
                            
                            // Reset failed attempts on successful login
                            _loginAttemptService.ResetAttempts(model.Email);

                            // Store the JWT token in a cookie
                            var tokenCookie = new CookieOptions
                            {
                                HttpOnly = true,
                                Secure = true,
                                SameSite = SameSiteMode.Strict,
                                Expires = DateTime.Now.Add(_configuration.GetValue<TimeSpan>("Authentication:ExpireTimeSpan", TimeSpan.FromHours(8)))
                            };

                            Response.Cookies.Append("JWTToken", authResponse.Token, tokenCookie);

                            // Recupera il nome dell'utente dall'API
                            string userName = model.Email; // Fallback all'email
                            try
                            {
                                // Crea un nuovo client HTTP con il token appena ricevuto
                                var apiBaseUrl = _configuration.GetValue<string>("ApiSettings:BaseUrl", "http://10.36.150.250:5000/");
                                var profileClient = _httpClientFactory.CreateClient();
                                profileClient.BaseAddress = new Uri(apiBaseUrl);
                                profileClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                                profileClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResponse.Token);
                                
                                var profileResponse = await profileClient.GetAsync("api/profile/profile");
                                if (profileResponse.IsSuccessStatusCode)
                                {
                                    var profileContent = await profileResponse.Content.ReadAsStringAsync();
                                    var userInfo = JsonSerializer.Deserialize<UserInfo>(profileContent, new JsonSerializerOptions
                                    {
                                        PropertyNameCaseInsensitive = true
                                    });
                                    
                                    if (userInfo != null && !string.IsNullOrEmpty(userInfo.nome))
                                    {
                                        userName = userInfo.nome;
                                        _logger.LogInformation("Nome utente recuperato durante login: {Nome}", userName);
                                    }
                                }
                                else
                                {
                                    _logger.LogWarning("Errore nel recupero del profilo durante login: {StatusCode}", profileResponse.StatusCode);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Impossibile recuperare il nome utente durante login, uso email come fallback");
                            }

                            // Create claims for the user
                            var claims = new List<Claim>
                            {
                                new Claim(ClaimTypes.Name, userName),
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
                        
                        // Record failed attempt
                        _loginAttemptService.RecordFailedAttempt(model.Email);
                        
                        var remainingAttempts = _loginAttemptService.GetRemainingAttempts(model.Email);
                        ModelState.AddModelError(string.Empty, 
                            $"Email o password non validi. Tentativi rimanenti: {remainingAttempts}");
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