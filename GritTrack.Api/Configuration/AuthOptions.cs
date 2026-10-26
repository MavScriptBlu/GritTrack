namespace GritTrack.Api.Configuration;

/// <summary>
/// The "Auth" section of the settings. Enabled, Issuer, Audience, DemoUserName and TokenMinutes live in
/// appsettings.json. SigningKey and DemoPassword are secrets: they only ever live in dotnet user-secrets.
/// </summary>
public class AuthOptions
{
    public const string SectionName = "Auth";

    /// <summary>true = writes need a login token. false = anyone can write (for the "auth off" demo).</summary>
    public bool Enabled { get; set; }

    public string Issuer { get; set; } = "GritTrack.Api";
    public string Audience { get; set; } = "GritTrack.Client";

    /// <summary>The one demo account's user name. Not a secret.</summary>
    public string DemoUserName { get; set; } = "demo";

    /// <summary>The demo account's password. Secret: user-secrets only.</summary>
    public string? DemoPassword { get; set; }

    /// <summary>The key that signs tokens. Secret: user-secrets only.</summary>
    public string? SigningKey { get; set; }

    /// <summary>How long a token works after login.</summary>
    public int TokenMinutes { get; set; } = 60;
}
