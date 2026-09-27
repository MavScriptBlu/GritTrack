namespace GritTrack.Services
{
    /// <summary>
    /// Stand-in storage for the starting GPA — just holds it in memory for
    /// now (same idea as InMemoryCourseRepository). It goes away when the
    /// app closes. Swapping this for real storage later is a one-line
    /// change in MauiProgram.cs, nothing else has to change.
    /// </summary>
    public class InMemoryGpaProfileRepository : IGpaProfileRepository
    {
        // starts at 0/0, meaning "no starting GPA set yet"
        private GpaProfile _profile = new GpaProfile { StartingGpa = 0, StartingCredits = 0 };

        public Task<GpaProfile> GetProfileAsync() => Task.FromResult(_profile);

        public Task SaveProfileAsync(GpaProfile profile)
        {
            _profile = profile;
            return Task.CompletedTask;
        }
    }
}
