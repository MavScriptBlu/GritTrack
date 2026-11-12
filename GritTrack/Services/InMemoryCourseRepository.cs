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

        public Task<Course> SaveCourseAsync(Course course)
        {
            // a brand new course (ID 0) gets the next free ID, like a database would
            if (course.ID == 0)
                course.ID = _courses.Count == 0 ? 1 : _courses.Max(c => c.ID) + 1;

            var index = _courses.FindIndex(c => c.ID == course.ID);
            if (index >= 0)
                _courses[index] = course;
            else
                _courses.Add(course);

            return Task.FromResult(course);
        }

        public Task DeleteCourseAsync(int courseId)
        {
            _courses.RemoveAll(c => c.ID == courseId);
            return Task.CompletedTask;
        }

        public Task<List<Assignment>> GetAssignmentsLocalAsync(int courseId)
        {
            var course = _courses.FirstOrDefault(candidate => candidate.ID == courseId);
            return Task.FromResult(course is null ? new List<Assignment>() : new List<Assignment>(course.Assignments));
        }

        public Task<AssignmentImportResult> SaveAssignmentsLocalAsync(int courseId, IEnumerable<Assignment> assignments)
        {
            var assignmentList = assignments.ToList();
            var course = _courses.FirstOrDefault(candidate => candidate.ID == courseId);
            if (course is null)
                return Task.FromResult(new AssignmentImportResult(0, assignmentList.Count, new List<Assignment>()));

            foreach (var assignment in assignmentList)
                assignment.CourseID = courseId;

            // same duplicate rule as the real store: same title + due day is skipped
            var result = AssignmentDuplicateRule.Filter(course.Assignments, assignmentList);
            course.Assignments.AddRange(result.AddedAssignments);

            return Task.FromResult(result);
        }
    }
}
