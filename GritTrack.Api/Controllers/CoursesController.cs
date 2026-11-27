// ============================================================================
// CoursesController - CRUD for courses, the bread and butter of GritTrack
// thin controller pattern: just maps HTTP to service calls, nothing fancy
// ============================================================================

using GritTrack.Api.Configuration;
using GritTrack.Api.DTOs;
using GritTrack.Api.Models;
using GritTrack.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GritTrack.Api.Controllers;

[ApiController]
[Route("api/courses")]
public class CoursesController : ControllerBase
{
    private readonly ICourseService _service;

    public CoursesController(ICourseService service)
    {
        _service = service;
    }

    // GET /api/courses or GET /api/courses?grade=A - filter is optional
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CourseResponseDto>>> GetAll(
        [FromQuery] string? grade, CancellationToken cancellationToken)
    {
        var courses = await _service.GetCoursesAsync(grade, cancellationToken);
        return Ok(courses.Select(ToResponseDto));
    }

    // GET /api/courses/42 - fetch one course by id
    [HttpGet("{id:int}", Name = nameof(GetById))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CourseResponseDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var course = await _service.GetCourseByIdAsync(id, cancellationToken);
        return course is null ? NotFound() : Ok(ToResponseDto(course));
    }

    // POST /api/courses - create a new course, 409 if course code already exists
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
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

    // PUT /api/courses/42 - update existing course, 404 if gone, 409 if code collision
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
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

    // DELETE /api/courses/42 - bye bye course
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Authorize(Policy = AuthPolicies.WriteAccess)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await _service.DeleteCourseAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    // 409 response - someone already has this course code
    private ObjectResult DuplicateCourseCodeProblem(string courseCode) => Problem(
        statusCode: StatusCodes.Status409Conflict,
        title: "Duplicate course code",
        detail: $"A course with code '{courseCode}' already exists.");

    // map internal model to what the client sees - keeps the DB shape private
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
