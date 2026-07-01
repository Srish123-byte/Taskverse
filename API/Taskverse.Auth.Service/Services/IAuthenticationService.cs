// Taskverse.API.Auth.Service/Services/IAuthenticationService.cs
using Taskverse.API.Auth.Service.Models;

namespace Taskverse.API.Auth.Service.Services;

public interface IAuthenticationService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request);
    Task<RefreshTokenResponse?> RefreshTokenAsync(string refreshToken, string? accessToken = null, bool forceRotate = false);
    Task<bool> ValidateTokenAsync(string token);
    Task LogoutAsync(Guid userId, string? refreshToken = null);
    Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request);
}
