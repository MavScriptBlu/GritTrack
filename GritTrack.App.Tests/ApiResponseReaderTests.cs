using System.Net;
using GritTrack.App.Tests.Support;
using GritTrack.Services.Api;

namespace GritTrack.App.Tests;

// ApiResponseReader turns "the server said something bad" into messages I can put on screen.
// if it gets a status wrong the user sees the wrong advice, so every status gets a test
public class ApiResponseReaderTests
{
    // ---- sending ----

    [Fact]
    public async Task SendAsync_ReturnsTheResponse_WhenTheServerAnswers()
    {
        using var client = TestApi.CreateClient(StubHttpMessageHandler.Always(HttpStatusCode.OK, "[]"));
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/courses");

        using var response = await ApiResponseReader.SendAsync(client, request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SendAsync_TurnsADeadNetwork_IntoAFriendlyMessage()
    {
        using var client = TestApi.CreateClient(StubHttpMessageHandler.Throwing(new HttpRequestException("connection refused")));
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/courses");

        var exception = await Assert.ThrowsAsync<ApiRequestException>(() => ApiResponseReader.SendAsync(client, request));

        Assert.Equal("Couldn't reach the GritTrack server. Check that the API is running and try again.", exception.Message);
    }

    [Fact]
    public async Task SendAsync_TurnsATimeout_IntoTheSameFriendlyMessage()
    {
        using var client = TestApi.CreateClient(StubHttpMessageHandler.Throwing(new TaskCanceledException("timed out")));
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/courses");

        var exception = await Assert.ThrowsAsync<ApiRequestException>(() => ApiResponseReader.SendAsync(client, request));

        Assert.Contains("Couldn't reach the GritTrack server", exception.Message);
    }

    [Fact]
    public async Task SendAsync_LeavesUnexpectedErrorsAlone()
    {
        // only network trouble gets the friendly message. a real bug should still blow up loudly
        using var client = TestApi.CreateClient(StubHttpMessageHandler.Throwing(new InvalidOperationException("bug")));
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/courses");

        await Assert.ThrowsAsync<InvalidOperationException>(() => ApiResponseReader.SendAsync(client, request));
    }

    // ---- statuses ----

    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.Created)]
    [InlineData(HttpStatusCode.NoContent)]
    public async Task EnsureSuccessAsync_DoesNothing_ForAnySuccessStatus(HttpStatusCode statusCode)
    {
        using var response = StubHttpMessageHandler.JsonResponse(statusCode);

        var exception = await Record.ExceptionAsync(() => ApiResponseReader.EnsureSuccessAsync(response));

        Assert.Null(exception);
    }

    [Fact]
    public async Task EnsureSuccessAsync_Unauthorized_MeansPleaseLogIn()
    {
        using var response = StubHttpMessageHandler.JsonResponse(HttpStatusCode.Unauthorized);

        var exception = await Assert.ThrowsAsync<AuthenticationRequiredException>(() => ApiResponseReader.EnsureSuccessAsync(response));

        Assert.Equal("Sign in to make changes.", exception.Message);
    }

    [Fact]
    public async Task EnsureSuccessAsync_BadRequest_ListsEveryFieldMessage()
    {
        const string body = """
            {"title":"One or more validation errors occurred.","status":400,
             "errors":{"name":["Course name is required."],"credits":["Credits has to be between 0 and 6.","Credits is not a number."]}}
            """;
        using var response = StubHttpMessageHandler.JsonResponse(HttpStatusCode.BadRequest, body);

        var exception = await Assert.ThrowsAsync<ApiValidationException>(() => ApiResponseReader.EnsureSuccessAsync(response));

        Assert.Equal(
            new[] { "Course name is required.", "Credits has to be between 0 and 6.", "Credits is not a number." },
            exception.Messages);
        Assert.Contains("Course name is required.", exception.Message);
    }

    [Fact]
    public async Task EnsureSuccessAsync_BadRequest_SkipsBlankMessages()
    {
        const string body = """{"errors":{"name":["", "   ", "Course name is required."]}}""";
        using var response = StubHttpMessageHandler.JsonResponse(HttpStatusCode.BadRequest, body);

        var exception = await Assert.ThrowsAsync<ApiValidationException>(() => ApiResponseReader.EnsureSuccessAsync(response));

        Assert.Equal(new[] { "Course name is required." }, exception.Messages);
    }

    [Theory]
    [InlineData("not json at all")]
    [InlineData("")]
    [InlineData("{}")]
    [InlineData("""{"errors":{}}""")]
    [InlineData("""{"errors":"a string, not an object"}""")]
    public async Task EnsureSuccessAsync_BadRequest_FallsBackToAGenericLine_WhenTheBodyIsNotWhatWeExpect(string body)
    {
        using var response = StubHttpMessageHandler.JsonResponse(HttpStatusCode.BadRequest, body);

        var exception = await Assert.ThrowsAsync<ApiValidationException>(() => ApiResponseReader.EnsureSuccessAsync(response));

        Assert.Equal(new[] { "Some of the course details aren't valid." }, exception.Messages);
    }

    [Fact]
    public async Task EnsureSuccessAsync_Conflict_UsesTheDetailSentence()
    {
        const string body = """{"title":"Duplicate course code","status":409,"detail":"A course with code 'CS-101' already exists."}""";
        using var response = StubHttpMessageHandler.JsonResponse(HttpStatusCode.Conflict, body);

        var exception = await Assert.ThrowsAsync<ApiValidationException>(() => ApiResponseReader.EnsureSuccessAsync(response));

        Assert.Equal("A course with code 'CS-101' already exists.", exception.Message);
    }

    [Fact]
    public async Task EnsureSuccessAsync_Conflict_FallsBackToTheCourseCodeLine()
    {
        using var response = StubHttpMessageHandler.JsonResponse(HttpStatusCode.Conflict, "{}");

        var exception = await Assert.ThrowsAsync<ApiValidationException>(() => ApiResponseReader.EnsureSuccessAsync(response));

        Assert.Equal("That course code is already in use.", exception.Message);
    }

    [Fact]
    public async Task EnsureSuccessAsync_NotFound_TellsMeToRefresh()
    {
        using var response = StubHttpMessageHandler.JsonResponse(HttpStatusCode.NotFound);

        var exception = await Assert.ThrowsAsync<ApiRequestException>(() => ApiResponseReader.EnsureSuccessAsync(response));

        Assert.IsNotType<ApiValidationException>(exception);
        Assert.Equal("That course no longer exists. Pull down to refresh the list.", exception.Message);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError, "500")]
    [InlineData(HttpStatusCode.BadGateway, "502")]
    [InlineData(HttpStatusCode.ServiceUnavailable, "503")]
    [InlineData(HttpStatusCode.Forbidden, "403")]
    public async Task EnsureSuccessAsync_AnythingElse_ShowsTheStatusNumberAndAsksToTryAgain(HttpStatusCode statusCode, string expectedNumber)
    {
        using var response = StubHttpMessageHandler.JsonResponse(statusCode);

        var exception = await Assert.ThrowsAsync<ApiRequestException>(() => ApiResponseReader.EnsureSuccessAsync(response));

        Assert.Contains($"error {expectedNumber}", exception.Message);
        Assert.Contains("Try again", exception.Message);
    }

    [Fact]
    public async Task EnsureSuccessAsync_NeverShowsTheServerBodyForOtherErrors()
    {
        // a 500 body can hold stack traces and connection strings, none of that belongs on a phone screen
        using var response = StubHttpMessageHandler.JsonResponse(HttpStatusCode.InternalServerError, """{"detail":"Server=db;Password=hunter2"}""");

        var exception = await Assert.ThrowsAsync<ApiRequestException>(() => ApiResponseReader.EnsureSuccessAsync(response));

        Assert.DoesNotContain("hunter2", exception.Message);
    }

    // ---- the exception types ----

    [Fact]
    public void ValidationException_IsAlsoARequestException_SoOneCatchHandlesBoth()
    {
        var exception = new ApiValidationException(new[] { "a", "b" });

        Assert.IsAssignableFrom<ApiRequestException>(exception);
        Assert.Equal($"a{Environment.NewLine}b", exception.Message);
    }

    [Fact]
    public void AuthenticationRequired_AndLoginCancelled_AreNotRequestExceptions()
    {
        // they mean "log in", not "the server broke", so they must not be swallowed by a catch for ApiRequestException
        Assert.False(typeof(ApiRequestException).IsAssignableFrom(typeof(AuthenticationRequiredException)));
        Assert.False(typeof(ApiRequestException).IsAssignableFrom(typeof(LoginCancelledException)));
        Assert.Equal("Sign in is needed to save changes.", new LoginCancelledException().Message);
    }

    [Fact]
    public void JsonOptions_UseCamelCase_LikeTheApiDoes()
    {
        var json = System.Text.Json.JsonSerializer.Serialize(new { CourseCode = "CS-101" }, ApiResponseReader.JsonOptions);

        Assert.Equal("""{"courseCode":"CS-101"}""", json);
    }
}
