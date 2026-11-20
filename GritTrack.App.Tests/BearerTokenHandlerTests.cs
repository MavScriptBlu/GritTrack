using System.Net;
using GritTrack.App.Tests.Support;
using GritTrack.Services.Api;

namespace GritTrack.App.Tests;

// the handler stamps the login token on every write and leaves reads alone.
// if it ever stamps a read, or skips a write, the API says 401 and the app looks broken
public class BearerTokenHandlerTests
{
    // runs one request through the handler and returns what actually went out
    private static async Task<RecordedRequest> SendThroughHandler(FakeAuthService authService, HttpMethod method)
    {
        var inner = StubHttpMessageHandler.Always(HttpStatusCode.OK);
        var handler = new BearerTokenHandler(authService) { InnerHandler = inner };
        using var invoker = new HttpMessageInvoker(handler);
        using var request = new HttpRequestMessage(method, "https://localhost:5227/api/courses");

        using var response = await invoker.SendAsync(request, CancellationToken.None);

        return Assert.Single(inner.Requests);
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task Writes_GetTheBearerToken(string methodName)
    {
        var authService = new FakeAuthService { Token = "my.login.token" };

        var sent = await SendThroughHandler(authService, new HttpMethod(methodName));

        Assert.Equal("Bearer my.login.token", sent.Authorization);
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("HEAD")]
    public async Task Reads_GoOutWithNoAuthorizationHeader(string methodName)
    {
        var authService = new FakeAuthService { Token = "my.login.token" };

        var sent = await SendThroughHandler(authService, new HttpMethod(methodName));

        Assert.Null(sent.Authorization);
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("HEAD")]
    public async Task Reads_NeverEvenAskForTheToken(string methodName)
    {
        // browsing is public, so a read shouldn't touch secure storage at all
        var authService = new FakeAuthService { Token = "my.login.token" };

        await SendThroughHandler(authService, new HttpMethod(methodName));

        Assert.Equal(0, authService.TokenRequestCount);
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task Writes_StillGoOut_WhenThereIsNoToken_SoTheServerCanAnswer401(string methodName)
    {
        var authService = new FakeAuthService { Token = null };

        var sent = await SendThroughHandler(authService, new HttpMethod(methodName));

        Assert.Null(sent.Authorization);
        Assert.Equal(1, authService.TokenRequestCount);
    }

    [Fact]
    public async Task TheTokenIsAskedForOncePerWrite_SoAFreshLoginIsPickedUpRightAway()
    {
        var authService = new FakeAuthService { Token = "first" };
        var inner = StubHttpMessageHandler.Always(HttpStatusCode.OK);
        var handler = new BearerTokenHandler(authService) { InnerHandler = inner };
        using var invoker = new HttpMessageInvoker(handler);

        using (var first = new HttpRequestMessage(HttpMethod.Post, "https://localhost:5227/api/courses"))
            (await invoker.SendAsync(first, CancellationToken.None)).Dispose();

        authService.Token = "second";

        using (var second = new HttpRequestMessage(HttpMethod.Post, "https://localhost:5227/api/courses"))
            (await invoker.SendAsync(second, CancellationToken.None)).Dispose();

        Assert.Equal(new[] { "Bearer first", "Bearer second" }, inner.Requests.Select(sent => sent.Authorization));
    }

    [Fact]
    public async Task TheResponseFromTheInnerHandler_IsPassedBackUntouched()
    {
        var authService = new FakeAuthService { Token = "my.login.token" };
        var handler = new BearerTokenHandler(authService)
        {
            InnerHandler = StubHttpMessageHandler.Always(HttpStatusCode.Created, """{"id":7}""")
        };
        using var invoker = new HttpMessageInvoker(handler);
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://localhost:5227/api/courses");

        using var response = await invoker.SendAsync(request, CancellationToken.None);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("""{"id":7}""", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task TheBodyOfTheRequest_IsNotChanged()
    {
        var authService = new FakeAuthService { Token = "my.login.token" };
        var inner = StubHttpMessageHandler.Always(HttpStatusCode.OK);
        var handler = new BearerTokenHandler(authService) { InnerHandler = inner };
        using var invoker = new HttpMessageInvoker(handler);
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://localhost:5227/api/courses")
        {
            Content = new StringContent("""{"name":"Databases"}""")
        };

        (await invoker.SendAsync(request, CancellationToken.None)).Dispose();

        Assert.Equal("""{"name":"Databases"}""", Assert.Single(inner.Requests).Body);
    }
}
