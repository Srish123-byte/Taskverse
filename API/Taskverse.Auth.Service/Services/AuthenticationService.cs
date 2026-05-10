// Taskverse.Auth.Service/Services/AuthenticationService.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Taskverse.Auth.Service.Models;
using Taskverse.Business.Enums;
using Taskverse.Data.DataAccess;

namespace Taskverse.Auth.Service.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly ITokenService _tokenService;
    private readonly TaskverseContext _context;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(
        ITokenService tokenService,
        TaskverseContext context,
        ILogger<AuthenticationService> logger)
    {
        _tokenService = tokenService;
        _context = context;
        _logger = logger;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                _logger.LogWarning("Login attempt with empty credentials");
                return null;
            }

            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);
            if (user is null || !CanLogin(user))
            {
                _logger.LogWarning("Blocked login attempt for {Email}", normalizedEmail);
                return null;
            }

            var passwordHasher = new PasswordHasher<User>();
            var verificationResult = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
            if (verificationResult == PasswordVerificationResult.Failed)
            {
                _logger.LogWarning("Invalid password for {Email}", normalizedEmail);
                return null;
            }

            var (firstName, lastName) = SplitName(user.FullName);
            var token = await _tokenService.GenerateTokenAsync(user.Id, user.Email, user.Role, firstName, lastName);
            var refreshToken = await _tokenService.GenerateRefreshTokenAsync();

            _logger.LogInformation($"User logged in: {request.Email}");

            return new LoginResponse
            {
                AccessToken = token,
                RefreshToken = refreshToken,
                ExpiresAt = _tokenService.GetExpiryUtc(),
                UserId = user.Id.ToString(),
                Email = user.Email,
                FirstName = firstName,
                LastName = lastName,
                Roles = [user.Role]
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
            var newAccessToken = await _tokenService.GenerateTokenAsync(Guid.NewGuid(), "user@example.com", "Student", "Taskverse", "User");

            return new RefreshTokenResponse
            {
                AccessToken = newAccessToken,
                ExpiresAt = _tokenService.GetExpiryUtc()
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

    private static bool CanLogin(User user)
    {
        if (string.Equals(user.Role, "SuperAdmin", StringComparison.OrdinalIgnoreCase))
        {
            return user.Status != UserStatus.REJECTED;
        }

        return user.Status == UserStatus.APPROVED;
    }

    private static (string FirstName, string LastName) SplitName(string fullName)
    {
        var parts = (fullName ?? string.Empty)
            .Trim()
            .Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);

        return parts.Length switch
        {
            0 => (string.Empty, string.Empty),
            1 => (parts[0], string.Empty),
            _ => (parts[0], parts[1])
        };
    }
}
