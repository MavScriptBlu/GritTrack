using GritTrack.Api.Models;
using GritTrack.Api.Repositories;

namespace GritTrack.Api.Tests;

// stand-in for the real repo so tests never touch Data/courses.json
public class InMemoryCourseRepository : ICourseRepository
{
    private readonly List<Course> _courses = new();
    private readonly object _lock = new();

    public Task<List<Course>> GetAllAsync(CancellationToken cancellationToken)
    {
        lock (_lock)
            return Task.FromResult(_courses.ToList());
    }

    public Task<Course?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        lock (_lock)
            return Task.FromResult(_courses.FirstOrDefault(course => course.Id == id));
    }

    public Task<Course?> GetByCourseCodeAsync(string courseCode, CancellationToken cancellationToken)
    {
        lock (_lock)
            return Task.FromResult(_courses.FirstOrDefault(course =>
                string.Equals(course.CourseCode, courseCode, StringComparison.OrdinalIgnoreCase)));
    }

    public Task<Course> AddAsync(Course course, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            course.Id = _courses.Count == 0 ? 1 : _courses.Max(existing => existing.Id) + 1;
            _courses.Add(course);
            return Task.FromResult(course);
        }
    }

    public Task<bool> UpdateAsync(Course course, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            var position = _courses.FindIndex(existing => existing.Id == course.Id);
            if (position < 0)
                return Task.FromResult(false);

            _courses[position] = course;
            return Task.FromResult(true);
        }
    }

    public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        lock (_lock)
            return Task.FromResult(_courses.RemoveAll(course => course.Id == id) > 0);
    }
}
