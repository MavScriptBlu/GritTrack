using GritTrack.Models;

namespace GritTrack.Services
{
    /// <summary>
    /// Stand-in ICourseRepository backed by a plain in-memory list instead
    /// of a real backend. Registered as a Singleton in MauiProgram.cs so the
    /// data survives navigation for the life of the app run, the same way a
    /// cached API client or local database connection would.
    ///
    /// When a real backend shows up (REST API, SQLite, whatever), this is
    /// the ONLY class that needs replacing — build a new
    /// ApiCourseRepository/SqliteCourseRepository implementing
    /// ICourseRepository, swap the DI registration in MauiProgram.cs, and
    /// every PageModel that depends on ICourseRepository keeps working
    /// without a single line changing.
    /// </summary>
    public class InMemoryCourseRepository : ICourseRepository
    {
        private readonly List<Course> _courses;

        public InMemoryCourseRepository()
        {
            _courses = new List<Course>
            {
                new Course { ID = 1, Name = "Programming Fundamentals", CourseCode = "CS-101", Instructor = "Staff", Credits = 3, CurrentGrade = "A-", GradePoints = 3.7 },
                new Course { ID = 2, Name = "Database Concepts and Design", CourseCode = "CS-210", Instructor = "Staff", Credits = 3, CurrentGrade = "B+", GradePoints = 3.3 },
                new Course { ID = 3, Name = "Web Development I", CourseCode = "CS-150", Instructor = "Staff", Credits = 3, CurrentGrade = "A", GradePoints = 4.0 }
            };
        }

        public Task<List<Course>> GetCoursesAsync()
        {
            // a real implementation would "await" an HttpClient call or a
            // database query here — the Task-based signature is already in
            // place so nothing calling this has to change when that happens.
            // Returning a copy so callers can't mutate our backing list by
            // accident through the reference.
            return Task.FromResult(new List<Course>(_courses));
        }

        public Task SaveCourseAsync(Course course)
        {
            var index = _courses.FindIndex(c => c.ID == course.ID);
            if (index >= 0)
                _courses[index] = course;
            else
                _courses.Add(course);

            return Task.CompletedTask;
        }

        public Task DeleteCourseAsync(int courseId)
        {
            _courses.RemoveAll(c => c.ID == courseId);
            return Task.CompletedTask;
        }
    }
}
