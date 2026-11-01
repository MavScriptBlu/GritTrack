using GritTrack.Api.Configuration;
using GritTrack.Api.DTOs;
using GritTrack.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace GritTrack.Api.Controllers;

/// <summary>Login. Thin on purpose: the token service does the real work.</summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IJwtTokenService _tokenService;
    private readonly AuthOptions _options;

    private readonly ILogger<AuthController> _logger;

    public AuthController(IJwtTokenService tokenService, IOptions<AuthOptions> options, ILogger<AuthController> logger)
    {
        _tokenService = tokenService;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>POST /api/auth/login. 200 with a token, 400 if a field is missing, 401 for a wrong user name or password.</summary>
    [HttpPost("login")]
    public ActionResult<LoginResponseDto> Login(LoginRequestDto request)
    {
        // with auth off there is nothing to log in to
        if (!_options.Enabled)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Authentication is turned off",
                detail: "Writes do not need a token while Auth:Enabled is false.");
        }

        if (!_tokenService.AreCredentialsValid(request.UserName, request.Password))
        {
            // log that it happened, never what was typed: no user name, no password
            _logger.LogWarning("Login failed");

            // same message for a wrong user name and a wrong password on purpose
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Login failed",
                detail: "User name or password is wrong.");
        }

        return Ok(_tokenService.CreateToken(request.UserName));
    }
}
