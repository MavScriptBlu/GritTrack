using GritTrack.Models;

namespace GritTrack.Services.Reminders
{
    // used on devices where the notification library doesn't work (Windows).
    // the button still answers, it just says why nothing was set
    public class NoReminderService : IReminderService
    {
        public Task<ReminderResult> ScheduleAsync(Assignment assignment, string courseCode)
            => Task.FromResult(new ReminderResult(ReminderStatus.NotAvailable, "Reminders only work on the phone app. This device can't show them."));

        public void Cancel(Assignment assignment)
        {
            // nothing to cancel: this device never schedules a reminder in the first place
        }
    }
}
