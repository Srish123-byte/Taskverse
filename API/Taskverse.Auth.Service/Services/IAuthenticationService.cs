// Taskverse.API.Auth.Service/Services/IAuthenticationService.cs
using Taskverse.API.Auth.Service.Models;

namespace Taskverse.API.Auth.Service.Services;

public interface IAuthenticationService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request);
    Task<RefreshTokenResponse?> RefreshTokenAsync(string refreshToken);
    Task<bool> ValidateTokenAsync(string token);
    Task LogoutAsync(Guid userId);
}
