using GritTrack.Models;
using GritTrack.Services.Offline;

namespace GritTrack.App.Tests;

// the waiting list rules, with no files and no network involved
public class PendingChangeRulesTests
{
    private static Course NewCourse(string code, int id = 0) => new() { ID = id, CourseCode = code, Name = code };

    [Fact]
    public void Create_GetsATemporaryNegativeId()
    {
        var queue = new List<PendingChange>();

        var first = PendingChangeRules.Add(queue, PendingChangeKind.Create, NewCourse("CS101"));
        var second = PendingChangeRules.Add(queue, PendingChangeKind.Create, NewCourse("CS102"));

        Assert.Equal(-1, first.ID);
        Assert.Equal(-2, second.ID);
    }

    [Fact]
    public void EditingACourseThatIsWaitingToBeCreated_KeepsOneCreate()
    {
        var queue = new List<PendingChange>();
        var created = PendingChangeRules.Add(queue, PendingChangeKind.Create, NewCourse("CS101"));

        PendingChangeRules.Add(queue, PendingChangeKind.Update, NewCourse("CS101-B", created.ID));

        var only = Assert.Single(queue);
        Assert.Equal(PendingChangeKind.Create, only.Kind);
        Assert.Equal("CS101-B", only.Course.CourseCode);
    }

    [Fact]
    public void EditingTwice_KeepsOnlyTheLastEdit()
    {
        var queue = new List<PendingChange>();

        PendingChangeRules.Add(queue, PendingChangeKind.Update, NewCourse("FIRST", 7));
        PendingChangeRules.Add(queue, PendingChangeKind.Update, NewCourse("SECOND", 7));

        Assert.Equal("SECOND", Assert.Single(queue).Course.CourseCode);
    }

    [Fact]
    public void DeletingACourseThatNeverReachedTheServer_EmptiesTheQueue()
    {
        var queue = new List<PendingChange>();
        var created = PendingChangeRules.Add(queue, PendingChangeKind.Create, NewCourse("CS101"));

        PendingChangeRules.Add(queue, PendingChangeKind.Delete, NewCourse("CS101", created.ID));

        Assert.Empty(queue);
    }

    [Fact]
    public void DeletingAfterAnEdit_ReplacesTheEditWithTheDelete()
    {
        var queue = new List<PendingChange>();
        PendingChangeRules.Add(queue, PendingChangeKind.Update, NewCourse("CS101", 7));

        PendingChangeRules.Add(queue, PendingChangeKind.Delete, NewCourse("CS101", 7));

        Assert.Equal(PendingChangeKind.Delete, Assert.Single(queue).Kind);
    }

    [Fact]
    public void ApplyTo_LaysTheWaitingChangesOverTheServerList()
    {
        var server = new List<Course> { NewCourse("KEEP", 1), NewCourse("OLD", 2), NewCourse("GONE", 3) };
        var queue = new List<PendingChange>();
        PendingChangeRules.Add(queue, PendingChangeKind.Update, NewCourse("EDITED", 2));
        PendingChangeRules.Add(queue, PendingChangeKind.Delete, NewCourse("GONE", 3));
        PendingChangeRules.Add(queue, PendingChangeKind.Create, NewCourse("NEW"));

        var shown = PendingChangeRules.ApplyTo(server, queue);

        Assert.Equal(new[] { "KEEP", "EDITED", "NEW" }, shown.Select(course => course.CourseCode));
        Assert.False(shown[0].IsPendingSync);
        Assert.True(shown[1].IsPendingSync);
        Assert.True(shown[2].IsPendingSync);
    }
}
