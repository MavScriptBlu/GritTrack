using GritTrack.Api.Models;

namespace GritTrack.Api.Repositories;

/// <summary>
/// Talks to wherever courses actually live. Part 1 this is a JSON file,
/// Part 2 swaps in SQL Server without anything above this interface changing.
/// </summary>
public interface ICourseRepository
{
    /// <summary>Every course on file.</summary>
    Task<List<Course>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>One course by id, or null if it doesn't exist.</summary>
    Task<Course?> GetByIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>One course by its code (case-insensitive), or null if nothing has that code yet.</summary>
    Task<Course?> GetByCourseCodeAsync(string courseCode, CancellationToken cancellationToken);

    /// <summary>Saves a brand new course and assigns it an id.</summary>
    Task<Course> AddAsync(Course course, CancellationToken cancellationToken);

    /// <summary>Overwrites an existing course. Returns false if that id doesn't exist.</summary>
    Task<bool> UpdateAsync(Course course, CancellationToken cancellationToken);

    /// <summary>Removes a course by id. Returns false if that id doesn't exist.</summary>
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
}
