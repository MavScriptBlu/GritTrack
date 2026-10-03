using GritTrack.Api.DTOs;
using GritTrack.Api.Models;

namespace GritTrack.Api.Services;

/// <summary>The business layer between the controller and the repository — owns the duplicate-course-code rule.</summary>
public interface ICourseService
{
    /// <summary>All courses, optionally filtered to one letter grade.</summary>
    Task<List<Course>> GetCoursesAsync(string? grade, CancellationToken cancellationToken);

    /// <summary>One course by id, or null if it doesn't exist.</summary>
    Task<Course?> GetCourseByIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>Adds a course. Fails with DuplicateCourseCode if the code's already taken.</summary>
    Task<(SaveCourseOutcome Outcome, Course? Course)> CreateCourseAsync(CourseRequestDto request, CancellationToken cancellationToken);

    /// <summary>Replaces a course's fields. Fails with NotFound or DuplicateCourseCode.</summary>
    Task<(SaveCourseOutcome Outcome, Course? Course)> UpdateCourseAsync(int id, CourseRequestDto request, CancellationToken cancellationToken);

    /// <summary>Removes a course. Returns false if that id doesn't exist.</summary>
    Task<bool> DeleteCourseAsync(int id, CancellationToken cancellationToken);
}
