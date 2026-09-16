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

        [JsonIgnore]
        public bool IsOverdue => !IsCompleted && DueDate.Date < DateTime.Today;

        [JsonIgnore]
        public bool IsDueSoon => !IsCompleted && !IsOverdue
            && (DueDate.Date - DateTime.Today).TotalDays <= 3;

        public override string ToString() => $"{Title} ({Type}) due {DueDate:MM/dd}";
    }
}
