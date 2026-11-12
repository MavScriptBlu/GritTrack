namespace GritTrack.Api.Configuration;

/// <summary>The two places courses can live. appsettings.json "DataSource" picks one when the app starts.</summary>
public static class DataSources
{
    public const string Json = "Json";
    public const string SqlServer = "SqlServer";
}
