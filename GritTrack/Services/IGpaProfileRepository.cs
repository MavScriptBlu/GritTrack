namespace GritTrack.Services
{
    /// <summary>
    /// Holds a student's starting GPA — the GPA they already had before
    /// they started using GritTrack (past semesters, transfer credits,
    /// whatever). Kept separate from ICourseRepository (courses) because
    /// it's a different kind of data. It's its own interface (not a real
    /// class) so we can swap the storage for something real later — like
    /// SQLite or a database — without changing anything else in the app.
    /// </summary>
    public interface IGpaProfileRepository
    {
        // grabs the saved starting GPA info
        Task<GpaProfile> GetProfileAsync();

        // saves new starting GPA info
        Task SaveProfileAsync(GpaProfile profile);
    }

    /// <summary>A student's starting GPA and how many credits it's based on.</summary>
    public class GpaProfile
    {
        public double StartingGpa { get; set; }
        public double StartingCredits { get; set; }
    }
}
