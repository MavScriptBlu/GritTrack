using System.Text.Json;
using GritTrack.Api.Models;

namespace GritTrack.Api.Repositories;

// stores courses in Data/courses.json. One semaphore guards every method
// that touches the file, so two requests can never read or write it at
// the same time and corrupt it.
public class JsonCourseRepository : ICourseRepository
{
    private readonly string _filePath;
    private readonly SemaphoreSlim _fileLock = new(1, 1);

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    public JsonCourseRepository(IWebHostEnvironment environment)
    {
        var dataFolder = Path.Combine(environment.ContentRootPath, "Data");
        Directory.CreateDirectory(dataFolder);
        _filePath = Path.Combine(dataFolder, "courses.json");
    }

    public async Task<List<Course>> GetAllAsync(CancellationToken cancellationToken)
    {
        await _fileLock.WaitAsync(cancellationToken);
        try
        {
            return await ReadAllAsync(cancellationToken);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task<Course?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        await _fileLock.WaitAsync(cancellationToken);
        try
        {
            var courses = await ReadAllAsync(cancellationToken);
            return courses.FirstOrDefault(c => c.Id == id);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task<Course?> GetByCourseCodeAsync(string courseCode, CancellationToken cancellationToken)
    {
        await _fileLock.WaitAsync(cancellationToken);
        try
        {
            var courses = await ReadAllAsync(cancellationToken);
            return courses.FirstOrDefault(c =>
                string.Equals(c.CourseCode, courseCode, StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task<Course> AddAsync(Course course, CancellationToken cancellationToken)
    {
        await _fileLock.WaitAsync(cancellationToken);
        try
        {
            var courses = await ReadAllAsync(cancellationToken);

            // the repo hands out the id, not the caller — keeps two
            // requests from ever landing on the same one
            course.Id = courses.Count == 0 ? 1 : courses.Max(c => c.Id) + 1;
            courses.Add(course);

            await WriteAllAsync(courses, cancellationToken);
            return course;
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task<bool> UpdateAsync(Course course, CancellationToken cancellationToken)
    {
        await _fileLock.WaitAsync(cancellationToken);
        try
        {
            var courses = await ReadAllAsync(cancellationToken);
            var index = courses.FindIndex(c => c.Id == course.Id);
            if (index < 0)
                return false;

            courses[index] = course;
            await WriteAllAsync(courses, cancellationToken);
            return true;
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        await _fileLock.WaitAsync(cancellationToken);
        try
        {
            var courses = await ReadAllAsync(cancellationToken);
            var removed = courses.RemoveAll(c => c.Id == id);
            if (removed == 0)
                return false;

            await WriteAllAsync(courses, cancellationToken);
            return true;
        }
        finally
        {
            _fileLock.Release();
        }
    }

    // caller already holds _fileLock before either of these run
    private async Task<List<Course>> ReadAllAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_filePath))
            return new List<Course>();

        await using var stream = File.OpenRead(_filePath);
        if (stream.Length == 0)
            return new List<Course>();

        var courses = await JsonSerializer.DeserializeAsync<List<Course>>(stream, SerializerOptions, cancellationToken);
        return courses ?? new List<Course>();
    }

    // writes to a temp file first, then swaps it in — a crash mid-write
    // can't leave a half-written courses.json behind
    private async Task WriteAllAsync(List<Course> courses, CancellationToken cancellationToken)
    {
        var tempPath = _filePath + ".tmp";
        await using (var stream = File.Create(tempPath))
        {
            await JsonSerializer.SerializeAsync(stream, courses, SerializerOptions, cancellationToken);
        }

        File.Move(tempPath, _filePath, overwrite: true);
    }
}
