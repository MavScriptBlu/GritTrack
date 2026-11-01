using System.Net.Http.Json;

namespace GritTrack.Services.Api
{
    // keeps the login token in SecureStorage (the phone/PC's protected store),
    // never in plain preferences or a file. the token is a "key card" the API
    // gave us at login; we show it on every write
    public class AuthService : IAuthService
    {
        public const string HttpClientName = "GritTrackAuth";

        private const string TokenKey = "grittrack_token";
        private const string ExpiresKey = "grittrack_token_expires";
        private const string UserNameKey = "grittrack_user";

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly INavigationService _navigation;

        // a copy kept in memory too, so the app still works if secure storage
        // isn't available on this machine
        private string? _token;
        private DateTime _expiresAtUtc;
        private TaskCompletionSource<bool>? _pendingLogin;

        public AuthService(IHttpClientFactory httpClientFactory, INavigationService navigation)
        {
            _httpClientFactory = httpClientFactory;
            _navigation = navigation;
        }

        public event EventHandler? SignInChanged;

        public bool IsSignedIn => _token is not null && _expiresAtUtc > DateTime.UtcNow;

        public string UserName { get; private set; } = string.Empty;

        public async Task RestoreAsync()
        {
            try
            {
                var token = await SecureStorage.Default.GetAsync(TokenKey);
                var expires = await SecureStorage.Default.GetAsync(ExpiresKey);
                var userName = await SecureStorage.Default.GetAsync(UserNameKey);

                if (token is not null && DateTime.TryParse(expires, null, System.Globalization.DateTimeStyles.RoundtripKind, out var expiresAtUtc))
                {
                    _token = token;
                    _expiresAtUtc = expiresAtUtc;
                    UserName = userName ?? string.Empty;
                }
            }
            catch (Exception)
            {
                // secure storage can be missing or locked. start signed out
            }

            if (!IsSignedIn)
                await ClearAsync();
            else
                SignInChanged?.Invoke(this, EventArgs.Empty);
        }

        public Task<string?> GetTokenAsync() => Task.FromResult(IsSignedIn ? _token : null);

        public async Task SignInAsync(string userName, string password)
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/auth/login")
            {
                Content = JsonContent.Create(new LoginRequest { UserName = userName, Password = password }, options: ApiResponseReader.JsonOptions)
            };

            using var response = await ApiResponseReader.SendAsync(client, request);

            // 401 here means a wrong user name or password, not "log in first"
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                throw new ApiValidationException(new[] { "User name or password is wrong." });

            await ApiResponseReader.EnsureSuccessAsync(response);

            var login = await response.Content.ReadFromJsonAsync<LoginResponse>(ApiResponseReader.JsonOptions);
            if (login is null || string.IsNullOrWhiteSpace(login.Token))
                throw new ApiRequestException("The server answered, but not with a login. Try again.");

            _token = login.Token;
            _expiresAtUtc = login.ExpiresAtUtc.ToUniversalTime();
            UserName = userName.Trim();

            try
            {
                await SecureStorage.Default.SetAsync(TokenKey, _token);
                await SecureStorage.Default.SetAsync(ExpiresKey, _expiresAtUtc.ToString("O"));
                await SecureStorage.Default.SetAsync(UserNameKey, UserName);
            }
            catch (Exception)
            {
                // couldn't save it for next time. the login still works for this run
            }

            SignInChanged?.Invoke(this, EventArgs.Empty);
        }

        public async Task SignOutAsync()
        {
            await ClearAsync();
        }

        public async Task<bool> RequestLoginAsync()
        {
            // a login that was left open gets cancelled, so nothing waits forever
            _pendingLogin?.TrySetResult(false);

            var pending = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _pendingLogin = pending;

            await _navigation.GoToAsync(Routes.Login);
            return await pending.Task;
        }

        public void CompleteLogin(bool signedIn)
        {
            _pendingLogin?.TrySetResult(signedIn);
            _pendingLogin = null;
        }

        public async Task<T> RunWriteAsync<T>(Func<Task<T>> write)
        {
            try
            {
                return await write();
            }
            catch (AuthenticationRequiredException)
            {
                // no token, or an old one the server turned down: forget it, ask for a new login
                await ClearAsync();

                if (!await RequestLoginAsync())
                    throw new LoginCancelledException();

                return await write();
            }
        }

        private async Task ClearAsync()
        {
            var wasSignedIn = _token is not null;

            _token = null;
            _expiresAtUtc = DateTime.MinValue;
            UserName = string.Empty;

            try
            {
                SecureStorage.Default.Remove(TokenKey);
                SecureStorage.Default.Remove(ExpiresKey);
                SecureStorage.Default.Remove(UserNameKey);
            }
            catch (Exception)
            {
                // nothing stored, or storage unavailable: either way we're signed out
            }

            if (wasSignedIn)
                SignInChanged?.Invoke(this, EventArgs.Empty);

            await Task.CompletedTask;
        }
    }
}
