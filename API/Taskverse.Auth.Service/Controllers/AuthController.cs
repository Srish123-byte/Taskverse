// Taskverse.API.Auth.Service/Controllers/AuthController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Taskverse.API.Auth.Service.Models;
using Taskverse.API.Auth.Service.Services;

namespace Taskverse.API.Auth.Service.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authService;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthenticationService authService,
        ITokenService tokenService,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _tokenService = tokenService;
        _logger = logger;
    }

    /// <summary>
    /// Login with email and password
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var response = await _authService.LoginAsync(request);
            if (response == null)
                return Unauthorized(new { message = "Invalid credentials" });

            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Login blocked: {Message}", ex.Message);
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Login error: {ex.Message}");
            return StatusCode(500, new { message = "An error occurred during login" });
        }
    }

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return BadRequest(new { message = "Refresh token is required" });

        try
        {
            var response = await _authService.RefreshTokenAsync(request.RefreshToken, request.AccessToken, request.ForceRotate);
            if (response == null)
                return Unauthorized(new { message = "Invalid or expired refresh token" });

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Refresh token error: {ex.Message}");
            return StatusCode(500, new { message = "An error occurred during token refresh" });
        }
    }

    /// <summary>
    /// Validate a token
    /// </summary>
    [HttpPost("validate")]
    public async Task<IActionResult> ValidateToken([FromBody] ValidateTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
            return BadRequest(new { message = "Token is required" });

        try
        {
            var principal = await _tokenService.ValidateTokenAsync(request.Token);
            if (principal == null)
                return Ok(new ValidateTokenResponse { IsValid = false });

            return Ok(new ValidateTokenResponse 
            { 
                IsValid = true,
                UserId = principal.FindFirstValue(ClaimTypes.NameIdentifier),
                Roles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList(),
                ExpiresAt = ResolveExpiryUtc(principal)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Token validation error: {ex.Message}");
            return Ok(new ValidateTokenResponse 
            { 
                IsValid = false,
                Message = "Token validation failed"
            });
        }
    }

    /// <summary>
    /// Logout user
    /// </summary>
    [AllowAnonymous]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
    {
        try
        {
            var requestUserId = request.UserId;
            if (string.IsNullOrWhiteSpace(requestUserId))
            {
                requestUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            }

            if (!Guid.TryParse(requestUserId, out var userGuid))
                return BadRequest(new { message = "Invalid user ID" });

            await _authService.LogoutAsync(userGuid, request.RefreshToken);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Logout error: {ex.Message}");
            return StatusCode(500, new { message = "An error occurred during logout" });
        }
    }

    [Authorize]
    [HttpPost("change-temporary-password")]
    public async Task<IActionResult> ChangeTemporaryPassword([FromBody] ChangeTemporaryPasswordRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var requestUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(requestUserId, out var userGuid))
            return Unauthorized(new { message = "Invalid user ID" });

        try
        {
            await _authService.ChangeTemporaryPasswordAsync(userGuid, request);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Change temporary password error: {ex.Message}");
            return StatusCode(500, new { message = "An error occurred while changing the temporary password" });
        }
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", service = "auth-service" });
    }

    private static DateTime? ResolveExpiryUtc(ClaimsPrincipal principal)
    {
        var expClaim = principal.FindFirst(JwtRegisteredClaimNames.Exp)?.Value;
        if (!long.TryParse(expClaim, out var expUnix))
            return null;

        return DateTimeOffset.FromUnixTimeSeconds(expUnix).UtcDateTime;
    }
}
