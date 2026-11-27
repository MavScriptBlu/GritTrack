using GritTrack.Services.Api;
using GritTrack.Services.Offline;
using GritTrack.Services.Photos;
using GritTrack.Services.Reminders;

namespace GritTrack.PageModels
{
    // ---- the extra services the course list needs ----
    // CourseListPageModel was up to 9 constructor parameters and the code
    // checker only likes 8 or less. these four are the "side" services
    // (login, offline sync, photos, reminders), so they ride together in
    // one bag. the page model still gets every one of them, just in one hand
    public sealed class CourseListPageModelServices
    {
        // DI fills this in, same as any other service
        public CourseListPageModelServices(IAuthService authService, ISyncService syncService, IPhotoService photoService, IReminderService reminderService)
        {
            AuthService = authService;
            SyncService = syncService;
            PhotoService = photoService;
            ReminderService = reminderService;
        }

        public IAuthService AuthService { get; }

        public ISyncService SyncService { get; }

        public IPhotoService PhotoService { get; }

        public IReminderService ReminderService { get; }
    }
}
