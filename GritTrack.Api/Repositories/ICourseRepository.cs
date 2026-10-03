using GritTrack.Api.Models;

namespace GritTrack.Api.Repositories;

// talks to wherever courses actually live. Part 1 this is a JSON file,
// Part 2 swaps in SQL Server without anything above this interface changing
public interface ICourseRepository
{
    Task<List<Course>> GetAllAsync(CancellationToken cancellationToken);
    Task<Course?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<Course?> GetByCourseCodeAsync(string courseCode, CancellationToken cancellationToken);
    Task<Course> AddAsync(Course course, CancellationToken cancellationToken);
    Task<bool> UpdateAsync(Course course, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
}
