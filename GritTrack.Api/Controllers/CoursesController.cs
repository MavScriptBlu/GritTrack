using GritTrack.Api.DTOs;
using GritTrack.Api.Models;
using GritTrack.Api.Services;
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

    // GET /api/courses
    // GET /api/courses?grade=B+
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CourseResponseDto>>> GetAll(
        [FromQuery] string? grade, CancellationToken cancellationToken)
    {
        var courses = await _service.GetCoursesAsync(grade, cancellationToken);
        return Ok(courses.Select(ToResponseDto));
    }

    [HttpGet("{id:int}", Name = nameof(GetById))]
    public async Task<ActionResult<CourseResponseDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var course = await _service.GetCourseByIdAsync(id, cancellationToken);
        return course is null ? NotFound() : Ok(ToResponseDto(course));
    }

    [HttpPost]
    public async Task<ActionResult<CourseResponseDto>> Create(
        CourseRequestDto request, CancellationToken cancellationToken)
    {
        var (outcome, course) = await _service.CreateCourseAsync(request, cancellationToken);

        return outcome switch
        {
            SaveCourseOutcome.DuplicateCourseCode => Conflict(new
            {
                message = $"A course with code '{request.CourseCode}' already exists."
            }),
            SaveCourseOutcome.Created => CreatedAtAction(nameof(GetById), new { id = course!.Id }, ToResponseDto(course)),
            _ => Problem()
        };
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<CourseResponseDto>> Update(
        int id, CourseRequestDto request, CancellationToken cancellationToken)
    {
        var (outcome, course) = await _service.UpdateCourseAsync(id, request, cancellationToken);

        return outcome switch
        {
            SaveCourseOutcome.NotFound => NotFound(),
            SaveCourseOutcome.DuplicateCourseCode => Conflict(new
            {
                message = $"A course with code '{request.CourseCode}' already exists."
            }),
            SaveCourseOutcome.Updated => Ok(ToResponseDto(course!)),
            _ => Problem()
        };
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await _service.DeleteCourseAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

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
