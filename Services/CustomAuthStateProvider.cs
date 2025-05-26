//using System.Security.Claims;
//using Microsoft.AspNetCore.Components.Authorization;
//using Microsoft.JSInterop;

//namespace WebApplicationCentralino.Services
//{
//    public class CustomAuthStateProvider : AuthenticationStateProvider
//    {
//        private readonly ChiamataService _apiService;
//        private readonly IJSRuntime _jsRuntime;

//        public CustomAuthStateProvider(ChiamataService apiService, IJSRuntime jsRuntime)
//        {
//            _apiService = apiService;
//            _jsRuntime = jsRuntime;
//        }

//        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
//        {
//            try
//            {
//                var token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "authToken");
//                var expiration = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "tokenExpiration");

//                if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(expiration))
//                {
//                    var expirationDate = DateTime.Parse(expiration);
//                    if (expirationDate > DateTime.UtcNow)
//                    {
//                        var identity = new ClaimsIdentity(new[]
//                        {
//                            new Claim(ClaimTypes.Name, "User"),
//                            new Claim(ClaimTypes.Role, "User")
//                        }, "jwt");

//                        var user = new ClaimsPrincipal(identity);
//                        return new AuthenticationState(user);
//                    }
//                }
//            }
//            catch (Exception)
//            {
//                // In caso di errore, consideriamo l'utente come non autenticato
//            }

//            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
//        }

//        public void NotifyUserAuthentication(string token)
//        {
//            var identity = new ClaimsIdentity(new[]
//            {
//                new Claim(ClaimTypes.Name, "User"),
//                new Claim(ClaimTypes.Role, "User")
//            }, "jwt");

//            var user = new ClaimsPrincipal(identity);
//            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
//        }

//        public void NotifyUserLogout()
//        {
//            var identity = new ClaimsIdentity();
//            var user = new ClaimsPrincipal(identity);
//            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
//        }
//    }
//} 