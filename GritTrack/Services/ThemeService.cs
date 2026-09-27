namespace GritTrack.Services
{
    /// <summary>
    /// This is the DYNAMIC resource in GritTrack — different from the static
    /// gradient brushes each page defines once in ContentPage.Resources and
    /// never touches again. ApplyGpaStatusTheme rewrites
    /// Application.Current.Resources["AccentColor"] at RUNTIME, and every
    /// {DynamicResource AccentColor} binding across the app repaints
    /// immediately — no page reload, no manual UI refresh needed.
    ///
    /// Triggered from CourseListPageModel every time GPA recalculates, so
    /// the whole app's accent color reflects how you're actually doing:
    /// calm mint while you're Safe, amber on Watch, pink once you're AtRisk.
    /// </summary>
    public class ThemeService : IThemeService
    {
        public void ApplyGpaStatusTheme(GpaStatus status)
        {
            if (Application.Current is null)
                return;

            var accent = status switch
            {
                GpaStatus.Safe => Color.FromArgb("#9fe0bd"),   // Mint
                GpaStatus.Watch => Color.FromArgb("#f2e3a6"),  // Butter
                GpaStatus.AtRisk => Color.FromArgb("#f6adbf"), // Pink
                _ => Color.FromArgb("#20B2AA")                 // lightseagreen default
            };

            // setting the key directly on the top-level Application resource
            // dictionary is what makes DynamicResource bindings react —
            // MAUI raises ResourcesChanged and every consumer repaints
            Application.Current.Resources["AccentColor"] = accent;
        }
    }
}
