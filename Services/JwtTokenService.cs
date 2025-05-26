using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;

namespace WebApplicationCentralino.Services
{
    public class JwtTokenService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public JwtTokenService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string GetToken()
        {
            return _httpContextAccessor.HttpContext?.Request.Cookies["JWTToken"] ?? string.Empty;
        }

        public void AddTokenToRequest(HttpRequestMessage request)
        {
            var token = GetToken();
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }
    }
} 