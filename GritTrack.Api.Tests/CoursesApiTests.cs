using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using GritTrack.Api.DTOs;

namespace GritTrack.Api.Tests;

// hits the real controllers, auth and validation through an in-memory server.
// xunit makes a fresh instance per test, so every test gets its own empty API
public class CoursesApiTests : IDisposable
{
    private readonly ApiFactory _factory = new();
    private readonly HttpClient _client;

    public CoursesApiTests()
    {
        _client = _factory.CreateApiClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private static CourseRequestDto SampleCourse(string courseCode = "CS201", string grade = "A-") => new()
    {
        Name = "Data Structures",
        CourseCode = courseCode,
        Instructor = "Dr. Rivera",
        Credits = 3,
        CurrentGrade = grade
    };

    // logs in as the demo user and puts the token on every request after
    private async Task SignInAsync()
    {
        var response = await _client.PostAsJsonAsync("api/auth/login",
            new LoginRequestDto { UserName = ApiFactory.DemoUserName, Password = ApiFactory.DemoPassword });
        response.EnsureSuccessStatusCode();

        var login = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.Token);
    }

    private async Task<CourseResponseDto> CreateAsync(CourseRequestDto request)
    {
        var response = await _client.PostAsJsonAsync("api/courses", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CourseResponseDto>())!;
    }

    // ---- login ----

    [Fact]
    public async Task Login_WithCorrectCredentials_ReturnsToken()
    {
        var response = await _client.PostAsJsonAsync("api/auth/login",
            new LoginRequestDto { UserName = ApiFactory.DemoUserName, Password = ApiFactory.DemoPassword });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var login = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        Assert.False(string.IsNullOrWhiteSpace(login!.Token));
        Assert.True(login.ExpiresAtUtc > DateTime.UtcNow);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var response = await _client.PostAsJsonAsync("api/auth/login",
            new LoginRequestDto { UserName = ApiFactory.DemoUserName, Password = "nope" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithMissingFields_Returns400()
    {
        var response = await _client.PostAsJsonAsync("api/auth/login", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---- reading is public ----

    [Fact]
    public async Task GetAll_WithoutToken_Returns200()
    {
        var response = await _client.GetAsync("api/courses");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetById_WhenMissing_Returns404()
    {
        var response = await _client.GetAsync("api/courses/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---- writes need a token ----

    [Fact]
    public async Task Create_WithoutToken_Returns401()
    {
        var response = await _client.PostAsJsonAsync("api/courses", SampleCourse());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithGarbageToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not.a.real.token");

        var response = await _client.PostAsJsonAsync("api/courses", SampleCourse());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Delete_WithoutToken_Returns401()
    {
        var response = await _client.DeleteAsync("api/courses/1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---- create ----

    [Fact]
    public async Task Create_WithToken_Returns201AndCalculatesGradePoints()
    {
        await SignInAsync();

        var response = await _client.PostAsJsonAsync("api/courses", SampleCourse(grade: "A-"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var created = await response.Content.ReadFromJsonAsync<CourseResponseDto>();
        Assert.True(created!.Id > 0);
        Assert.Equal(3.7, created.GradePoints);
        Assert.Equal("CS201", created.CourseCode);
    }

    [Fact]
    public async Task Create_WithDuplicateCourseCode_Returns409()
    {
        await SignInAsync();
        await CreateAsync(SampleCourse("CS201"));

        // same code, different letter case, still counts as a duplicate
        var response = await _client.PostAsJsonAsync("api/courses", SampleCourse("cs201"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithMissingName_Returns400WithCamelCaseFieldName()
    {
        await SignInAsync();

        var response = await _client.PostAsJsonAsync("api/courses",
            new { courseCode = "CS201", credits = 3, currentGrade = "A" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        // the error key should say "name" (what the client sent), not "Name"
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"name\"", body);
    }

    // ---- read back ----

    [Fact]
    public async Task GetById_AfterCreate_ReturnsTheCourse()
    {
        await SignInAsync();
        var created = await CreateAsync(SampleCourse());

        var response = await _client.GetAsync($"api/courses/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var fetched = await response.Content.ReadFromJsonAsync<CourseResponseDto>();
        Assert.Equal(created.Id, fetched!.Id);
    }

    [Fact]
    public async Task GetAll_FilteredByGrade_OnlyReturnsMatches()
    {
        await SignInAsync();
        await CreateAsync(SampleCourse("CS201", "A"));
        await CreateAsync(SampleCourse("CS202", "B"));

        var courses = await _client.GetFromJsonAsync<List<CourseResponseDto>>("api/courses?grade=a");

        Assert.Single(courses!);
        Assert.Equal("CS201", courses![0].CourseCode);
    }

    // ---- update ----

    [Fact]
    public async Task Update_ChangesGradeAndRecalculatesPoints()
    {
        await SignInAsync();
        var created = await CreateAsync(SampleCourse(grade: "A"));

        var response = await _client.PutAsJsonAsync($"api/courses/{created.Id}", SampleCourse(grade: "B"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<CourseResponseDto>();
        Assert.Equal(3.0, updated!.GradePoints);
    }

    [Fact]
    public async Task Update_KeepingItsOwnCourseCode_Returns200()
    {
        await SignInAsync();
        var created = await CreateAsync(SampleCourse("CS201"));

        // same code on the same course isn't a collision
        var response = await _client.PutAsJsonAsync($"api/courses/{created.Id}", SampleCourse("CS201"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Update_TakingAnotherCoursesCode_Returns409()
    {
        await SignInAsync();
        await CreateAsync(SampleCourse("CS201"));
        var second = await CreateAsync(SampleCourse("CS202"));

        var response = await _client.PutAsJsonAsync($"api/courses/{second.Id}", SampleCourse("CS201"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Update_WhenMissing_Returns404()
    {
        await SignInAsync();

        var response = await _client.PutAsJsonAsync("api/courses/999", SampleCourse());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---- delete ----

    [Fact]
    public async Task Delete_ThenDeleteAgain_Returns204ThenNotFound()
    {
        await SignInAsync();
        var created = await CreateAsync(SampleCourse());

        var first = await _client.DeleteAsync($"api/courses/{created.Id}");
        var second = await _client.DeleteAsync($"api/courses/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, second.StatusCode);
    }
}
