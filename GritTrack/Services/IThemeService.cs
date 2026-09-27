namespace GritTrack.Services
{
    public interface IThemeService
    {
        /// <summary>Rewrites the app-wide accent color to match the given GPA status.</summary>
        void ApplyGpaStatusTheme(GpaStatus status);
    }
}
