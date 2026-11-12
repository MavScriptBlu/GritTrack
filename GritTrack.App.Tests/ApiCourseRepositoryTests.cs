using System.Net;
using System.Text.Json;
using GritTrack.App.Tests.Support;
using GritTrack.Models;
using GritTrack.Services;
using GritTrack.Services.Api;

namespace GritTrack.App.Tests;

// the app side of the courses API: what it sends, how it reads the answer,
// and how it glues the phone-only assignments back onto each course
public class ApiCourseRepositoryTests : PhoneStaticsTestBase
{
    private const string TwoCoursesJson = """
        [
          {"id":1,"name":"Programming Fundamentals","courseCode":"CS-101","instructor":"Dr. Rivera","credits":3,"currentGrade":"A-","gradePoints":3.7,"createdAt":"2026-10-01T12:00:00Z"},
          {"id":2,"name":"Databases","courseCode":"CS-210","instructor":null,"credits":4,"currentGrade":"B+","gradePoints":3.3,"createdAt":"2026-10-02T12:00:00Z"}
        ]
        """;

    private readonly LocalAssignmentStore _assignmentStore = new();

    private ApiCourseRepository CreateRepository(StubHttpMessageHandler handler) => new(TestApi.CreateClient(handler), _assignmentStore);

    private static Assignment NewAssignment(string title) => new() { Title = title, DueDate = new DateTime(2026, 11, 10), PointsPossible = 25 };

    // ---- reading courses ----

    [Fact]
    public async Task GetCoursesAsync_AsksForTheCourseList()
    {
        var handler = StubHttpMessageHandler.Always(HttpStatusCode.OK, "[]");

        await CreateRepository(handler).GetCoursesAsync();

        var sent = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, sent.Method);
        Assert.Equal("/api/courses", sent.Path);
    }

    [Fact]
    public async Task GetCoursesAsync_ReadsEveryField()
    {
        var repository = CreateRepository(StubHttpMessageHandler.Always(HttpStatusCode.OK, TwoCoursesJson));

        var courses = await repository.GetCoursesAsync();

        Assert.Equal(2, courses.Count);
        var first = courses[0];
        Assert.Equal(1, first.ID);
        Assert.Equal("Programming Fundamentals", first.Name);
        Assert.Equal("CS-101", first.CourseCode);
        Assert.Equal("Dr. Rivera", first.Instructor);
        Assert.Equal(3, first.Credits);
        Assert.Equal("A-", first.CurrentGrade);
        Assert.Equal(3.7, first.GradePoints);
    }

    [Fact]
    public async Task GetCoursesAsync_ANullInstructor_BecomesAnEmptyString()
    {
        var repository = CreateRepository(StubHttpMessageHandler.Always(HttpStatusCode.OK, TwoCoursesJson));

        var courses = await repository.GetCoursesAsync();

        Assert.Equal(string.Empty, courses[1].Instructor);
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("null")]
    public async Task GetCoursesAsync_NothingThere_IsAnEmptyList(string json)
    {
        var repository = CreateRepository(StubHttpMessageHandler.Always(HttpStatusCode.OK, json));

        Assert.Empty(await repository.GetCoursesAsync());
    }

    [Fact]
    public async Task GetCoursesAsync_AttachesTheAssignmentsSavedOnThePhone()
    {
        await _assignmentStore.AddAsync(1, new[] { NewAssignment("Lab 4"), NewAssignment("Lab 5") });
        var repository = CreateRepository(StubHttpMessageHandler.Always(HttpStatusCode.OK, TwoCoursesJson));

        var courses = await repository.GetCoursesAsync();

        Assert.Equal(new[] { "Lab 4", "Lab 5" }, courses[0].Assignments.Select(assignment => assignment.Title));
        Assert.Empty(courses[1].Assignments);
    }

    [Fact]
    public async Task GetCoursesAsync_ServerDown_IsAFriendlyError()
    {
        var repository = CreateRepository(StubHttpMessageHandler.Throwing(new HttpRequestException("refused")));

        var exception = await Assert.ThrowsAsync<ApiRequestException>(() => repository.GetCoursesAsync());

        Assert.Contains("Couldn't reach the GritTrack server", exception.Message);
    }

    [Fact]
    public async Task GetCoursesAsync_ServerError_IsAFriendlyError()
    {
        var repository = CreateRepository(StubHttpMessageHandler.Always(HttpStatusCode.InternalServerError));

        var exception = await Assert.ThrowsAsync<ApiRequestException>(() => repository.GetCoursesAsync());

        Assert.Contains("error 500", exception.Message);
    }

    // ---- saving a course ----

    [Fact]
    public async Task SaveCourseAsync_NewCourse_PostsToTheCourseList()
    {
        var handler = StubHttpMessageHandler.Always(HttpStatusCode.Created,
            """{"id":9,"name":"Web Dev","courseCode":"CS-150","instructor":"Staff","credits":3,"currentGrade":"A","gradePoints":4.0}""");
        var repository = CreateRepository(handler);

        var saved = await repository.SaveCourseAsync(new Course { ID = 0, Name = "Web Dev", CourseCode = "CS-150", Instructor = "Staff", Credits = 3, CurrentGrade = "A" });

        var sent = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, sent.Method);
        Assert.Equal("/api/courses", sent.Path);
        // the server picks the real id and works out the grade points
        Assert.Equal(9, saved.ID);
        Assert.Equal(4.0, saved.GradePoints);
    }

    [Fact]
    public async Task SaveCourseAsync_ExistingCourse_PutsToThatCoursesAddress()
    {
        var handler = StubHttpMessageHandler.Always(HttpStatusCode.OK,
            """{"id":5,"name":"Renamed","courseCode":"CS-101","instructor":"Staff","credits":3,"currentGrade":"B","gradePoints":3.0}""");
        var repository = CreateRepository(handler);

        await repository.SaveCourseAsync(new Course { ID = 5, Name = "Renamed", CourseCode = "CS-101", Credits = 3, CurrentGrade = "B" });

        var sent = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Put, sent.Method);
        Assert.Equal("/api/courses/5", sent.Path);
    }

    [Fact]
    public async Task SaveCourseAsync_SendsOnlyTheFieldsTheApiAccepts_InCamelCase()
    {
        var handler = StubHttpMessageHandler.Always(HttpStatusCode.Created, """{"id":9,"name":"Web Dev","courseCode":"CS-150","credits":3,"currentGrade":"A","gradePoints":4.0}""");
        var repository = CreateRepository(handler);

        await repository.SaveCourseAsync(new Course
        {
            ID = 0, Name = "Web Dev", CourseCode = "CS-150", Instructor = "Staff", Credits = 3, CurrentGrade = "A", GradePoints = 4.0,
            Assignments = { NewAssignment("should not be sent") }
        });

        using var body = JsonDocument.Parse(Assert.Single(handler.Requests).Body);
        var propertyNames = body.RootElement.EnumerateObject().Select(property => property.Name).Order().ToList();
        Assert.Equal(new[] { "courseCode", "credits", "currentGrade", "instructor", "name" }, propertyNames);
        Assert.DoesNotContain("should not be sent", Assert.Single(handler.Requests).Body);
    }

    [Fact]
    public async Task SaveCourseAsync_ABlankInstructor_IsSentAsNull()
    {
        var handler = StubHttpMessageHandler.Always(HttpStatusCode.Created, """{"id":9,"name":"Web Dev","courseCode":"CS-150","credits":3,"currentGrade":"A","gradePoints":4.0}""");
        var repository = CreateRepository(handler);

        await repository.SaveCourseAsync(new Course { Name = "Web Dev", CourseCode = "CS-150", Instructor = "   ", Credits = 3, CurrentGrade = "A" });

        using var body = JsonDocument.Parse(Assert.Single(handler.Requests).Body);
        Assert.Equal(JsonValueKind.Null, body.RootElement.GetProperty("instructor").ValueKind);
    }

    [Fact]
    public async Task SaveCourseAsync_GivesTheSavedCourseItsPhoneAssignmentsBack()
    {
        await _assignmentStore.AddAsync(5, new[] { NewAssignment("Lab 4") });
        var repository = CreateRepository(StubHttpMessageHandler.Always(HttpStatusCode.OK,
            """{"id":5,"name":"Renamed","courseCode":"CS-101","credits":3,"currentGrade":"B","gradePoints":3.0}"""));

        var saved = await repository.SaveCourseAsync(new Course { ID = 5, Name = "Renamed", CourseCode = "CS-101", Credits = 3, CurrentGrade = "B" });

        Assert.Equal("Lab 4", Assert.Single(saved.Assignments).Title);
    }

    [Fact]
    public async Task SaveCourseAsync_AnAnswerWithNoCourse_IsAFriendlyError()
    {
        var repository = CreateRepository(StubHttpMessageHandler.Always(HttpStatusCode.OK, "null"));

        var exception = await Assert.ThrowsAsync<ApiRequestException>(() =>
            repository.SaveCourseAsync(new Course { Name = "X", CourseCode = "X-1", CurrentGrade = "A" }));

        Assert.Contains("not with the saved course", exception.Message);
    }

    [Fact]
    public async Task SaveCourseAsync_DuplicateCode_Shows409Detail()
    {
        var repository = CreateRepository(StubHttpMessageHandler.Always(HttpStatusCode.Conflict,
            """{"detail":"A course with code 'CS-101' already exists."}"""));

        var exception = await Assert.ThrowsAsync<ApiValidationException>(() =>
            repository.SaveCourseAsync(new Course { Name = "X", CourseCode = "CS-101", CurrentGrade = "A" }));

        Assert.Equal("A course with code 'CS-101' already exists.", exception.Message);
    }

    [Fact]
    public async Task SaveCourseAsync_NotLoggedIn_ThrowsSoRunWriteAsyncCanOpenTheLoginPage()
    {
        var repository = CreateRepository(StubHttpMessageHandler.Always(HttpStatusCode.Unauthorized));

        await Assert.ThrowsAsync<AuthenticationRequiredException>(() =>
            repository.SaveCourseAsync(new Course { Name = "X", CourseCode = "X-1", CurrentGrade = "A" }));
    }

    // ---- deleting ----

    [Fact]
    public async Task DeleteCourseAsync_SendsADeleteForThatCourse()
    {
        var handler = StubHttpMessageHandler.Always(HttpStatusCode.NoContent);

        await CreateRepository(handler).DeleteCourseAsync(5);

        var sent = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Delete, sent.Method);
        Assert.Equal("/api/courses/5", sent.Path);
    }

    [Fact]
    public async Task DeleteCourseAsync_AlsoClearsThatCoursesPhoneAssignments()
    {
        await _assignmentStore.AddAsync(5, new[] { NewAssignment("Lab 4") });
        await _assignmentStore.AddAsync(6, new[] { NewAssignment("Other course") });

        await CreateRepository(StubHttpMessageHandler.Always(HttpStatusCode.NoContent)).DeleteCourseAsync(5);

        Assert.Empty(await _assignmentStore.GetAsync(5));
        Assert.Single(await _assignmentStore.GetAsync(6));
    }

    [Fact]
    public async Task DeleteCourseAsync_WhenTheServerRefuses_KeepsThePhoneAssignments()
    {
        await _assignmentStore.AddAsync(5, new[] { NewAssignment("Lab 4") });
        var repository = CreateRepository(StubHttpMessageHandler.Always(HttpStatusCode.NotFound));

        var exception = await Assert.ThrowsAsync<ApiRequestException>(() => repository.DeleteCourseAsync(5));

        Assert.Contains("no longer exists", exception.Message);
        Assert.Single(await _assignmentStore.GetAsync(5));
    }

    [Fact]
    public async Task DeleteCourseAsync_NotLoggedIn_Throws401()
    {
        var repository = CreateRepository(StubHttpMessageHandler.Always(HttpStatusCode.Unauthorized));

        await Assert.ThrowsAsync<AuthenticationRequiredException>(() => repository.DeleteCourseAsync(5));
    }

    // ---- assignments (phone only, for now) ----

    [Fact]
    public async Task SaveAssignmentsLocalAsync_StampsTheCourseId_AndNeverCallsTheApi()
    {
        var handler = StubHttpMessageHandler.Always(HttpStatusCode.OK);
        var repository = CreateRepository(handler);
        var assignment = NewAssignment("Lab 4");

        var result = await repository.SaveAssignmentsLocalAsync(5, new[] { assignment });

        Assert.Equal(1, result.AddedCount);
        Assert.Equal(5, assignment.CourseID);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task SaveAssignmentsLocalAsync_SkipsDuplicates_AndGetAssignmentsLocalAsyncSeesTheRest()
    {
        var repository = CreateRepository(StubHttpMessageHandler.Always(HttpStatusCode.OK));
        await repository.SaveAssignmentsLocalAsync(5, new[] { NewAssignment("Lab 4") });

        var result = await repository.SaveAssignmentsLocalAsync(5, new[] { NewAssignment("Lab 4"), NewAssignment("Lab 5") });
        var stored = await repository.GetAssignmentsLocalAsync(5);

        Assert.Equal(1, result.AddedCount);
        Assert.Equal(1, result.SkippedCount);
        Assert.Equal(new[] { "Lab 4", "Lab 5" }, stored.Select(assignment => assignment.Title));
    }
}
