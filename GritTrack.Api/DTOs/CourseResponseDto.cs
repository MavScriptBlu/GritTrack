namespace GritTrack.Api.DTOs;

// what the client gets back — includes everything, even the fields it
// couldn't set itself
public class CourseResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CourseCode { get; set; } = string.Empty;
    public string? Instructor { get; set; }
    public double Credits { get; set; }
    public string CurrentGrade { get; set; } = string.Empty;
    public double GradePoints { get; set; }
    public DateTime CreatedAt { get; set; }
}
