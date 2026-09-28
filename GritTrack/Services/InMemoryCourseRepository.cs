using GritTrack.Models;

namespace GritTrack.Services
{
    // stand-in for ICourseRepository — just a list in memory, not a real
    // backend. registered as a Singleton so it survives navigation, but
    // it resets every time the app fully restarts. swap it for a real
    // database later by writing a new class that uses ICourseRepository
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
            // returning a copy so nothing outside this class can mess with
            // the real list by accident
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
