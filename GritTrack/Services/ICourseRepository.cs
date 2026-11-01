using GritTrack.Models;

namespace GritTrack.Services
{
    // reads and writes course data — doesn't care where that data actually
    // lives. today it's in-memory (see InMemoryCourseRepository); swapping
    // in real storage later just means a new class using this interface
    public interface ICourseRepository
    {
        Task<List<Course>> GetCoursesAsync();

        // adds a new course (ID 0), or updates one if the ID already exists.
        // gives back the course as stored, because the backend picks the real ID
        Task<Course> SaveCourseAsync(Course course);

        Task DeleteCourseAsync(int courseId);

        // assignments for one course. the API can't store these yet, so the
        // real repository keeps them on the phone (see LocalAssignmentStore)
        Task<List<Assignment>> GetAssignmentsLocalAsync(int courseId);

        // adds assignments to a course. stamps CourseID on each one. skips ones
        // the course already has (same title + due day), and says how many
        Task<AssignmentImportResult> SaveAssignmentsLocalAsync(int courseId, IEnumerable<Assignment> assignments);
    }
}
