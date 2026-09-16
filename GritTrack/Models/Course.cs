using System.Text.Json.Serialization;

namespace GritTrack.Models
{
    public class Course
    {
        public int ID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string CourseCode { get; set; } = string.Empty;
        public string Instructor { get; set; } = string.Empty;
        public double Credits { get; set; }
        public string CurrentGrade { get; set; } = string.Empty; // letter grade, e.g. "A-"
        public double GradePoints { get; set; }                  // e.g. 3.7
        public List<Assignment> Assignments { get; set; } = new();

        [JsonIgnore]
        public int CompletedCount => Assignments.Count(a => a.IsCompleted);

        [JsonIgnore]
        public int TotalCount => Assignments.Count;

        public override string ToString() => $"{CourseCode} - {Name}";
    }

    // Wrapper class matches the project's established JSON-persistence pattern
    public class CoursesJson
    {
        public List<Course> Courses { get; set; } = new();
    }
}
