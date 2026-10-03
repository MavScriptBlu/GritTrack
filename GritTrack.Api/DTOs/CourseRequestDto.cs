using System.ComponentModel.DataAnnotations;

namespace GritTrack.Api.DTOs;

// what the client sends in. No Id, no CreatedAt, no GradePoints — those
// are ours to set, not theirs
public class CourseRequestDto
{
    [Required(ErrorMessage = "Course name is required.")]
    [StringLength(100, ErrorMessage = "Course name can't be longer than 100 characters.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Course code is required.")]
    [StringLength(10, ErrorMessage = "Course code can't be longer than 10 characters.")]
    public string CourseCode { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "Instructor name can't be longer than 100 characters.")]
    public string? Instructor { get; set; }

    [Range(0.0, 6.0, ErrorMessage = "Credits has to be between 0 and 6.")]
    public double Credits { get; set; }

    [Required(ErrorMessage = "Current grade is required.")]
    [StringLength(2, ErrorMessage = "Current grade should look like 'A', 'B+', 'C-', etc.")]
    public string CurrentGrade { get; set; } = string.Empty;
}
