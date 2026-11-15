using GritTrack.Models;
using GritTrack.Services;
using GritTrack.Services.Offline;

namespace GritTrack.Tests;

// the whole offline flow against a fake server: go offline, make changes,
// come back, sync. uses real files in a throwaway folder
public class OfflineCourseRepositoryTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "grittrack-offline-" + Guid.NewGuid().ToString("N"));
    private readonly FakeCourseRepository _server = new();
    private readonly List<(int From, int To)> _idMoves = new();

    public OfflineCourseRepositoryTests()
    {
        Directory.CreateDirectory(_folder);
    }

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    // a fresh repository on the same files = "the app was closed and opened again"
    private OfflineCourseRepository NewRepository() => new(
        _server,
        new JsonFile<List<PendingChange>>(Path.Combine(_folder, "pending.json")),
        new JsonFile<List<Course>>(Path.Combine(_folder, "cache.json")),
        new GpaCalculatorService().GetGradePoints,
        (from, to) =>
        {
            _idMoves.Add((from, to));
            return Task.CompletedTask;
        });

    private static Course NewCourse(string code, string grade = "A") => new()
    {
        Name = "Course " + code,
        CourseCode = code,
        Credits = 3,
        CurrentGrade = grade
    };

    [Fact]
    public async Task Online_SaveGoesStraightToTheServer()
    {
        var repository = NewRepository();

        var saved = await repository.SaveCourseAsync(NewCourse("CS101"));

        Assert.True(saved.ID > 0);
        Assert.False(saved.IsPendingSync);
        Assert.Equal(0, repository.PendingCount);
        Assert.Single(_server.Courses);
    }

    [Fact]
    public async Task Offline_SaveIsKeptOnThePhoneAndStillShowsInTheList()
    {
        var repository = NewRepository();
        _server.IsDown = true;

        var saved = await repository.SaveCourseAsync(NewCourse("CS101", grade: "B+"));
        var courses = await repository.GetCoursesAsync();

        Assert.True(saved.IsPendingSync);
        Assert.True(saved.ID < 0);
        Assert.Equal(3.3, saved.GradePoints); // worked out on the phone, no server to ask
        Assert.Equal(1, repository.PendingCount);
        Assert.True(repository.IsOffline);
        Assert.Equal("CS101", Assert.Single(courses).CourseCode);
        Assert.Empty(_server.Courses);
    }

    [Fact]
    public async Task Offline_ListComesFromTheSavedCopy()
    {
        var repository = NewRepository();
        await repository.SaveCourseAsync(NewCourse("CS101"));
        await repository.GetCoursesAsync(); // online load saves the copy

        _server.IsDown = true;
        var courses = await NewRepository().GetCoursesAsync();

        Assert.Equal("CS101", Assert.Single(courses).CourseCode);
    }

    [Fact]
    public async Task WaitingChanges_SurviveClosingTheApp()
    {
        _server.IsDown = true;
        await NewRepository().SaveCourseAsync(NewCourse("CS101"));

        var reopened = NewRepository();
        await reopened.GetCoursesAsync();

        Assert.Equal(1, reopened.PendingCount);
    }

    [Fact]
    public async Task Sync_SendsEverythingAndEmptiesTheList()
    {
        var repository = NewRepository();
        var existing = await repository.SaveCourseAsync(NewCourse("CS101"));

        _server.IsDown = true;
        var offlineCourse = await repository.SaveCourseAsync(NewCourse("CS202"));
        existing.Name = "Renamed";
        await repository.SaveCourseAsync(existing);

        _server.IsDown = false;
        var result = await repository.SyncAsync();

        Assert.Equal(new SyncResult(SentCount: 2, ProblemCount: 0, StillWaitingCount: 0, StillOffline: false), result);
        Assert.Equal(0, repository.PendingCount);
        Assert.Equal(2, _server.Courses.Count);
        Assert.Contains(_server.Courses, course => course.Name == "Renamed");

        // the course made offline got a real ID, and the app was told so it can move its assignments
        var realId = _server.Courses.Single(course => course.CourseCode == "CS202").ID;
        Assert.Equal((offlineCourse.ID, realId), Assert.Single(_idMoves));
    }

    [Fact]
    public async Task Sync_WhileStillOffline_KeepsEverythingWaiting()
    {
        var repository = NewRepository();
        _server.IsDown = true;
        await repository.SaveCourseAsync(NewCourse("CS101"));

        var result = await repository.SyncAsync();

        Assert.True(result.StillOffline);
        Assert.Equal(0, result.SentCount);
        Assert.Equal(1, repository.PendingCount);
    }

    [Fact]
    public async Task Sync_WhenTheServerSaysNo_KeepsThatChangeWithTheReason()
    {
        var repository = NewRepository();
        _server.IsDown = true;
        await repository.SaveCourseAsync(NewCourse("DUPE"));
        await repository.SaveCourseAsync(NewCourse("FINE"));

        _server.IsDown = false;
        _server.RejectedCourseCode = "DUPE";
        var result = await repository.SyncAsync();

        Assert.Equal(1, result.SentCount);
        Assert.Equal(1, result.ProblemCount);
        Assert.Contains("already in use", Assert.Single(repository.Problems));

        // the user can throw the bad one away
        Assert.Equal(1, await repository.DiscardProblemsAsync());
        Assert.Equal(0, repository.PendingCount);
    }

    [Fact]
    public async Task Sync_RunTwice_NeverSendsTheSameChangeTwice()
    {
        var repository = NewRepository();
        _server.IsDown = true;
        await repository.SaveCourseAsync(NewCourse("CS101"));
        _server.IsDown = false;

        await repository.SyncAsync();
        await repository.SyncAsync();

        Assert.Equal(new[] { "create CS101" }, _server.Calls);
    }

    [Fact]
    public async Task Offline_DeleteIsQueuedAndTheCourseLeavesTheList()
    {
        var repository = NewRepository();
        var saved = await repository.SaveCourseAsync(NewCourse("CS101"));
        await repository.GetCoursesAsync();

        _server.IsDown = true;
        await repository.DeleteCourseAsync(saved.ID);

        Assert.Empty(await repository.GetCoursesAsync());
        Assert.Single(_server.Courses); // still on the server until we sync

        _server.IsDown = false;
        await repository.SyncAsync();
        Assert.Empty(_server.Courses);
    }

    [Fact]
    public async Task Sync_WhenTheCourseIsAlreadyGoneOnTheServer_DropsTheChange()
    {
        var repository = NewRepository();
        var saved = await repository.SaveCourseAsync(NewCourse("CS101"));

        _server.IsDown = true;
        saved.Name = "Edited offline";
        await repository.SaveCourseAsync(saved);

        _server.IsDown = false;
        _server.Courses.Clear(); // someone else deleted it meanwhile
        var result = await repository.SyncAsync();

        Assert.Equal(0, result.ProblemCount);
        Assert.Equal(0, repository.PendingCount);
    }
}
