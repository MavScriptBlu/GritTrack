using System.Net.Http.Headers;

namespace GritTrack.Services.Api
{
    // sits in the HttpClient's pipeline and adds "Authorization: Bearer <token>"
    // to every write (POST, PUT, DELETE). reads (GET) go out with no header,
    // because browsing is public. if there's no token yet the write goes out
    // anyway: the server answers 401 and the app sends the user to log in
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
            var isRead = request.Method == HttpMethod.Get || request.Method == HttpMethod.Head;
            if (!isRead)
            {
                var token = await _authService.GetTokenAsync();
                if (token is not null)
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
