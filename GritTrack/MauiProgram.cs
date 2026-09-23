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
            // one shared instance app-wide - it has no state of its own. just math
            builder.Services.AddTransient<GpaCalculatorService>();

            // Singleton so the "database" survives navigation for the app's
            // whole run, same way a cached API client would. Swapping to a
            // real backend later is a one-line change: point this
            // registration at a different ICourseRepository implementation
            // and nothing else in the app has to know.
            builder.Services.AddSingleton<ICourseRepository, InMemoryCourseRepository>();

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
