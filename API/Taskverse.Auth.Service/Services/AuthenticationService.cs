// Taskverse.API.Auth.Service/Services/AuthenticationService.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Taskverse.API.Auth.Service.Models;
using Taskverse.Business.Enums;
using Taskverse.Data.DataAccess;

namespace Taskverse.API.Auth.Service.Services;

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
            _logger.LogInformation($"[Login] Starting login for email: {request.Email}");

            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                _logger.LogWarning("Login attempt with empty credentials");
                return null;
            }

            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            _logger.LogInformation($"[Login] Querying user from database for email: {normalizedEmail}");
            
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);
            if (user is null)
            {
                _logger.LogWarning("Blocked login attempt for {Email}", normalizedEmail);
                return null;
            }

            _logger.LogInformation($"[Login] User found. Status: {user.Status}, Role: {user.Role}");

            var blockedMessage = GetLoginBlockMessage(user);
            if (!string.IsNullOrWhiteSpace(blockedMessage))
            {
                _logger.LogWarning("Blocked login attempt for {Email}: {Reason}", normalizedEmail, blockedMessage);
                throw new UnauthorizedAccessException(blockedMessage);
            }

            _logger.LogInformation($"[Login] Verifying password for user: {normalizedEmail}");
            var passwordHasher = new PasswordHasher<User>();
            var verificationResult = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
            if (verificationResult == PasswordVerificationResult.Failed)
            {
                _logger.LogWarning("Invalid password for {Email}", normalizedEmail);
                return null;
            }

            _logger.LogInformation($"[Login] Password verified. Generating tokens for user: {normalizedEmail}");
            var (firstName, lastName) = SplitName(user.FullName);
            var token = await _tokenService.GenerateTokenAsync(
                user.Id,
                user.Email,
                user.Role,
                firstName,
                lastName,
                user.CollegeId,
                user.CollegeName);
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
                CollegeId = user.CollegeId?.ToString(),
                CollegeName = user.CollegeName,
                Roles = [user.Role],
                Status = user.Status.ToString()
            };
        }
        catch (UnauthorizedAccessException)
        {
            throw;
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
            var newAccessToken = await _tokenService.GenerateTokenAsync(
                Guid.NewGuid(),
                "user@example.com",
                "Student",
                "Taskverse",
                "User");
            var newRefreshToken = await _tokenService.GenerateRefreshTokenAsync();

            return new RefreshTokenResponse
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
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

    private static string? GetLoginBlockMessage(User user)
    {
        // Only block SuperAdmin if rejected
        if (string.Equals(user.Role, "SuperAdmin", StringComparison.OrdinalIgnoreCase))
        {
            return user.Status == UserStatus.REJECTED ? "Your account is not allowed to sign in." : null;
        }

        // For other roles, allow login but frontend will check status and show message
        return null;
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
