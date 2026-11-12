using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using GritTrack.Api.Configuration;
using GritTrack.Api.DTOs;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace GritTrack.Api.Services;

/// <inheritdoc cref="IJwtTokenService" />
public class JwtTokenService : IJwtTokenService
{
    private readonly AuthOptions _options;

    public JwtTokenService(IOptions<AuthOptions> options)
    {
        _options = options.Value;
    }

    /// <inheritdoc />
    public bool AreCredentialsValid(string userName, string password)
    {
        // both checks always run and compare in fixed time, so a wrong guess
        // can't be told apart from a right one by how long the answer took
        var userNameMatches = FixedTimeTextEquals(userName, _options.DemoUserName);
        var passwordMatches = FixedTimeTextEquals(password, _options.DemoPassword ?? string.Empty);
        return userNameMatches & passwordMatches;
    }

    /// <inheritdoc />
    public LoginResponseDto CreateToken(string userName)
    {
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(_options.TokenMinutes);
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey!));

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Subject = new ClaimsIdentity([new Claim(ClaimTypes.Name, userName)]),
            Expires = expiresAtUtc,
            SigningCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256)
        };

        return new LoginResponseDto
        {
            Token = new JsonWebTokenHandler().CreateToken(descriptor),
            ExpiresAtUtc = expiresAtUtc
        };
    }

    /// <summary>Compares two strings by their hashes, so the comparison time doesn't depend on where they differ.</summary>
    private static bool FixedTimeTextEquals(string first, string second)
        => CryptographicOperations.FixedTimeEquals(
            SHA256.HashData(Encoding.UTF8.GetBytes(first)),
            SHA256.HashData(Encoding.UTF8.GetBytes(second)));
}
