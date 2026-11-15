using GritTrack.Models;
using GritTrack.Services.Reminders;

namespace GritTrack.Tests;

public class ReminderPlannerTests
{
    private static readonly DateTime Now = new(2026, 11, 10, 14, 0, 0); // a Tuesday, 2 PM

    private static Assignment DueOn(DateTime dueDate) => new() { Title = "Lab 4", DueDate = dueDate };

    [Fact]
    public void DueLaterThisWeek_RemindsAtNineTheDayBefore()
    {
        var remindAt = ReminderPlanner.GetReminderTime(DueOn(new DateTime(2026, 11, 13)), Now);

        Assert.Equal(new DateTime(2026, 11, 12, 9, 0, 0), remindAt);
    }

    [Fact]
    public void DueTomorrow_DayBeforeAlreadyPassed_RemindsOnTheDueDay()
    {
        var remindAt = ReminderPlanner.GetReminderTime(DueOn(new DateTime(2026, 11, 11)), Now);

        Assert.Equal(new DateTime(2026, 11, 11, 9, 0, 0), remindAt);
    }

    [Fact]
    public void DueTodayAfterNine_IsTooLate()
    {
        Assert.Null(ReminderPlanner.GetReminderTime(DueOn(Now.Date), Now));
    }

    [Fact]
    public void Overdue_IsTooLate()
    {
        Assert.Null(ReminderPlanner.GetReminderTime(DueOn(Now.AddDays(-3)), Now));
    }

    [Fact]
    public void AlreadyDone_NeverReminds()
    {
        var assignment = DueOn(Now.AddDays(5));
        assignment.IsCompleted = true;

        Assert.Null(ReminderPlanner.GetReminderTime(assignment, Now));
    }

    [Fact]
    public void Title_SaysTomorrowOrToday()
    {
        var assignment = DueOn(new DateTime(2026, 11, 13));

        Assert.Equal("Due tomorrow: Lab 4", ReminderPlanner.GetTitle(assignment, new DateTime(2026, 11, 12, 9, 0, 0)));
        Assert.Equal("Due today: Lab 4", ReminderPlanner.GetTitle(assignment, new DateTime(2026, 11, 13, 9, 0, 0)));
    }

    [Fact]
    public void Message_NamesTheCourse()
    {
        var message = ReminderPlanner.GetMessage(DueOn(new DateTime(2026, 11, 13)), "CS101");

        Assert.Equal("CS101 is due Fri, Nov 13. You got this.", message);
    }
}
