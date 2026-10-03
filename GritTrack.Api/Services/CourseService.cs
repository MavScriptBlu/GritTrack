using GritTrack.Api.DTOs;
using GritTrack.Api.Models;
using GritTrack.Api.Repositories;

namespace GritTrack.Api.Services;

/// <inheritdoc cref="ICourseService" />
public class CourseService : ICourseService
{
    private readonly ICourseRepository _repository;

    // same grade-to-points table GritTrack's mobile app uses, so a course
    // is worth the same GPA points no matter which one touched it
    private static readonly Dictionary<string, double> GradeTable = new(StringComparer.OrdinalIgnoreCase)
    {
        { "A", 4.0 }, { "A-", 3.7 },
        { "B+", 3.3 }, { "B", 3.0 }, { "B-", 2.7 },
        { "C+", 2.3 }, { "C", 2.0 }, { "C-", 1.7 },
        { "D+", 1.3 }, { "D", 1.0 }, { "D-", 0.7 },
        { "F", 0.0 }
    };

    public CourseService(ICourseRepository repository)
    {
        _repository = repository;
    }

    /// <inheritdoc />
    public async Task<List<Course>> GetCoursesAsync(string? grade, CancellationToken cancellationToken)
    {
        var courses = await _repository.GetAllAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(grade))
        {
            courses = courses
                .Where(c => string.Equals(c.CurrentGrade, grade, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return courses;
    }

    /// <inheritdoc />
    public Task<Course?> GetCourseByIdAsync(int id, CancellationToken cancellationToken)
        => _repository.GetByIdAsync(id, cancellationToken);

    /// <inheritdoc />
    public async Task<(SaveCourseOutcome Outcome, Course? Course)> CreateCourseAsync(CourseRequestDto request, CancellationToken cancellationToken)
    {
        // the business rule: two courses can't share a course code
        var existing = await _repository.GetByCourseCodeAsync(request.CourseCode, cancellationToken);
        if (existing is not null)
            return (SaveCourseOutcome.DuplicateCourseCode, null);

        var course = new Course
        {
            Name = request.Name.Trim(),
            CourseCode = request.CourseCode.Trim(),
            Instructor = request.Instructor?.Trim(),
            Credits = request.Credits,
            CurrentGrade = request.CurrentGrade.Trim(),
            GradePoints = GetGradePoints(request.CurrentGrade),
            CreatedAt = DateTime.UtcNow
        };

        var saved = await _repository.AddAsync(course, cancellationToken);
        return (SaveCourseOutcome.Created, saved);
    }

    /// <inheritdoc />
    public async Task<(SaveCourseOutcome Outcome, Course? Course)> UpdateCourseAsync(int id, CourseRequestDto request, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetByIdAsync(id, cancellationToken);
        if (existing is null)
            return (SaveCourseOutcome.NotFound, null);

        // only a problem if some OTHER course already owns this code
        var codeOwner = await _repository.GetByCourseCodeAsync(request.CourseCode, cancellationToken);
        if (codeOwner is not null && codeOwner.Id != id)
            return (SaveCourseOutcome.DuplicateCourseCode, null);

        existing.Name = request.Name.Trim();
        existing.CourseCode = request.CourseCode.Trim();
        existing.Instructor = request.Instructor?.Trim();
        existing.Credits = request.Credits;
        existing.CurrentGrade = request.CurrentGrade.Trim();
        existing.GradePoints = GetGradePoints(request.CurrentGrade);

        await _repository.UpdateAsync(existing, cancellationToken);
        return (SaveCourseOutcome.Updated, existing);
    }

    /// <inheritdoc />
    public Task<bool> DeleteCourseAsync(int id, CancellationToken cancellationToken)
        => _repository.DeleteAsync(id, cancellationToken);

    /// <summary>Looks up how many GPA points a letter grade is worth. Unknown grades are worth 0.</summary>
    private static double GetGradePoints(string letterGrade)
        => GradeTable.TryGetValue(letterGrade.Trim(), out var points) ? points : 0.0;
}
