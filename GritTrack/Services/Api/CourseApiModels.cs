using GritTrack.Models;

namespace GritTrack.Services.Api
{
    // what we send to POST/PUT /api/courses. only the fields the API accepts:
    // no id, no grade points (the server works those out)
    public class CourseRequest
    {
        public string Name { get; set; } = string.Empty;
        public string CourseCode { get; set; } = string.Empty;
        public string? Instructor { get; set; }
        public double Credits { get; set; }
        public string CurrentGrade { get; set; } = string.Empty;

        public static CourseRequest From(Course course) => new()
        {
            Name = course.Name,
            CourseCode = course.CourseCode,
            Instructor = string.IsNullOrWhiteSpace(course.Instructor) ? null : course.Instructor,
            Credits = course.Credits,
            CurrentGrade = course.CurrentGrade
        };
    }

    // what the API sends back for a course
    public class CourseResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string CourseCode { get; set; } = string.Empty;
        public string? Instructor { get; set; }
        public double Credits { get; set; }
        public string CurrentGrade { get; set; } = string.Empty;
        public double GradePoints { get; set; }

        public Course ToCourse() => new()
        {
            ID = Id,
            Name = Name,
            CourseCode = CourseCode,
            Instructor = Instructor ?? string.Empty,
            Credits = Credits,
            CurrentGrade = CurrentGrade,
            GradePoints = GradePoints
        };
    }

    // POST /api/auth/login, both directions
    public class LoginRequest
    {
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAtUtc { get; set; }
    }
}
