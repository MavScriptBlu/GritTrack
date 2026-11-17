using GritTrack.Models;

namespace GritTrack.Services.Reminders
{
    public enum ReminderStatus
    {
        Scheduled,
        PermissionDenied, // notifications are turned off for the app
        NotAvailable,     // this kind of device can't do reminders
        TooLate,          // it's already due (or done)
        Failed
    }

    // what happened, plus a message that's safe to show on screen
    public record ReminderResult(ReminderStatus Status, string Message);

    // sets and cancels the "it's due soon" notification for an assignment
    public interface IReminderService
    {
        Task<ReminderResult> ScheduleAsync(Assignment assignment, string courseCode);

        void Cancel(Assignment assignment);
    }
}
