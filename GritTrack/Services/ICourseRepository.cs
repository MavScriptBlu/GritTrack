using GritTrack.Models;

namespace GritTrack.Services
{
    /// <summary>
    /// Everything the app needs to read and write course data, with no
    /// opinion about where that data actually lives. Today it's in-memory
    /// sample data (see InMemoryCourseRepository); swapping in a real REST
    /// API or a local SQLite database later just means writing a new class
    /// that implements this interface and changing one registration line in
    /// MauiProgram.cs. Nothing in the ViewModels has to change — they only
    /// ever talk to ICourseRepository, never to a concrete implementation.
    /// </summary>
    public interface ICourseRepository
    {
        Task<List<Course>> GetCoursesAsync();

        /// <summary>Adds a new course, or updates an existing one if the ID already exists.</summary>
        Task SaveCourseAsync(Course course);

        Task DeleteCourseAsync(int courseId);
    }
}
