using System.Net.Http.Headers;

namespace GritTrack.Services.Api
{
    // sits in the HttpClient's pipeline and adds "Authorization: Bearer <token>"
    // to every request once you're signed in. reads (GET) still work without
    // it, because browsing is public. a write with no token goes out anyway:
    // the server answers 401 and the app sends the user to log in
    // (see AuthService.RunWriteAsync)
    public class BearerTokenHandler : DelegatingHandler
    {
        private readonly IAuthService _authService;

        public BearerTokenHandler(IAuthService authService)
        {
            _authService = authService;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var token = await _authService.GetTokenAsync();
            if (token is not null)
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
