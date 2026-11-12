using System.ComponentModel.DataAnnotations;

namespace GritTrack.Api.DTOs;

/// <summary>What the client sends to log in.</summary>
public class LoginRequestDto
{
    [Required(ErrorMessage = "User name is required.")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    public string Password { get; set; } = string.Empty;
}
