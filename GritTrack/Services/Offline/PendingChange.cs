using GritTrack.Models;

namespace GritTrack.Services.Offline
{
    public enum PendingChangeKind
    {
        Create,
        Update,
        Delete
    }

    // one course change that couldn't reach the server yet. it sits in a
    // file on the phone until Sync sends it
    public class PendingChange
    {
        public PendingChangeKind Kind { get; set; }

        // the course as it should look after the change. a course that was
        // created offline has a made-up negative ID until the server gives it a real one
        public Course Course { get; set; } = new();

        // filled in when a sync tried this change and the server said no
        // (like "that course code is already in use")
        public string? Problem { get; set; }
    }
}
