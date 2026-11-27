using GritTrack.Api.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace GritTrack.Api.Tests;

// boots the real API in memory: no ports, no certs, nothing leaves the process
internal class ApiFactory : WebApplicationFactory<Program>
{
    // throwaway values that only exist inside the test run. not the real secrets
    public const string DemoUserName = "demo"; // matches Auth:DemoUserName in appsettings.json
    public const string DemoPassword = "test-only-password";

    static ApiFactory()
    {
        // env vars beat every other config source, so the test values always win
        Environment.SetEnvironmentVariable("DataSource", "Json");
        Environment.SetEnvironmentVariable("Auth__Enabled", "true");
        Environment.SetEnvironmentVariable("Auth__SigningKey", "test-only-signing-key-that-is-long-enough-123456");
        Environment.SetEnvironmentVariable("Auth__DemoPassword", DemoPassword);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // not "Development", so the dev machine's user-secrets never get loaded
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // swap the JSON-file repo for the in-memory one
            var realRepositories = services.Where(descriptor => descriptor.ServiceType == typeof(ICourseRepository)).ToList();
            foreach (var descriptor in realRepositories)
                services.Remove(descriptor);

            services.AddSingleton<ICourseRepository, InMemoryCourseRepository>();
        });
    }

    // https base address + no auto-redirect, so the https-redirect middleware leaves requests alone
    public HttpClient CreateApiClient() => CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false
    });
}
