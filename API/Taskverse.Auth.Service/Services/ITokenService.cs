// Taskverse.API.Auth.Service/Services/ITokenService.cs
using System.Security.Claims;

namespace Taskverse.API.Auth.Service.Services;

public interface ITokenService
{
    /// <summary>
    /// Generates a JWT token for the given user claims
    /// </summary>
    Task<string> GenerateTokenAsync(
        Guid userId,
        string email,
        string role,
        string firstName,
        string lastName,
        Guid? collegeId = null,
        string? collegeName = null);

    /// <summary>
    /// Validates a JWT token and returns the principal if valid
    /// </summary>
    Task<ClaimsPrincipal?> ValidateTokenAsync(string token);

    /// <summary>
    /// Generates a refresh token
    /// </summary>
    Task<string> GenerateRefreshTokenAsync();

    DateTime GetExpiryUtc();

    /// <summary>
    /// Validates a refresh token
    /// </summary>
    Task<bool> ValidateRefreshTokenAsync(string refreshToken, Guid userId);
}
