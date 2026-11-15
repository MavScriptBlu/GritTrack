namespace GritTrack.Services.Offline
{
    // what one Sync did
    public record SyncResult(int SentCount, int ProblemCount, int StillWaitingCount, bool StillOffline);

    // the offline side of the course repository. PageModels use this to show
    // the "waiting to sync" banner and to run Sync
    public interface ISyncService
    {
        // true when the last load couldn't reach the server and showed the saved copy
        bool IsOffline { get; }

        // how many changes are saved on the phone and not on the server yet
        int PendingCount { get; }

        // what the server said about changes it turned down, one line each
        IReadOnlyList<string> Problems { get; }

        // raised when any of the above changes
        event EventHandler? Changed;

        // sends the waiting changes, oldest first. safe to run again: whatever
        // already went through is off the list
        Task<SyncResult> SyncAsync();

        // throws away the changes the server turned down. gives back how many
        Task<int> DiscardProblemsAsync();
    }
}
