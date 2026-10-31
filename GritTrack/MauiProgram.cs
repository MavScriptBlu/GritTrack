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
            // no state of its own, just math - fine to share one instance
            builder.Services.AddTransient<GpaCalculatorService>();

            // courses now live behind the API. the app never opens a SQL
            // connection itself, it only ever makes HTTP calls to GritTrack.Api
            // (which decides on its own if that's a JSON file or SQL Server)
            builder.Services.AddSingleton<IAuthService, AuthService>();
            builder.Services.AddTransient<BearerTokenHandler>();

            // login's client has no bearer handler: it's how you get the token
            builder.Services.AddHttpClient(AuthService.HttpClientName, ConfigureApiClient)
                .ConfigurePrimaryHttpMessageHandler(DevelopmentHttpHandler.Create);

            // the courses client stamps the saved token on every request
            builder.Services.AddHttpClient<ICourseRepository, ApiCourseRepository>(ConfigureApiClient)
                .ConfigurePrimaryHttpMessageHandler(DevelopmentHttpHandler.Create)
                .AddHttpMessageHandler<BearerTokenHandler>();

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
            builder.Services.AddTransient<LoginPageModel>();
            builder.Services.AddTransient<LoginPage>();

            return builder.Build();
        }

        // points an HttpClient at the API (the address depends on the device, see ApiSettings)
        private static void ConfigureApiClient(HttpClient client)
        {
            client.BaseAddress = new Uri(ApiSettings.BaseAddress);
            client.Timeout = ApiSettings.RequestTimeout;
        }
    }
}
