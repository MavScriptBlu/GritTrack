namespace GritTrack.Api.Models;

/// <summary>What actually gets stored. Id and CreatedAt are set by the server — never by whoever's calling the API.</summary>
public class Course
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
