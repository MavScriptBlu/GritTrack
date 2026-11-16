using GritTrack.Api.Models;
using GritTrack.Api.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

namespace GritTrack.Api.Tests;

// the real JSON file repo, pointed at a throwaway folder so the
// actual Data/courses.json never gets touched
public class JsonCourseRepositoryTests : IDisposable
{
    private readonly string _contentRoot = Path.Combine(Path.GetTempPath(), "grittrack-tests-" + Guid.NewGuid().ToString("N"));
    private readonly JsonCourseRepository _repository;

    public JsonCourseRepositoryTests()
    {
        Directory.CreateDirectory(_contentRoot);
        _repository = new JsonCourseRepository(new FakeEnvironment(_contentRoot));
    }

    public void Dispose() => Directory.Delete(_contentRoot, recursive: true);

    private string DataFile => Path.Combine(_contentRoot, "Data", "courses.json");

    private static Course NewCourse(string code) => new()
    {
        Name = "Data Structures",
        CourseCode = code,
        Credits = 3,
        CurrentGrade = "A",
        GradePoints = 4.0,
        CreatedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task GetAll_WithNoFile_ReturnsEmptyList()
    {
        var courses = await _repository.GetAllAsync(CancellationToken.None);

        Assert.Empty(courses);
    }

    [Fact]
    public async Task GetAll_WithEmptyFile_ReturnsEmptyList()
    {
        await File.WriteAllTextAsync(DataFile, string.Empty);

        var courses = await _repository.GetAllAsync(CancellationToken.None);

        Assert.Empty(courses);
    }

    [Fact]
    public async Task Add_AssignsIdsAndWritesTheFile()
    {
        var first = await _repository.AddAsync(NewCourse("CS201"), CancellationToken.None);
        var second = await _repository.AddAsync(NewCourse("CS202"), CancellationToken.None);

        Assert.Equal(1, first.Id);
        Assert.Equal(2, second.Id);
        Assert.Contains("CS202", await File.ReadAllTextAsync(DataFile));

        // the temp file is only a stepping stone, it shouldn't hang around
        Assert.False(File.Exists(DataFile + ".tmp"));
    }

    [Fact]
    public async Task Add_FromManyCallersAtOnce_NeverHandsOutTheSameId()
    {
        var adds = Enumerable.Range(0, 20)
            .Select(number => _repository.AddAsync(NewCourse($"CS{number}"), CancellationToken.None));

        var saved = await Task.WhenAll(adds);

        Assert.Equal(20, saved.Select(course => course.Id).Distinct().Count());
        Assert.Equal(20, (await _repository.GetAllAsync(CancellationToken.None)).Count);
    }

    [Fact]
    public async Task UpdateAndDelete_OnMissingId_ReturnFalse()
    {
        var ghost = NewCourse("CS999");
        ghost.Id = 42;

        Assert.False(await _repository.UpdateAsync(ghost, CancellationToken.None));
        Assert.False(await _repository.DeleteAsync(42, CancellationToken.None));
    }

    // just enough of the hosting environment for the repo to find its folder
    private sealed class FakeEnvironment(string contentRoot) : IWebHostEnvironment
    {
        public string ContentRootPath { get; set; } = contentRoot;
        public string WebRootPath { get; set; } = contentRoot;
        public string EnvironmentName { get; set; } = "Testing";
        public string ApplicationName { get; set; } = "GritTrack.Api.Tests";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
}
