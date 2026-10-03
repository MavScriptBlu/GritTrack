using GritTrack.Api.DTOs;
using GritTrack.Api.Models;

namespace GritTrack.Api.Services;

public interface ICourseService
{
    Task<List<Course>> GetCoursesAsync(string? grade, CancellationToken cancellationToken);
    Task<Course?> GetCourseByIdAsync(int id, CancellationToken cancellationToken);
    Task<(SaveCourseOutcome Outcome, Course? Course)> CreateCourseAsync(CourseRequestDto request, CancellationToken cancellationToken);
    Task<(SaveCourseOutcome Outcome, Course? Course)> UpdateCourseAsync(int id, CourseRequestDto request, CancellationToken cancellationToken);
    Task<bool> DeleteCourseAsync(int id, CancellationToken cancellationToken);
}
