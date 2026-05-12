// Taskverse.Auth.Service/Services/IAuthenticationService.cs
using Taskverse.Auth.Service.Models;

namespace Taskverse.Auth.Service.Services;

public interface IAuthenticationService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request);
    Task<RefreshTokenResponse?> RefreshTokenAsync(string refreshToken);
    Task<bool> ValidateTokenAsync(string token);
    Task LogoutAsync(Guid userId);
}
