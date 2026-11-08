using GritTrack.App.Tests.Support;
using GritTrack.Models;
using GritTrack.Services;

namespace GritTrack.App.Tests;

// the API can't store assignments yet, so the phone keeps them in local-assignments.json.
// these tests are about that file: do imports survive a restart, do duplicates stay out,
// and does a broken file ever take the app down
public class LocalAssignmentStoreTests : PhoneStaticsTestBase
{
    private static Assignment NewAssignment(string title, int dayOfMonth = 10, double points = 50) => new()
    {
        ID = Random.Shared.Next(1, 1_000_000),
        Title = title,
        DueDate = new DateTime(2026, 11, dayOfMonth, 9, 30, 0),
        PointsPossible = points,
        Type = AssignmentType.Homework,
        Notes = "from a test"
    };

    // ---- save and load ----

    [Fact]
    public async Task AddAsync_ThenGetAsync_GivesTheAssignmentsBack()
    {
        var store = new LocalAssignmentStore();

        var result = await store.AddAsync(7, new[] { NewAssignment("Lab 4"), NewAssignment("Midterm", 12, 100) });
        var loaded = await store.GetAsync(7);

        Assert.Equal(2, result.AddedCount);
        Assert.Equal(new[] { "Lab 4", "Midterm" }, loaded.Select(assignment => assignment.Title));
        Assert.Equal(100, loaded[1].PointsPossible);
        Assert.Equal(AssignmentType.Homework, loaded[0].Type);
    }

    [Fact]
    public async Task AddAsync_WritesAJsonFileInTheAppDataFolder()
    {
        var store = new LocalAssignmentStore();

        await store.AddAsync(7, new[] { NewAssignment("Lab 4") });

        Assert.True(File.Exists(AssignmentFilePath));
        Assert.Contains("Lab 4", await File.ReadAllTextAsync(AssignmentFilePath));
    }

    [Fact]
    public async Task Assignments_SurviveARestart()
    {
        await new LocalAssignmentStore().AddAsync(7, new[] { NewAssignment("Lab 4") });

        // a brand new store object, like the app starting again tomorrow
        var afterRestart = await new LocalAssignmentStore().GetAsync(7);

        Assert.Equal("Lab 4", Assert.Single(afterRestart).Title);
    }

    [Fact]
    public async Task GetAsync_PutsTheCourseIdBackOnEachAssignment()
    {
        // CourseID is left out of the JSON on purpose (the course already holds it), so the store has to restore it
        await new LocalAssignmentStore().AddAsync(7, new[] { NewAssignment("Lab 4") });

        var afterRestart = await new LocalAssignmentStore().GetAsync(7);

        Assert.Equal(7, Assert.Single(afterRestart).CourseID);
        Assert.DoesNotContain("CourseID", await File.ReadAllTextAsync(AssignmentFilePath));
    }

    [Fact]
    public async Task GetAsync_ForACourseWithNothingSaved_IsAnEmptyList()
    {
        var store = new LocalAssignmentStore();

        Assert.Empty(await store.GetAsync(99));
    }

    [Fact]
    public async Task Assignments_AreKeptSeparatePerCourse()
    {
        var store = new LocalAssignmentStore();
        await store.AddAsync(1, new[] { NewAssignment("Course one work") });
        await store.AddAsync(2, new[] { NewAssignment("Course two work") });

        Assert.Equal("Course one work", Assert.Single(await store.GetAsync(1)).Title);
        Assert.Equal("Course two work", Assert.Single(await store.GetAsync(2)).Title);
    }

    [Fact]
    public async Task GetAsync_HandsBackACopyOfTheList_SoCallersCantScrambleTheStore()
    {
        var store = new LocalAssignmentStore();
        await store.AddAsync(7, new[] { NewAssignment("Lab 4") });

        var firstLook = await store.GetAsync(7);
        firstLook.Clear();

        Assert.Single(await store.GetAsync(7));
    }

    [Fact]
    public async Task AddAsync_KeepsEarlierAssignments_WhenMoreAreAddedLater()
    {
        var store = new LocalAssignmentStore();
        await store.AddAsync(7, new[] { NewAssignment("Lab 4") });

        await store.AddAsync(7, new[] { NewAssignment("Lab 5", 17) });

        Assert.Equal(new[] { "Lab 4", "Lab 5" }, (await new LocalAssignmentStore().GetAsync(7)).Select(assignment => assignment.Title));
    }

    // ---- duplicates ----

    [Fact]
    public async Task AddAsync_SkipsAnAssignmentThatIsAlreadyThere()
    {
        var store = new LocalAssignmentStore();
        await store.AddAsync(7, new[] { NewAssignment("Lab 4") });

        var result = await store.AddAsync(7, new[] { NewAssignment("Lab 4"), NewAssignment("Lab 5", 17) });

        Assert.Equal(1, result.AddedCount);
        Assert.Equal(1, result.SkippedCount);
        Assert.Equal(2, (await store.GetAsync(7)).Count);
    }

    [Fact]
    public async Task AddAsync_SameTitleOnADifferentDay_IsANewAssignment()
    {
        var store = new LocalAssignmentStore();
        await store.AddAsync(7, new[] { NewAssignment("Weekly quiz", 10) });

        var result = await store.AddAsync(7, new[] { NewAssignment("Weekly quiz", 17) });

        Assert.Equal(1, result.AddedCount);
        Assert.Equal(0, result.SkippedCount);
    }

    [Fact]
    public async Task AddAsync_SameTitleInAnotherCourse_IsNotADuplicate()
    {
        var store = new LocalAssignmentStore();
        await store.AddAsync(1, new[] { NewAssignment("Final project") });

        var result = await store.AddAsync(2, new[] { NewAssignment("Final project") });

        Assert.Equal(1, result.AddedCount);
    }

    [Fact]
    public async Task AddAsync_DuplicatesInsideOnePasteAreSkippedToo()
    {
        var store = new LocalAssignmentStore();

        var result = await store.AddAsync(7, new[] { NewAssignment("Lab 4"), NewAssignment("Lab 4"), NewAssignment("Lab 4") });

        Assert.Equal(1, result.AddedCount);
        Assert.Equal(2, result.SkippedCount);
    }

    [Fact]
    public async Task AddAsync_AllDuplicates_DoesNotRewriteTheFile()
    {
        var store = new LocalAssignmentStore();
        await store.AddAsync(7, new[] { NewAssignment("Lab 4") });
        var writtenAt = File.GetLastWriteTimeUtc(AssignmentFilePath);
        File.SetLastWriteTimeUtc(AssignmentFilePath, writtenAt.AddHours(-1));
        var marker = File.GetLastWriteTimeUtc(AssignmentFilePath);

        await store.AddAsync(7, new[] { NewAssignment("Lab 4") });

        Assert.Equal(marker, File.GetLastWriteTimeUtc(AssignmentFilePath));
    }

    [Fact]
    public async Task AddAsync_NothingToAdd_NeverCreatesTheFile()
    {
        var store = new LocalAssignmentStore();

        var result = await store.AddAsync(7, new[] { NewAssignment("   ") });

        Assert.Equal(0, result.AddedCount);
        Assert.Equal(1, result.SkippedCount);
        Assert.False(File.Exists(AssignmentFilePath));
    }

    // ---- deleting a course ----

    [Fact]
    public async Task RemoveCourseAsync_ForgetsThatCoursesAssignments_AndKeepsTheRest()
    {
        var store = new LocalAssignmentStore();
        await store.AddAsync(1, new[] { NewAssignment("Keep me") });
        await store.AddAsync(2, new[] { NewAssignment("Delete me") });

        await store.RemoveCourseAsync(2);

        Assert.Empty(await new LocalAssignmentStore().GetAsync(2));
        Assert.Equal("Keep me", Assert.Single(await new LocalAssignmentStore().GetAsync(1)).Title);
    }

    [Fact]
    public async Task RemoveCourseAsync_ForACourseWithNothingSaved_DoesNothing()
    {
        var store = new LocalAssignmentStore();

        await store.RemoveCourseAsync(99);

        Assert.False(File.Exists(AssignmentFilePath));
    }

    // ---- a broken or missing file ----

    [Fact]
    public async Task GetAsync_WithNoFileYet_IsEmpty_NotAnError()
    {
        Assert.False(File.Exists(AssignmentFilePath));

        Assert.Empty(await new LocalAssignmentStore().GetAsync(7));
    }

    [Theory]
    [InlineData("this is not json")]
    [InlineData("{\"1\": [ {\"title\": ")]
    [InlineData("")]
    public async Task GetAsync_WithAMessedUpFile_StartsEmptyInsteadOfCrashing(string fileContents)
    {
        await File.WriteAllTextAsync(AssignmentFilePath, fileContents);

        var exception = await Record.ExceptionAsync(async () => Assert.Empty(await new LocalAssignmentStore().GetAsync(7)));

        Assert.Null(exception);
    }

    [Fact]
    public async Task AddAsync_AfterAMessedUpFile_WritesAGoodFileAgain()
    {
        await File.WriteAllTextAsync(AssignmentFilePath, "garbage");
        var store = new LocalAssignmentStore();

        await store.AddAsync(7, new[] { NewAssignment("Lab 4") });

        Assert.Equal("Lab 4", Assert.Single(await new LocalAssignmentStore().GetAsync(7)).Title);
    }

    [Fact]
    public async Task ManySavesAtOnce_AllMakeItIntoTheFile()
    {
        // two imports that land at the same moment must not eat each other's work
        var store = new LocalAssignmentStore();

        await Task.WhenAll(Enumerable.Range(1, 20).Select(number =>
            store.AddAsync(7, new[] { NewAssignment($"Assignment {number}", 1 + number) })));

        Assert.Equal(20, (await new LocalAssignmentStore().GetAsync(7)).Count);
    }
}

// the duplicate rule on its own, with no file involved
public class AssignmentDuplicateRuleTests
{
    private static Assignment Make(string title, DateTime due) => new() { Title = title, DueDate = due };

    [Fact]
    public void KeyOf_IgnoresCapitalsAndSpaces_AndTheTimeOfDay()
    {
        var morning = AssignmentDuplicateRule.KeyOf(Make("  Lab 4  ", new DateTime(2026, 11, 5, 8, 0, 0)));
        var night = AssignmentDuplicateRule.KeyOf(Make("LAB 4", new DateTime(2026, 11, 5, 23, 59, 0)));

        Assert.Equal(morning, night);
    }

    [Fact]
    public void KeyOf_DifferentDays_AreDifferent()
    {
        var first = AssignmentDuplicateRule.KeyOf(Make("Lab 4", new DateTime(2026, 11, 5)));
        var second = AssignmentDuplicateRule.KeyOf(Make("Lab 4", new DateTime(2026, 11, 6)));

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Filter_KeepsOnlyTheNewOnes_AndCountsTheRest()
    {
        var existing = new[] { Make("Lab 4", new DateTime(2026, 11, 5)) };
        var incoming = new[]
        {
            Make("lab 4", new DateTime(2026, 11, 5, 17, 0, 0)),
            Make("Lab 5", new DateTime(2026, 11, 12)),
            Make("", new DateTime(2026, 11, 12))
        };

        var result = AssignmentDuplicateRule.Filter(existing, incoming);

        Assert.Equal(1, result.AddedCount);
        Assert.Equal(2, result.SkippedCount);
        Assert.Equal("Lab 5", Assert.Single(result.AddedAssignments).Title);
    }

    [Fact]
    public void Filter_WithNothingIncoming_AddsNothing()
    {
        var result = AssignmentDuplicateRule.Filter(Array.Empty<Assignment>(), Array.Empty<Assignment>());

        Assert.Equal(0, result.AddedCount);
        Assert.Equal(0, result.SkippedCount);
        Assert.Empty(result.AddedAssignments);
    }
}
