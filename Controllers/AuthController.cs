using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using WebApplicationCentralino.Models;

namespace WebApplicationCentralino.Controllers
{
    public class AuthController : Controller
    {
        private readonly ILogger<AuthController> _logger;

        public AuthController(ILogger<AuthController> logger)
        {
            _logger = logger;
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
                _logger.LogInformation("Tentativo di login per email: {Email}", model.Email);
                
                // Calcoliamo l'hash della password inserita
                var passwordHash = ComputeSha256Hash(model.Password);
                _logger.LogInformation("Hash password inserita: {Hash}", passwordHash);
                
                // Hash atteso per admin123
                var expectedHash = "ef92b778bafe771e89245b89ecbc08a44a4e166c06659911881f383d4473e94f";
                _logger.LogInformation("Hash atteso: {Hash}", expectedHash);

                // Verifichiamo le credenziali
                if (model.Email == "admin@example.com" && passwordHash == expectedHash)
                {
                    _logger.LogInformation("Credenziali valide, procedo con l'autenticazione");
                    
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, "Admin"),
                        new Claim(ClaimTypes.Email, model.Email),
                        new Claim(ClaimTypes.Role, "Admin")
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

                    _logger.LogInformation("Autenticazione completata con successo");

                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }
                    return RedirectToAction("Index", "Home");
                }

                _logger.LogWarning("Tentativo di login fallito per email: {Email}", model.Email);
                ModelState.AddModelError(string.Empty, "Email o password non validi.");
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
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        private string ComputeSha256Hash(string rawData)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // Convertiamo la stringa in bytes usando ASCII (come specificato nell'esempio)
                byte[] bytes = Encoding.ASCII.GetBytes(rawData);
                
                // Calcoliamo l'hash
                byte[] hashBytes = sha256Hash.ComputeHash(bytes);
                
                // Convertiamo l'hash in una stringa esadecimale
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    builder.Append(hashBytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
} 