using System.Net;
using System.Text.Json;
using GritTrack.App.Tests.Support;
using GritTrack.Services;
using GritTrack.Services.Api;

namespace GritTrack.App.Tests;

// the whole sign in story: log in, remember it, come back tomorrow, sign out, and the
// "you got logged out in the middle of a save" flow that opens the login page and carries on
public class AuthServiceTests : PhoneStaticsTestBase
{
    // a token that is good for a long time, and one that already ran out
    private const string GoodLoginJson = """{"token":"good.login.token","expiresAtUtc":"2999-01-01T00:00:00Z"}""";
    private const string ExpiredLoginJson = """{"token":"old.login.token","expiresAtUtc":"2000-01-01T00:00:00Z"}""";

    private readonly FakeNavigationService _navigation = new();

    private AuthService CreateAuthService(StubHttpMessageHandler handler) => new(new StubHttpClientFactory(handler), _navigation);

    private AuthService CreateSignedInAuthService()
    {
        var authService = CreateAuthService(StubHttpMessageHandler.Always(HttpStatusCode.OK, GoodLoginJson));
        authService.SignInAsync("demo", "pw").GetAwaiter().GetResult();
        return authService;
    }

    // ---- signing in ----

    [Fact]
    public async Task SignInAsync_MarksMeSignedIn_AndRemembersTheName()
    {
        var authService = CreateAuthService(StubHttpMessageHandler.Always(HttpStatusCode.OK, GoodLoginJson));

        await authService.SignInAsync("  demo  ", "pw");

        Assert.True(authService.IsSignedIn);
        Assert.Equal("demo", authService.UserName);
        Assert.Equal("good.login.token", await authService.GetTokenAsync());
    }

    [Fact]
    public async Task SignInAsync_TellsTheScreensExactlyOnce()
    {
        var authService = CreateAuthService(StubHttpMessageHandler.Always(HttpStatusCode.OK, GoodLoginJson));
        var raisedCount = 0;
        authService.SignInChanged += (_, _) => raisedCount++;

        await authService.SignInAsync("demo", "pw");

        Assert.Equal(1, raisedCount);
    }

    [Fact]
    public async Task SignInAsync_PostsTheNameAndPasswordAsJson_ToTheLoginRoute_UsingTheLoginClient()
    {
        var handler = StubHttpMessageHandler.Always(HttpStatusCode.OK, GoodLoginJson);
        var factory = new StubHttpClientFactory(handler);
        var authService = new AuthService(factory, _navigation);

        await authService.SignInAsync("demo", "my-password");

        var sent = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, sent.Method);
        Assert.Equal("/api/auth/login", sent.Path);
        using var body = JsonDocument.Parse(sent.Body);
        Assert.Equal("demo", body.RootElement.GetProperty("userName").GetString());
        Assert.Equal("my-password", body.RootElement.GetProperty("password").GetString());
        // the login client has no bearer handler, because login is how you get the token in the first place
        Assert.Equal(AuthService.HttpClientName, Assert.Single(factory.RequestedNames));
        Assert.Null(sent.Authorization);
    }

    [Fact]
    public async Task SignInAsync_SavesTheTokenInSecureStorage_ButNeverThePassword()
    {
        var authService = CreateAuthService(StubHttpMessageHandler.Always(HttpStatusCode.OK, GoodLoginJson));

        await authService.SignInAsync("demo", "my-super-secret-password");

        Assert.Equal(3, SecureStorageFake.Values.Count);
        Assert.Contains("good.login.token", SecureStorageFake.Values.Values);
        Assert.Contains("demo", SecureStorageFake.Values.Values);
        Assert.DoesNotContain(SecureStorageFake.Values.Values, value => value.Contains("my-super-secret-password"));
    }

    [Fact]
    public async Task SignInAsync_WrongPassword_SaysSoInPlainWords()
    {
        var authService = CreateAuthService(StubHttpMessageHandler.Always(HttpStatusCode.Unauthorized));
        var raisedCount = 0;
        authService.SignInChanged += (_, _) => raisedCount++;

        var exception = await Assert.ThrowsAsync<ApiValidationException>(() => authService.SignInAsync("demo", "wrong"));

        // a 401 from the LOGIN route means bad credentials, not "please log in", so it must not be an AuthenticationRequiredException
        Assert.Equal("User name or password is wrong.", exception.Message);
        Assert.False(authService.IsSignedIn);
        Assert.Equal(0, raisedCount);
        Assert.Empty(SecureStorageFake.Values);
    }

    [Fact]
    public async Task SignInAsync_AuthTurnedOffOnTheServer_404_IsAFriendlyRequestError()
    {
        var authService = CreateAuthService(StubHttpMessageHandler.Always(HttpStatusCode.NotFound));

        var exception = await Assert.ThrowsAsync<ApiRequestException>(() => authService.SignInAsync("demo", "pw"));

        Assert.False(authService.IsSignedIn);
        Assert.NotEmpty(exception.Message);
    }

    [Fact]
    public async Task SignInAsync_ServerError_IsAFriendlyRequestError()
    {
        var authService = CreateAuthService(StubHttpMessageHandler.Always(HttpStatusCode.InternalServerError));

        var exception = await Assert.ThrowsAsync<ApiRequestException>(() => authService.SignInAsync("demo", "pw"));

        Assert.Contains("error 500", exception.Message);
        Assert.False(authService.IsSignedIn);
    }

    [Fact]
    public async Task SignInAsync_ServerNotRunning_IsAFriendlyRequestError()
    {
        var authService = CreateAuthService(StubHttpMessageHandler.Throwing(new HttpRequestException("refused")));

        var exception = await Assert.ThrowsAsync<ApiRequestException>(() => authService.SignInAsync("demo", "pw"));

        Assert.Contains("Couldn't reach the GritTrack server", exception.Message);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("""{"token":"","expiresAtUtc":"2999-01-01T00:00:00Z"}""")]
    [InlineData("""{"token":"   ","expiresAtUtc":"2999-01-01T00:00:00Z"}""")]
    [InlineData("""{"expiresAtUtc":"2999-01-01T00:00:00Z"}""")]
    public async Task SignInAsync_AnAnswerWithNoToken_IsNotALogin(string json)
    {
        var authService = CreateAuthService(StubHttpMessageHandler.Always(HttpStatusCode.OK, json));

        var exception = await Assert.ThrowsAsync<ApiRequestException>(() => authService.SignInAsync("demo", "pw"));

        Assert.Contains("not with a login", exception.Message);
        Assert.False(authService.IsSignedIn);
        Assert.Empty(SecureStorageFake.Values);
    }

    [Fact]
    public async Task SignInAsync_StillWorksForThisRun_WhenSecureStorageCantSave()
    {
        SecureStorageFake.ThrowOnWrite = true;
        var authService = CreateAuthService(StubHttpMessageHandler.Always(HttpStatusCode.OK, GoodLoginJson));

        await authService.SignInAsync("demo", "pw");

        Assert.True(authService.IsSignedIn);
        Assert.Equal("good.login.token", await authService.GetTokenAsync());
    }

    [Fact]
    public async Task SignInAsync_ATokenThatAlreadyExpired_CountsAsSignedOut()
    {
        var authService = CreateAuthService(StubHttpMessageHandler.Always(HttpStatusCode.OK, ExpiredLoginJson));

        await authService.SignInAsync("demo", "pw");

        Assert.False(authService.IsSignedIn);
        Assert.Null(await authService.GetTokenAsync());
    }

    [Fact]
    public async Task SignInAsync_ServerTimesInOtherZones_AreConvertedToUtc()
    {
        // 2999-01-01 10:00 at +05:00 is 05:00 UTC, still far in the future either way
        var authService = CreateAuthService(StubHttpMessageHandler.Always(HttpStatusCode.OK,
            """{"token":"zone.token","expiresAtUtc":"2999-01-01T10:00:00+05:00"}"""));

        await authService.SignInAsync("demo", "pw");

        Assert.True(authService.IsSignedIn);
        Assert.Equal("2999-01-01T05:00:00.0000000Z", SecureStorageFake.Values["grittrack_token_expires"]);
    }

    // ---- signing out ----

    [Fact]
    public async Task SignOutAsync_ForgetsEverything_AndTellsTheScreens()
    {
        var authService = CreateSignedInAuthService();
        var raisedCount = 0;
        authService.SignInChanged += (_, _) => raisedCount++;

        await authService.SignOutAsync();

        Assert.False(authService.IsSignedIn);
        Assert.Equal(string.Empty, authService.UserName);
        Assert.Null(await authService.GetTokenAsync());
        Assert.Empty(SecureStorageFake.Values);
        Assert.Equal(1, raisedCount);
    }

    [Fact]
    public async Task SignOutAsync_WhenNobodyIsSignedIn_DoesNotFakeAnEvent()
    {
        var authService = CreateAuthService(StubHttpMessageHandler.Always(HttpStatusCode.OK));
        var raisedCount = 0;
        authService.SignInChanged += (_, _) => raisedCount++;

        await authService.SignOutAsync();

        Assert.Equal(0, raisedCount);
    }

    [Fact]
    public async Task SignOutAsync_StillSignsOut_WhenSecureStorageCantDelete()
    {
        var authService = CreateSignedInAuthService();
        SecureStorageFake.ThrowOnRemove = true;

        await authService.SignOutAsync();

        Assert.False(authService.IsSignedIn);
    }

    // ---- coming back later ----

    [Fact]
    public async Task RestoreAsync_PicksUpTheLoginFromLastTime()
    {
        SecureStorageFake.Values["grittrack_token"] = "saved.token";
        SecureStorageFake.Values["grittrack_token_expires"] = DateTime.UtcNow.AddHours(1).ToString("O");
        SecureStorageFake.Values["grittrack_user"] = "demo";
        var authService = CreateAuthService(StubHttpMessageHandler.Always(HttpStatusCode.OK));
        var raisedCount = 0;
        authService.SignInChanged += (_, _) => raisedCount++;

        await authService.RestoreAsync();

        Assert.True(authService.IsSignedIn);
        Assert.Equal("demo", authService.UserName);
        Assert.Equal("saved.token", await authService.GetTokenAsync());
        Assert.Equal(1, raisedCount);
    }

    [Fact]
    public async Task RestoreAsync_ThrowsAwayALoginThatRanOut()
    {
        SecureStorageFake.Values["grittrack_token"] = "stale.token";
        SecureStorageFake.Values["grittrack_token_expires"] = DateTime.UtcNow.AddHours(-1).ToString("O");
        SecureStorageFake.Values["grittrack_user"] = "demo";
        var authService = CreateAuthService(StubHttpMessageHandler.Always(HttpStatusCode.OK));

        await authService.RestoreAsync();

        Assert.False(authService.IsSignedIn);
        Assert.Null(await authService.GetTokenAsync());
        Assert.Empty(SecureStorageFake.Values);
    }

    [Fact]
    public async Task RestoreAsync_NothingSaved_JustMeansSignedOut()
    {
        var authService = CreateAuthService(StubHttpMessageHandler.Always(HttpStatusCode.OK));

        await authService.RestoreAsync();

        Assert.False(authService.IsSignedIn);
    }

    [Fact]
    public async Task RestoreAsync_ADateThatCantBeRead_JustMeansSignedOut()
    {
        SecureStorageFake.Values["grittrack_token"] = "saved.token";
        SecureStorageFake.Values["grittrack_token_expires"] = "not a date";
        var authService = CreateAuthService(StubHttpMessageHandler.Always(HttpStatusCode.OK));

        await authService.RestoreAsync();

        Assert.False(authService.IsSignedIn);
    }

    [Fact]
    public async Task RestoreAsync_ATokenWithNoDate_JustMeansSignedOut()
    {
        SecureStorageFake.Values["grittrack_token"] = "saved.token";
        var authService = CreateAuthService(StubHttpMessageHandler.Always(HttpStatusCode.OK));

        await authService.RestoreAsync();

        Assert.False(authService.IsSignedIn);
    }

    [Fact]
    public async Task RestoreAsync_LockedSecureStorage_StartsSignedOutInsteadOfCrashing()
    {
        SecureStorageFake.ThrowOnRead = true;
        var authService = CreateAuthService(StubHttpMessageHandler.Always(HttpStatusCode.OK));

        var exception = await Record.ExceptionAsync(() => authService.RestoreAsync());

        Assert.Null(exception);
        Assert.False(authService.IsSignedIn);
    }

    // ---- the login page handshake ----

    [Fact]
    public async Task RequestLoginAsync_OpensTheLoginPage_AndReturnsWhatItReports()
    {
        var authService = CreateAuthService(StubHttpMessageHandler.Always(HttpStatusCode.OK));
        // pretend the person typed their password and signed in
        _navigation.WhenNavigated = _ => authService.CompleteLogin(true);

        var signedIn = await authService.RequestLoginAsync();

        Assert.True(signedIn);
        Assert.Equal(new[] { Routes.Login }, _navigation.Routes);
    }

    [Fact]
    public async Task RequestLoginAsync_ReturnsFalse_WhenThePageIsBackedOutOf()
    {
        var authService = CreateAuthService(StubHttpMessageHandler.Always(HttpStatusCode.OK));
        _navigation.WhenNavigated = _ => authService.CompleteLogin(false);

        Assert.False(await authService.RequestLoginAsync());
    }

    [Fact]
    public async Task RequestLoginAsync_ASecondRequest_CancelsTheOneStillWaiting()
    {
        var authService = CreateAuthService(StubHttpMessageHandler.Always(HttpStatusCode.OK));

        // the first login page never reports back
        var first = authService.RequestLoginAsync();
        Assert.False(first.IsCompleted);

        // the second one replaces it. the first must not hang forever
        _navigation.WhenNavigated = _ => authService.CompleteLogin(true);
        var second = await authService.RequestLoginAsync();

        Assert.False(await first);
        Assert.True(second);
    }

    [Fact]
    public void CompleteLogin_WithNobodyWaiting_DoesNothing()
    {
        var authService = CreateAuthService(StubHttpMessageHandler.Always(HttpStatusCode.OK));

        var exception = Record.Exception(() => authService.CompleteLogin(true));

        Assert.Null(exception);
    }

    // ---- RunWriteAsync: the save that gets logged out halfway ----

    [Fact]
    public async Task RunWriteAsync_PassesTheResultStraightThrough_WhenNothingGoesWrong()
    {
        var authService = CreateSignedInAuthService();

        var result = await authService.RunWriteAsync(() => Task.FromResult("saved"));

        Assert.Equal("saved", result);
        Assert.Empty(_navigation.Routes);
        Assert.True(authService.IsSignedIn);
    }

    [Fact]
    public async Task RunWriteAsync_On401_ForgetsTheOldToken_OpensLogin_ThenRunsTheSameWriteAgain()
    {
        var authService = CreateSignedInAuthService();
        _navigation.WhenNavigated = _ => authService.CompleteLogin(true);
        var attemptCount = 0;

        var result = await authService.RunWriteAsync(() =>
        {
            attemptCount++;
            if (attemptCount == 1)
                throw new AuthenticationRequiredException();
            return Task.FromResult("saved after login");
        });

        Assert.Equal("saved after login", result);
        Assert.Equal(2, attemptCount);
        Assert.Equal(new[] { Routes.Login }, _navigation.Routes);
        // the turned down token was thrown away (the fake login page didn't sign in again)
        Assert.False(authService.IsSignedIn);
    }

    [Fact]
    public async Task RunWriteAsync_WhenLoginIsCancelled_ThrowsLoginCancelled_AndDoesNotRunTheWriteAgain()
    {
        var authService = CreateAuthService(StubHttpMessageHandler.Always(HttpStatusCode.OK));
        _navigation.WhenNavigated = _ => authService.CompleteLogin(false);
        var attemptCount = 0;

        await Assert.ThrowsAsync<LoginCancelledException>(() => authService.RunWriteAsync<string>(() =>
        {
            attemptCount++;
            throw new AuthenticationRequiredException();
        }));

        Assert.Equal(1, attemptCount);
    }

    [Fact]
    public async Task RunWriteAsync_IfTheServerStillSays401AfterLogin_TheErrorReachesTheCaller()
    {
        // one retry only. never an endless login loop
        var authService = CreateAuthService(StubHttpMessageHandler.Always(HttpStatusCode.OK));
        _navigation.WhenNavigated = _ => authService.CompleteLogin(true);
        var attemptCount = 0;

        await Assert.ThrowsAsync<AuthenticationRequiredException>(() => authService.RunWriteAsync<string>(() =>
        {
            attemptCount++;
            throw new AuthenticationRequiredException();
        }));

        Assert.Equal(2, attemptCount);
        Assert.Single(_navigation.Routes);
    }

    [Fact]
    public async Task RunWriteAsync_OtherErrors_PassThroughWithoutOpeningLogin()
    {
        var authService = CreateSignedInAuthService();

        await Assert.ThrowsAsync<ApiRequestException>(() =>
            authService.RunWriteAsync<string>(() => throw new ApiRequestException("server is down")));

        Assert.Empty(_navigation.Routes);
        Assert.True(authService.IsSignedIn);
    }

    [Fact]
    public async Task RunWriteAsync_TellsTheScreensWhenTheOldTokenIsThrownAway()
    {
        var authService = CreateSignedInAuthService();
        _navigation.WhenNavigated = _ => authService.CompleteLogin(true);
        var raisedCount = 0;
        authService.SignInChanged += (_, _) => raisedCount++;
        var attemptCount = 0;

        await authService.RunWriteAsync(() =>
        {
            attemptCount++;
            if (attemptCount == 1)
                throw new AuthenticationRequiredException();
            return Task.FromResult(true);
        });

        // the top bar's "Sign out" button has to flip back to "Sign in"
        Assert.Equal(1, raisedCount);
    }
}
