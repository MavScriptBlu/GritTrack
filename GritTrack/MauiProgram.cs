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

            // a new ViewModel + Page each time you navigate to them
            builder.Services.AddTransient<CourseListPageModel>();
            builder.Services.AddTransient<CourseListPage>();
            builder.Services.AddTransient<CourseDetailPageModel>();
            builder.Services.AddTransient<CourseDetailPage>();

            return builder.Build();
        }
    }
}
