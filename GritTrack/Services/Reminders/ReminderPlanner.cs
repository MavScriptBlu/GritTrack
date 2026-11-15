using GritTrack.Models;

namespace GritTrack.Services.Reminders
{
    // decides WHEN a reminder goes off and what it says. no phone stuff in
    // here, so it's easy to test
    public static class ReminderPlanner
    {
        // reminders go off at 9 in the morning
        public const int ReminderHour = 9;

        // 9 AM the day before it's due. if that already passed, 9 AM on the
        // due day. if that passed too (or it's done), there's nothing to remind about
        public static DateTime? GetReminderTime(Assignment assignment, DateTime now)
        {
            if (assignment.IsCompleted)
                return null;

            var dayBefore = assignment.DueDate.Date.AddDays(-1).AddHours(ReminderHour);
            if (dayBefore > now)
                return dayBefore;

            var dueDay = assignment.DueDate.Date.AddHours(ReminderHour);
            return dueDay > now ? dueDay : null;
        }

        public static string GetTitle(Assignment assignment, DateTime remindAt)
            => remindAt.Date == assignment.DueDate.Date
                ? $"Due today: {assignment.Title}"
                : $"Due tomorrow: {assignment.Title}";

        public static string GetMessage(Assignment assignment, string courseCode)
        {
            var course = string.IsNullOrWhiteSpace(courseCode) ? "Your assignment" : courseCode;
            return $"{course} is due {assignment.DueDate:ddd, MMM d}. You got this.";
        }
    }
}
