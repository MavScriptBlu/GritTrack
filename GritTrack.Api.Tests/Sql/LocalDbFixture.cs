using GritTrack.Api.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace GritTrack.Api.Tests.Sql;

// finds out once whether this machine has SQL Server LocalDB (it ships with Visual Studio).
// the real SQL tests use it so they hit a true SQL Server without needing Docker or my .env password
public static class LocalDb
{
    private const string Server = @"(localdb)\MSSQLLocalDB";

    private static readonly Lazy<bool> IsAvailableLazy = new(Probe);

    public static bool IsAvailable => IsAvailableLazy.Value;

    // windows login, no password anywhere. LocalDB only listens to the current Windows user anyway
    public static string ConnectionStringFor(string databaseName) => new SqlConnectionStringBuilder
    {
        DataSource = Server,
        InitialCatalog = databaseName,
        IntegratedSecurity = true,
        Encrypt = false,
        ConnectTimeout = 60
    }.ConnectionString;

    private static bool Probe()
    {
        if (!OperatingSystem.IsWindows())
            return false;

        try
        {
            using var connection = new SqlConnection(ConnectionStringFor("master"));
            connection.Open();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}

// a [Fact] that skips itself (instead of failing) when LocalDB isn't on this machine
public sealed class LocalDbFactAttribute : FactAttribute
{
    public LocalDbFactAttribute()
    {
        if (!LocalDb.IsAvailable)
            Skip = "SQL Server LocalDB isn't available on this machine, so the real SQL Server test was skipped.";
    }
}

// one throwaway database per test class: migrated for real, then deleted when the tests finish
public sealed class LocalDbFixture : IDisposable
{
    public string DatabaseName { get; } = "GritTrackTests_" + Guid.NewGuid().ToString("N");

    public LocalDbFixture()
    {
        if (!LocalDb.IsAvailable)
            return;

        using var context = CreateContext();
        context.Database.Migrate();
    }

    // a fresh context each time, like one request in the real API
    public GritTrackDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<GritTrackDbContext>()
            .UseSqlServer(LocalDb.ConnectionStringFor(DatabaseName))
            .Options;

        return new GritTrackDbContext(options);
    }

    public void Dispose()
    {
        if (!LocalDb.IsAvailable)
            return;

        using var context = CreateContext();
        context.Database.EnsureDeleted();
    }
}
