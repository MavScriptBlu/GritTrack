using System.Text.Json.Serialization;

namespace GritTrack.Models
{
    public enum AssignmentType
    {
        Homework,
        Quiz,
        Test,
        Project,
        Reading,
        Other
    }

    public class Assignment
    {
        public int ID { get; set; }

        [JsonIgnore]
        public int CourseID { get; set; }

        public string Title { get; set; } = string.Empty;
        public AssignmentType Type { get; set; }
        public DateTime DueDate { get; set; }
        public double PointsPossible { get; set; }
        public double? PointsEarned { get; set; }
        public bool IsCompleted { get; set; }
        public double EstimatedHours { get; set; } = 1.0;
        public string Notes { get; set; } = string.Empty;

        // file name of the attached photo (in the app's assignment-photos
        // folder), or null when there isn't one
        public string? PhotoFileName { get; set; }

        // true when a due date reminder is set for this assignment
        public bool ReminderOn { get; set; }

        [JsonIgnore]
        public bool HasPhoto => !string.IsNullOrEmpty(PhotoFileName);

        [JsonIgnore]
        public bool IsOverdue => !IsCompleted && DueDate.Date < DateTime.Today;

        [JsonIgnore]
        public bool IsDueSoon => !IsCompleted && !IsOverdue
            && (DueDate.Date - DateTime.Today).TotalDays <= 3;

        public override string ToString() => $"{Title} ({Type}) due {DueDate:MM/dd}";
    }
}
