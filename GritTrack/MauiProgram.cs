using GritTrack.PageModels;
using GritTrack.Pages;
using GritTrack.Services;
using Microsoft.Extensions.Logging;

namespace GritTrack
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("Kalam-Bold.ttf", "KalamBold");
                    fonts.AddFont("ArchitectsDaughter-Regular.ttf", "ArchitectsDaughter");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif
            // no state of its own, just math — fine to share one instance
            builder.Services.AddTransient<GpaCalculatorService>();

            // Singleton so the course list survives navigation for the
            // app's run. swap to a real backend later by pointing this at
            // a different ICourseRepository — nothing else has to change
            builder.Services.AddSingleton<ICourseRepository, InMemoryCourseRepository>();

            // same idea, for the starting GPA instead of courses
            builder.Services.AddSingleton<IGpaProfileRepository, InMemoryGpaProfileRepository>();

            builder.Services.AddSingleton<IThemeService, ThemeService>();

            // a new ViewModel + Page each time you navigate to them
            builder.Services.AddTransient<CourseListPageModel>();
            builder.Services.AddTransient<CourseListPage>();
            builder.Services.AddTransient<CourseDetailPageModel>();
            builder.Services.AddTransient<CourseDetailPage>();
            builder.Services.AddTransient<AddCoursePageModel>();
            builder.Services.AddTransient<AddCoursePage>();

            return builder.Build();
        }
    }
}
