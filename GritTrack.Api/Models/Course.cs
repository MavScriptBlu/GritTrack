// ============================================================================
// Course model - what gets stored in the database
// Id and CreatedAt are server-controlled, clients can't touch them
// ============================================================================

namespace GritTrack.Api.Models;

public class Course
{
    public int Id { get; set; }                            // auto-generated
    public string Name { get; set; } = string.Empty;       // "Data Structures"
    public string CourseCode { get; set; } = string.Empty; // "CS201" - must be unique
    public string? Instructor { get; set; }                // optional
    public double Credits { get; set; }                    // typically 1-4
    public string CurrentGrade { get; set; } = string.Empty; // A, B+, C-, etc
    public double GradePoints { get; set; }                // calculated from grade
    public DateTime CreatedAt { get; set; }                // server timestamp
}
