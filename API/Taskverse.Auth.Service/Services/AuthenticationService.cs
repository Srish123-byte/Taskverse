// Taskverse.Auth.Service/Services/AuthenticationService.cs
using Microsoft.AspNetCore.Identity;
using Taskverse.Auth.Service.Models;

namespace Taskverse.Auth.Service.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthenticationService> _logger;
    // TODO: Inject actual user service/repository
    // private readonly IUserService _userService;

    public AuthenticationService(
        ITokenService tokenService,
        ILogger<AuthenticationService> logger)
    {
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        try
        {
            // TODO: Validate credentials against user service
            // For now, return mock response

            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                _logger.LogWarning("Login attempt with empty credentials");
                return null;
            }

            // This should call Users microservice to validate credentials
            // For demonstration, we'll generate a token
            var userId = Guid.NewGuid(); // In reality, get from user service
            var token = await _tokenService.GenerateTokenAsync(userId, request.Email, "Student");
            var refreshToken = await _tokenService.GenerateRefreshTokenAsync();

            _logger.LogInformation($"User logged in: {request.Email}");

            return new LoginResponse
            {
                AccessToken = token,
                RefreshToken = refreshToken,
                ExpiresIn = 3600,
                TokenType = "Bearer"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"Login error: {ex.Message}");
            throw;
        }
    }

    public async Task<RefreshTokenResponse?> RefreshTokenAsync(string refreshToken)
    {
        try
        {
            var isValid = await _tokenService.ValidateRefreshTokenAsync(refreshToken, Guid.Empty);
            if (!isValid)
            {
                _logger.LogWarning("Invalid refresh token");
                return null;
            }

            // TODO: Get user from refresh token and generate new access token
            var newAccessToken = await _tokenService.GenerateTokenAsync(Guid.NewGuid(), "user@example.com", "Student");

            return new RefreshTokenResponse
            {
                AccessToken = newAccessToken,
                ExpiresIn = 3600,
                TokenType = "Bearer"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"Refresh token error: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> ValidateTokenAsync(string token)
    {
        try
        {
            var principal = await _tokenService.ValidateTokenAsync(token);
            return principal != null;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Token validation error: {ex.Message}");
            return false;
        }
    }

    public async Task LogoutAsync(Guid userId)
    {
        try
        {
            // TODO: Invalidate refresh tokens for this user
            _logger.LogInformation($"User logged out: {userId}");
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Logout error: {ex.Message}");
            throw;
        }
    }
}
