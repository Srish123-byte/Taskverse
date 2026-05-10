using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Swashbuckle.AspNetCore.Annotations;
using Taskverse.Api.Filters;
using Taskverse.Api.Models;
using Taskverse.Business.DTOs;
using Taskverse.Business.Interface;

namespace Taskverse.Api.Controllers;

[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : Controller
{
    private readonly IAuthOrchestrator _authOrchestrator;

    public AuthController(IAuthOrchestrator authOrchestrator)
    {
        _authOrchestrator = authOrchestrator ?? throw new ArgumentNullException(nameof(authOrchestrator));
    }

    /// <summary>Authenticates a user and returns access and refresh tokens.</summary>
    [AllowAnonymous]
    [HttpPost("login")]
    [SwaggerResponse(200, "Login successful", typeof(LoginResponseModel))]
    [SwaggerResponse(400, "Invalid request")]
    [SwaggerResponse(401, "Invalid credentials")]
    public async Task<IActionResult> Login([FromBody] LoginRequestModel model)
    {
        try
        {
            var result = await _authOrchestrator.Login(new LoginRequestDto(model.Email, model.Password));
            if (result is null) return Unauthorized("Invalid credentials");
            return Ok(new LoginResponseModel
            {
                Token = result.AccessToken,
                RefreshToken = result.RefreshToken,
                ExpiresAt = result.ExpiresAt,
                User = new CurrentUserResponseModel
                {
                    UserId = result.UserId,
                    Email = result.Email,
                    FirstName = result.FirstName,
                    LastName = result.LastName,
                    Role = result.Roles.FirstOrDefault() ?? string.Empty,
                    IsActive = true
                }
            });
        }
        catch (Exception ex)
        {
            return Problem(ex.Message);
        }
    }

    /// <summary>Refreshes an access token using a valid refresh token.</summary>
    [AllowAnonymous]
    [HttpPost("refresh")]
    [SwaggerResponse(200, "Token refreshed", typeof(RefreshLoginResponseModel))]
    [SwaggerResponse(401, "Invalid or expired refresh token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestModel model)
    {
        try
        {
            var result = await _authOrchestrator.RefreshToken(new RefreshTokenRequestDto(model.RefreshToken));
            if (result is null) return Unauthorized("Invalid or expired refresh token");
            return Ok(new RefreshLoginResponseModel
            {
                Token = result.AccessToken,
                RefreshToken = result.RefreshToken,
                ExpiresAt = result.ExpiresAt
            });
        }
        catch (Exception ex)
        {
            return Problem(ex.Message);
        }
    }

    /// <summary>Logs out a user and invalidates their refresh token.</summary>
    [ServiceFilter(typeof(JwtTokenValidationFilter))]
    [HttpPost("logout")]
    [SwaggerResponse(204, "Logout successful")]
    [SwaggerResponse(401, "Unauthorized")]
    public async Task<IActionResult> Logout([FromBody] LogoutRequestModel model)
    {
        try
        {
            await _authOrchestrator.Logout(new LogoutRequestDto(model.UserId, model.RefreshToken));
            return NoContent();
        }
        catch (Exception ex)
        {
            return Problem(ex.Message);
        }
    }

    /// <summary>Validates a JWT token and returns its claims.</summary>
    [AllowAnonymous]
    [HttpPost("validate")]
    [SwaggerResponse(200, "Token validation result", typeof(ValidateTokenResponseModel))]
    public async Task<IActionResult> ValidateToken([FromBody] ValidateTokenRequestModel model)
    {
        try
        {
            var result = await _authOrchestrator.ValidateToken(new ValidateTokenRequestDto(model.Token));
            if (result is null) return Ok(new ValidateTokenResponseModel { IsValid = false });
            return Ok(new ValidateTokenResponseModel
            {
                IsValid = result.IsValid,
                UserId = result.UserId,
                Roles = result.Roles,
                ExpiresAt = result.ExpiresAt
            });
        }
        catch (Exception ex)
        {
            return Problem(ex.Message);
        }
    }

    [Authorize]
    [HttpGet("profile")]
    [SwaggerResponse(200, "Current authenticated user", typeof(CurrentUserResponseModel))]
    public IActionResult Profile()
    {
        return Ok(new CurrentUserResponseModel
        {
            UserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty,
            Email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
            FirstName = User.FindFirstValue(ClaimTypes.GivenName) ?? string.Empty,
            LastName = User.FindFirstValue(ClaimTypes.Surname) ?? string.Empty,
            Role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty,
            IsActive = true
        });
    }
}
