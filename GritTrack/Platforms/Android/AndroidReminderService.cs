using GritTrack.Models;
using GritTrack.Services.Reminders;
using Plugin.LocalNotification;
using Plugin.LocalNotification.Core.Models;

namespace GritTrack.Platforms.Android
{
    // real phone notifications, through the Plugin.LocalNotification package.
    // ReminderPlanner decides when and what, this class only talks to Android
    public class AndroidReminderService : IReminderService
    {
        public async Task<ReminderResult> ScheduleAsync(Assignment assignment, string courseCode)
        {
            var remindAt = ReminderPlanner.GetReminderTime(assignment, DateTime.Now);
            if (remindAt is null)
                return new ReminderResult(ReminderStatus.TooLate, "This one is already due, so there's nothing left to remind you about.");

            try
            {
                // Android 13+ asks the user before an app can show notifications
                var allowed = await LocalNotificationCenter.Current.AreNotificationsEnabled()
                    || await LocalNotificationCenter.Current.RequestNotificationPermission();

                if (!allowed)
                    return new ReminderResult(ReminderStatus.PermissionDenied, "Notifications are off for GritTrack. Turn them on in your phone's Settings to get reminders.");

                await LocalNotificationCenter.Current.Show(new NotificationRequest
                {
                    // the assignment's own ID, so setting it again replaces the old one
                    NotificationId = assignment.ID,
                    Title = ReminderPlanner.GetTitle(assignment, remindAt.Value),
                    Description = ReminderPlanner.GetMessage(assignment, courseCode),
                    Schedule = new NotificationRequestSchedule { NotifyTime = remindAt.Value }
                });

                return new ReminderResult(ReminderStatus.Scheduled, $"Reminder set for {remindAt.Value:ddd, MMM d} at 9 AM.");
            }
            catch (Exception)
            {
                return new ReminderResult(ReminderStatus.Failed, "Couldn't set that reminder. Try again.");
            }
        }

        public void Cancel(Assignment assignment)
        {
            try
            {
                LocalNotificationCenter.Current.Cancel(assignment.ID);
            }
            catch (Exception)
            {
                // nothing was scheduled, or the phone said no. either way there's no reminder now
            }
        }
    }
}
