using System.Net.Http.Headers;

namespace WebApplicationCentralino.Services
{
    public class JwtTokenHandler : DelegatingHandler
    {
        private readonly JwtTokenService _tokenService;

        public JwtTokenHandler(JwtTokenService tokenService)
        {
            _tokenService = tokenService;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _tokenService.AddTokenToRequest(request);
            return await base.SendAsync(request, cancellationToken);
        }
    }
} 