using GritTrack.Api.DTOs;

namespace GritTrack.Api.Services;

/// <summary>Checks the demo login and hands out signed tokens.</summary>
public interface IJwtTokenService
{
    /// <summary>true when the user name and password match the demo account.</summary>
    bool AreCredentialsValid(string userName, string password);

    /// <summary>Makes a signed token for the user, good for the configured number of minutes.</summary>
    LoginResponseDto CreateToken(string userName);
}
