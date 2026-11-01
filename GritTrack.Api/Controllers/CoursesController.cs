using GritTrack.Api.Configuration;
using GritTrack.Api.DTOs;
using GritTrack.Api.Models;
using GritTrack.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GritTrack.Api.Controllers;

/// <summary>CRUD for courses. Thin on purpose — every action just calls the service once and maps the result.</summary>
[ApiController]
[Route("api/courses")]
public class CoursesController : ControllerBase
{
    private readonly ICourseService _service;

    public CoursesController(ICourseService service)
    {
        _service = service;
    }

    /// <summary>GET /api/courses, optionally narrowed with ?grade=.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CourseResponseDto>>> GetAll(
        [FromQuery] string? grade, CancellationToken cancellationToken)
    {
        var courses = await _service.GetCoursesAsync(grade, cancellationToken);
        return Ok(courses.Select(ToResponseDto));
    }

    /// <summary>GET /api/courses/{id}.</summary>
    [HttpGet("{id:int}", Name = nameof(GetById))]
    public async Task<ActionResult<CourseResponseDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var course = await _service.GetCourseByIdAsync(id, cancellationToken);
        return course is null ? NotFound() : Ok(ToResponseDto(course));
    }

    /// <summary>POST /api/courses. 401 without a token (when auth is on), 409 if the course code is already taken.</summary>
    [HttpPost]
    [Authorize(Policy = AuthPolicies.WriteAccess)]
    public async Task<ActionResult<CourseResponseDto>> Create(
        CourseRequestDto request, CancellationToken cancellationToken)
    {
        var (outcome, course) = await _service.CreateCourseAsync(request, cancellationToken);

        return outcome switch
        {
            SaveCourseOutcome.DuplicateCourseCode => DuplicateCourseCodeProblem(request.CourseCode),
            SaveCourseOutcome.Created => CreatedAtAction(nameof(GetById), new { id = course!.Id }, ToResponseDto(course)),
            _ => throw new InvalidOperationException($"Unexpected create outcome: {outcome}.")
        };
    }

    /// <summary>PUT /api/courses/{id}. 404s if it doesn't exist, 409s if the new code is already taken by a different course.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Policy = AuthPolicies.WriteAccess)]
    public async Task<ActionResult<CourseResponseDto>> Update(
        int id, CourseRequestDto request, CancellationToken cancellationToken)
    {
        var (outcome, course) = await _service.UpdateCourseAsync(id, request, cancellationToken);

        return outcome switch
        {
            SaveCourseOutcome.NotFound => NotFound(),
            SaveCourseOutcome.DuplicateCourseCode => DuplicateCourseCodeProblem(request.CourseCode),
            SaveCourseOutcome.Updated => Ok(ToResponseDto(course!)),
            _ => throw new InvalidOperationException($"Unexpected update outcome: {outcome}.")
        };
    }

    /// <summary>DELETE /api/courses/{id}.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = AuthPolicies.WriteAccess)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await _service.DeleteCourseAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    /// <summary>The 409 body: a standard ProblemDetails, so clients read errors one way for every status code.</summary>
    private ObjectResult DuplicateCourseCodeProblem(string courseCode) => Problem(
        statusCode: StatusCodes.Status409Conflict,
        title: "Duplicate course code",
        detail: $"A course with code '{courseCode}' already exists.");

    /// <summary>Maps the stored Course to what the client's actually allowed to see.</summary>
    private static CourseResponseDto ToResponseDto(Course course) => new()
    {
        Id = course.Id,
        Name = course.Name,
        CourseCode = course.CourseCode,
        Instructor = course.Instructor,
        Credits = course.Credits,
        CurrentGrade = course.CurrentGrade,
        GradePoints = course.GradePoints,
        CreatedAt = course.CreatedAt
    };
}
