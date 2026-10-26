using GritTrack.PageModels;
using GritTrack.Pages;
using GritTrack.Services;
using GritTrack.Services.Api;
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

            // courses now come from the GritTrack API. the app never talks to
            // SQL Server itself: it only calls the API over HTTP.
            // BearerTokenHandler adds the login token to every write.
            // (InMemoryCourseRepository is still in Services/ for tests, but
            // it isn't registered here anymore)
            builder.Services.AddSingleton<IAuthService, AuthService>();
            builder.Services.AddTransient<BearerTokenHandler>();

            // used only for logging in: no token handler, so login can't loop back on itself
            builder.Services.AddHttpClient(AuthService.HttpClientName, ConfigureApiClient)
                .ConfigurePrimaryHttpMessageHandler(DevelopmentHttpHandler.Create);

            builder.Services.AddHttpClient<ICourseRepository, ApiCourseRepository>(ConfigureApiClient)
                .ConfigurePrimaryHttpMessageHandler(DevelopmentHttpHandler.Create)
                .AddHttpMessageHandler<BearerTokenHandler>();

            // same idea, for the starting GPA instead of courses
            builder.Services.AddSingleton<IGpaProfileRepository, InMemoryGpaProfileRepository>();

            builder.Services.AddSingleton<IThemeService, ThemeService>();

            // PageModels use this to move between pages and show alerts,
            // so none of them ever touch Shell directly
            builder.Services.AddSingleton<INavigationService, ShellNavigationService>();

            // the Shell and the PageModel that feeds its flyout title
            builder.Services.AddSingleton<AppShellViewModel>();
            builder.Services.AddSingleton<AppShell>();

            // a new ViewModel + Page each time you navigate to them
            builder.Services.AddTransient<CourseListPageModel>();
            builder.Services.AddTransient<CourseListPage>();
            builder.Services.AddTransient<CourseDetailPageModel>();
            builder.Services.AddTransient<CourseDetailPage>();
            builder.Services.AddTransient<AddCoursePageModel>();
            builder.Services.AddTransient<AddCoursePage>();
            builder.Services.AddTransient<LoginPageModel>();
            builder.Services.AddTransient<LoginPage>();

            return builder.Build();
        }

        // points an HttpClient at the API (address depends on the device, see ApiSettings)
        private static void ConfigureApiClient(HttpClient client)
        {
            client.BaseAddress = new Uri(ApiSettings.BaseAddress);
            client.Timeout = ApiSettings.RequestTimeout;
        }
    }
}
