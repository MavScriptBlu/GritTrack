using GritTrack.Models;
using GritTrack.Services;
using GritTrack.Services.Api;

namespace GritTrack.Tests;

// pretends to be the API. flip IsDown to "unplug" the server
public class FakeCourseRepository : ICourseRepository
{
    public List<Course> Courses { get; } = new();
    public Dictionary<int, List<Assignment>> Assignments { get; } = new();

    public bool IsDown { get; set; }

    // a course code the fake server refuses, like a duplicate
    public string? RejectedCourseCode { get; set; }

    // every write that actually reached the "server", in order
    public List<string> Calls { get; } = new();

    public Task<List<Course>> GetCoursesAsync()
    {
        ThrowIfDown();
        return Task.FromResult(Courses.Select(Copy).ToList());
    }

    public Task<Course> SaveCourseAsync(Course course)
    {
        ThrowIfDown();

        if (course.CourseCode == RejectedCourseCode)
            throw new ApiValidationException(new[] { "That course code is already in use." });

        if (course.ID == 0)
        {
            course.ID = Courses.Count == 0 ? 1 : Courses.Max(existing => existing.ID) + 1;
            Courses.Add(Copy(course));
            Calls.Add($"create {course.CourseCode}");
        }
        else
        {
            var index = Courses.FindIndex(existing => existing.ID == course.ID);
            if (index < 0)
                throw new ApiNotFoundException("That course no longer exists.");

            Courses[index] = Copy(course);
            Calls.Add($"update {course.CourseCode}");
        }

        return Task.FromResult(Copy(course));
    }

    public Task DeleteCourseAsync(int courseId)
    {
        ThrowIfDown();

        if (Courses.RemoveAll(existing => existing.ID == courseId) == 0)
            throw new ApiNotFoundException("That course no longer exists.");

        Calls.Add($"delete {courseId}");
        return Task.CompletedTask;
    }

    public Task<List<Assignment>> GetAssignmentsLocalAsync(int courseId)
        => Task.FromResult(Assignments.TryGetValue(courseId, out var found) ? found.ToList() : new List<Assignment>());

    public Task<AssignmentImportResult> SaveAssignmentsLocalAsync(int courseId, IEnumerable<Assignment> assignments)
    {
        var added = assignments.ToList();
        Assignments[courseId] = added;
        return Task.FromResult(new AssignmentImportResult(added.Count, 0, added));
    }

    public Task UpdateAssignmentLocalAsync(int courseId, Assignment assignment) => Task.CompletedTask;

    private void ThrowIfDown()
    {
        if (IsDown)
            throw new ApiUnreachableException("Couldn't reach the GritTrack server.");
    }

    private static Course Copy(Course course) => new()
    {
        ID = course.ID,
        Name = course.Name,
        CourseCode = course.CourseCode,
        Instructor = course.Instructor,
        Credits = course.Credits,
        CurrentGrade = course.CurrentGrade,
        GradePoints = course.GradePoints
    };
}
