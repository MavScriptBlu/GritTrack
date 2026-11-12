using GritTrack.Api.Data;
using GritTrack.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GritTrack.Api.Repositories;

/// <summary>Stores courses in SQL Server through EF Core. Same promises as the JSON repo, different home.</summary>
public class SqlCourseRepository : ICourseRepository
{
    private readonly GritTrackDbContext _context;

    public SqlCourseRepository(GritTrackDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public Task<List<Course>> GetAllAsync(CancellationToken cancellationToken)
        => _context.Courses.AsNoTracking().OrderBy(c => c.Id).ToListAsync(cancellationToken);

    /// <inheritdoc />
    public Task<Course?> GetByIdAsync(int id, CancellationToken cancellationToken)
        => _context.Courses.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<Course?> GetByCourseCodeAsync(string courseCode, CancellationToken cancellationToken)
    {
        // the column compares without caring about upper or lower case, same as the JSON repo
        var trimmedCode = courseCode.Trim();
        return _context.Courses.FirstOrDefaultAsync(c => c.CourseCode == trimmedCode, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Course> AddAsync(Course course, CancellationToken cancellationToken)
    {
        // SQL Server hands out the id when it saves
        _context.Courses.Add(course);
        await _context.SaveChangesAsync(cancellationToken);
        return course;
    }

    /// <inheritdoc />
    public async Task<bool> UpdateAsync(Course course, CancellationToken cancellationToken)
    {
        var stored = await _context.Courses.FirstOrDefaultAsync(c => c.Id == course.Id, cancellationToken);
        if (stored is null)
            return false;

        // when the caller already edited the tracked row itself there is nothing to copy
        if (!ReferenceEquals(stored, course))
            _context.Entry(stored).CurrentValues.SetValues(course);

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var stored = await _context.Courses.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (stored is null)
            return false;

        _context.Courses.Remove(stored);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
