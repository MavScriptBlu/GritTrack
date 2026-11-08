using System.Net;
using System.Text;
using GritTrack.Services;
using GritTrack.Services.Api;
using Microsoft.Maui.Storage;

namespace GritTrack.App.Tests.Support;

// the pretend phone: a secure storage that lives in a dictionary and can be told to break
public sealed class FakeSecureStorage : ISecureStorage
{
    public Dictionary<string, string> Values { get; } = new();

    public bool ThrowOnRead { get; set; }
    public bool ThrowOnWrite { get; set; }
    public bool ThrowOnRemove { get; set; }

    public Task<string?> GetAsync(string key)
    {
        if (ThrowOnRead)
            throw new InvalidOperationException("secure storage is locked");

        return Task.FromResult(Values.TryGetValue(key, out var value) ? value : null);
    }

    public Task SetAsync(string key, string value)
    {
        if (ThrowOnWrite)
            throw new InvalidOperationException("secure storage is full");

        Values[key] = value;
        return Task.CompletedTask;
    }

    public bool Remove(string key)
    {
        if (ThrowOnRemove)
            throw new InvalidOperationException("secure storage can't delete");

        return Values.Remove(key);
    }
}

// what the app sent over the wire, saved so a test can look at it afterwards
public sealed record RecordedRequest(HttpMethod Method, Uri? Uri, string? Authorization, string Body)
{
    public string Path => Uri?.AbsolutePath ?? string.Empty;
    public string PathAndQuery => Uri?.PathAndQuery ?? string.Empty;
}

// stands in for the API. hands back whatever the test wants and remembers every request
public sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

    public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        _responder = responder;
    }

    public List<RecordedRequest> Requests { get; } = new();

    // always answer the same thing
    public static StubHttpMessageHandler Always(HttpStatusCode statusCode, string? json = null)
        => new(_ => JsonResponse(statusCode, json));

    // pretend the network is down
    public static StubHttpMessageHandler Throwing(Exception exception)
        => new(_ => throw exception);

    public static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, string? json = null)
    {
        var response = new HttpResponseMessage(statusCode);
        if (json is not null)
            response.Content = new StringContent(json, Encoding.UTF8, "application/json");
        return response;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // read the body now, before anything disposes the request
        var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
        Requests.Add(new RecordedRequest(request.Method, request.RequestUri, request.Headers.Authorization?.ToString(), body));

        return _responder(request);
    }
}

// the API address the app would use. real code adds the path to it
public static class TestApi
{
    public static HttpClient CreateClient(HttpMessageHandler handler) => new(handler, disposeHandler: false)
    {
        BaseAddress = new Uri("https://localhost:5227/")
    };
}

// IHttpClientFactory that always hands out a client wired to the stub, and remembers which name was asked for
public sealed class StubHttpClientFactory : IHttpClientFactory
{
    private readonly HttpMessageHandler _handler;

    public StubHttpClientFactory(HttpMessageHandler handler)
    {
        _handler = handler;
    }

    public List<string> RequestedNames { get; } = new();

    public HttpClient CreateClient(string name)
    {
        RequestedNames.Add(name);
        return TestApi.CreateClient(_handler);
    }
}

// the pretend Shell. remembers where the app tried to go, and can act like the login page when it gets there
public sealed class FakeNavigationService : INavigationService
{
    public List<string> Routes { get; } = new();

    // runs when the app navigates. a test uses it to play the part of the login page
    public Action<string>? WhenNavigated { get; set; }

    public Task GoToAsync(string route, IDictionary<string, object>? parameters = null)
    {
        Routes.Add(route);
        WhenNavigated?.Invoke(route);
        return Task.CompletedTask;
    }

    public Task ShowAlertAsync(string title, string message) => Task.CompletedTask;
}

// the smallest possible IAuthService, for tests of things that only need a token
public sealed class FakeAuthService : IAuthService
{
    public string? Token { get; set; }
    public int TokenRequestCount { get; private set; }

    public bool IsSignedIn => Token is not null;
    public string UserName => string.Empty;

#pragma warning disable CS0067 // never raised, the interface just asks for it
    public event EventHandler? SignInChanged;
#pragma warning restore CS0067

    public Task<string?> GetTokenAsync()
    {
        TokenRequestCount++;
        return Task.FromResult(Token);
    }

    public Task RestoreAsync() => Task.CompletedTask;
    public Task SignInAsync(string userName, string password) => Task.CompletedTask;
    public Task SignOutAsync() => Task.CompletedTask;
    public Task<bool> RequestLoginAsync() => Task.FromResult(false);
    public void CompleteLogin(bool signedIn) { }
    public Task<T> RunWriteAsync<T>(Func<Task<T>> write) => write();
}

// every test that touches the phone-only statics (secure storage, the app data folder) starts from this.
// it gives each test a fresh fake storage and its own empty folder, and cleans up after
[Collection("MauiStatics")]
public abstract class PhoneStaticsTestBase : IDisposable
{
    protected PhoneStaticsTestBase()
    {
        SecureStorageFake = new FakeSecureStorage();
        SecureStorage.Default = SecureStorageFake;

        AppDataFolder = Path.Combine(Path.GetTempPath(), "grittrack-app-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(AppDataFolder);
        FileSystem.AppDataDirectory = AppDataFolder;
    }

    protected FakeSecureStorage SecureStorageFake { get; }

    protected string AppDataFolder { get; }

    protected string AssignmentFilePath => Path.Combine(AppDataFolder, "local-assignments.json");

    public void Dispose()
    {
        if (Directory.Exists(AppDataFolder))
            Directory.Delete(AppDataFolder, recursive: true);
    }
}
