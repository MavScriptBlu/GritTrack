using GritTrack.Api.Configuration;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace GritTrack.Api.Tests.Sql;

// the connection string is the one place the SQL password shows up, so check it builds right
// and that a missing password stops the app with a message I can actually act on
public class SqlConnectionStringFactoryTests
{
    private const string FakePassword = "test-only-Password-123";

    private static IConfiguration BuildConfiguration(Dictionary<string, string?> values)
        => new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    [Fact]
    public void Build_UsesDefaultServerAndDatabase_WhenOnlyThePasswordIsSet()
    {
        var configuration = BuildConfiguration(new() { [SqlConnectionStringFactory.PasswordKey] = FakePassword });

        var builder = new SqlConnectionStringBuilder(SqlConnectionStringFactory.Build(configuration));

        Assert.Equal("127.0.0.1,1433", builder.DataSource);
        Assert.Equal("GritTrack", builder.InitialCatalog);
        Assert.Equal("sa", builder.UserID);
        Assert.Equal(FakePassword, builder.Password);
    }

    [Fact]
    public void Build_UsesServerAndDatabaseFromSettings()
    {
        var configuration = BuildConfiguration(new()
        {
            [SqlConnectionStringFactory.PasswordKey] = FakePassword,
            ["Database:Server"] = "sql.example.test,14330",
            ["Database:Name"] = "GritTrackScratch"
        });

        var builder = new SqlConnectionStringBuilder(SqlConnectionStringFactory.Build(configuration));

        Assert.Equal("sql.example.test,14330", builder.DataSource);
        Assert.Equal("GritTrackScratch", builder.InitialCatalog);
    }

    [Fact]
    public void Build_EncryptsTheConnection_AndTrustsTheSelfSignedDevCertificate()
    {
        var configuration = BuildConfiguration(new() { [SqlConnectionStringFactory.PasswordKey] = FakePassword });

        var builder = new SqlConnectionStringBuilder(SqlConnectionStringFactory.Build(configuration));

        Assert.True(builder.Encrypt == SqlConnectionEncryptOption.Mandatory);
        Assert.True(builder.TrustServerCertificate);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Build_Throws_WhenThePasswordIsMissingOrBlank(string? password)
    {
        var configuration = BuildConfiguration(new() { [SqlConnectionStringFactory.PasswordKey] = password });

        var exception = Assert.Throws<InvalidOperationException>(() => SqlConnectionStringFactory.Build(configuration));

        // tells me which setting to fix and where
        Assert.Contains(SqlConnectionStringFactory.PasswordKey, exception.Message);
        Assert.Contains(".env", exception.Message);
    }

    [Fact]
    public void Build_NeverPutsThePasswordInTheErrorMessage()
    {
        // the error path has no password to leak, but pin it down so a future edit can't add one
        var configuration = BuildConfiguration(new() { ["Database:Server"] = "sql.example.test" });

        var exception = Assert.Throws<InvalidOperationException>(() => SqlConnectionStringFactory.Build(configuration));

        Assert.DoesNotContain(FakePassword, exception.Message);
    }
}
