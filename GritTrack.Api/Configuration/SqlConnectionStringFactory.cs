using Microsoft.Data.SqlClient;

namespace GritTrack.Api.Configuration;

/// <summary>
/// Builds the SQL Server connection string. Server and database name come from appsettings.json,
/// the password only ever comes from the environment (.env) or user secrets, never from a file in git.
/// </summary>
public static class SqlConnectionStringFactory
{
    /// <summary>Where the password lives: the MSSQL_SA_PASSWORD environment variable or user secret.</summary>
    public const string PasswordKey = "MSSQL_SA_PASSWORD";

    /// <summary>Builds the connection string, or stops with a clear message if the password is missing.</summary>
    public static string Build(IConfiguration configuration)
    {
        var password = configuration[PasswordKey];
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                $"DataSource is SqlServer but {PasswordKey} is not set. " +
                "Copy .env.example to .env and set it there (or use dotnet user-secrets).");
        }

        return new SqlConnectionStringBuilder
        {
            DataSource = configuration["Database:Server"] ?? "127.0.0.1,1433",
            InitialCatalog = configuration["Database:Name"] ?? "GritTrack",
            UserID = "sa",
            Password = password,
            Encrypt = true,
            // the dev container uses a self-signed certificate
            TrustServerCertificate = true
        }.ConnectionString;
    }
}
