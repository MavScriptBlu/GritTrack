using GritTrack.Api.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GritTrack.Api.Data;

/// <summary>
/// Lets "dotnet ef" build the DbContext on its own, no matter which DataSource is picked in appsettings.json.
/// </summary>
public class GritTrackDbContextFactory : IDesignTimeDbContextFactory<GritTrackDbContext>
{
    public GritTrackDbContext CreateDbContext(string[] args)
    {
        DotEnvLoader.Load(Directory.GetCurrentDirectory());

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .AddUserSecrets<GritTrackDbContextFactory>(optional: true)
            .Build();

        var options = new DbContextOptionsBuilder<GritTrackDbContext>()
            .UseSqlServer(SqlConnectionStringFactory.Build(configuration))
            .Options;

        return new GritTrackDbContext(options);
    }
}
