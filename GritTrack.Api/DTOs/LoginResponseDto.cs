namespace GritTrack.Api.DTOs;

/// <summary>What a successful login returns: the token to send on every write, and when it stops working.</summary>
public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
}
