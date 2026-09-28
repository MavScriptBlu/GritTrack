namespace GritTrack.Services
{
    // holds a student's starting GPA — whatever GPA they already had
    // before using GritTrack. separate from ICourseRepository since it's
    // a different kind of data, same swap-it-out-later idea though
    public interface IGpaProfileRepository
    {
        Task<GpaProfile> GetProfileAsync();

        Task SaveProfileAsync(GpaProfile profile);
    }

    // a starting GPA and how many credits it's based on
    public class GpaProfile
    {
        public double StartingGpa { get; set; }
        public double StartingCredits { get; set; }
    }
}
