namespace GritTrack.Api.Configuration;

/// <summary>Names of the authorization policies, so controllers and Program.cs never type the string twice.</summary>
public static class AuthPolicies
{
    /// <summary>Needed for every create, update and delete. Reads (GET) stay public.</summary>
    public const string WriteAccess = "WriteAccess";
}
