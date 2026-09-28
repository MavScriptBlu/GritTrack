namespace GritTrack.Services
{
    // the dynamic resource in the app — rewrites the accent color at
    // runtime, and every {DynamicResource AccentColor} spot repaints
    // instantly. called from CourseListPageModel every time GPA
    // recalculates, so the whole app's color matches how you're doing
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

            // setting this key directly is what makes DynamicResource
            // bindings react everywhere in the app
            Application.Current.Resources["AccentColor"] = accent;
        }
    }
}
