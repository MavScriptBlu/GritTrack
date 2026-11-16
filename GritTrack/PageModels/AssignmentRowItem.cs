using GritTrack.Models;

namespace GritTrack.PageModels
{
    // one assignment on the course page. wraps the plain Assignment so the
    // screen updates when its photo, reminder or done state changes
    public class AssignmentRowItem : BaseViewModel
    {
        private static readonly Color DoneColor = Color.FromArgb("#9fe0bd");
        private static readonly Color OverdueColor = Color.FromArgb("#f6adbf");
        private static readonly Color DueSoonColor = Color.FromArgb("#f2e3a6");

        private readonly Func<string, string> _photoPathFor;

        public AssignmentRowItem(Assignment assignment, Func<string, string> photoPathFor)
        {
            Assignment = assignment;
            _photoPathFor = photoPathFor;
        }

        public Assignment Assignment { get; }

        public string Title => Assignment.Title;

        public string DueText => $"Due {Assignment.DueDate:ddd, MMM d}";

        public string PointsText => $"{Assignment.PointsPossible:0.##} pts";

        public string StatusText =>
            Assignment.IsCompleted ? "Done"
            : Assignment.IsOverdue ? "Overdue"
            : Assignment.IsDueSoon ? "Due soon"
            : string.Empty;

        public bool HasStatus => StatusText.Length > 0;

        public Color StatusColor =>
            Assignment.IsCompleted ? DoneColor
            : Assignment.IsOverdue ? OverdueColor
            : DueSoonColor;

        public bool HasPhoto => Assignment.HasPhoto;

        public bool HasNoPhoto => !Assignment.HasPhoto;

        // full path on this device, or null when there's no photo
        public string? PhotoPath => Assignment.HasPhoto ? _photoPathFor(Assignment.PhotoFileName!) : null;

        public bool ReminderOn => Assignment.ReminderOn;

        public string ReminderButtonText => Assignment.ReminderOn ? "Reminder Is On (tap to turn off)" : "Remind Me Before It's Due";

        public string DoneButtonText => Assignment.IsCompleted ? "Mark Not Done" : "Mark Done";

        // call after changing the Assignment so every label catches up.
        // an empty name means "everything changed"
        public void Refresh() => OnPropertyChanged(string.Empty);
    }
}
