// Taskverse.API.Auth.Service/Services/AuthenticationService.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using Taskverse.API.Auth.Service.Models;
using Taskverse.Data.Enums;
using Taskverse.Data.DataAccess;

namespace Taskverse.API.Auth.Service.Services;

public class AuthenticationService : IAuthenticationService
{
    private static readonly TimeSpan InactivityTimeout = TimeSpan.FromMinutes(30);
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
            var authSession = new AuthSession
            {
                UserId = user.Id,
                LastActivityAt = DateTime.UtcNow
            };
            var refreshToken = await _tokenService.GenerateRefreshTokenAsync();
            authSession.RefreshTokenHash = _tokenService.HashRefreshToken(refreshToken);
            _context.AuthSessions.Add(authSession);
            await _context.SaveChangesAsync();

            var token = await _tokenService.GenerateTokenAsync(
                user.Id,
                user.Email,
                user.Role,
                firstName,
                lastName,
                authSession.AuthSessionId,
                user.CollegeId,
                user.CollegeName);

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

    public async Task<RefreshTokenResponse?> RefreshTokenAsync(string refreshToken, string? accessToken = null, bool forceRotate = false)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return null;
            }

            var refreshTokenHash = _tokenService.HashRefreshToken(refreshToken);
            var authSession = await _context.AuthSessions
                .FirstOrDefaultAsync(session =>
                    session.RefreshTokenHash == refreshTokenHash &&
                    session.RevokedAt == null);
            if (authSession is null)
            {
                _logger.LogWarning("Refresh token not found or revoked");
                return null;
            }

            var now = DateTime.UtcNow;
            if (now - authSession.LastActivityAt > InactivityTimeout)
            {
                authSession.RevokedAt = now;
                authSession.ModifiedAt = now;
                await _context.SaveChangesAsync();
                _logger.LogWarning("Refresh token rejected due to inactivity timeout");
                return null;
            }

            var user = await _context.Users.FirstOrDefaultAsync(existingUser => existingUser.Id == authSession.UserId);
            if (user is null)
            {
                authSession.RevokedAt = now;
                authSession.ModifiedAt = now;
                await _context.SaveChangesAsync();
                return null;
            }

            var rotateSession = forceRotate || ShouldRotateAccessToken(accessToken);
            if (!rotateSession)
            {
                authSession.LastActivityAt = now;
                authSession.ModifiedAt = now;
                await _context.SaveChangesAsync();
                return null;
            }

            var (firstName, lastName) = SplitName(user.FullName);
            var newAccessToken = await _tokenService.GenerateTokenAsync(
                user.Id,
                user.Email,
                user.Role,
                firstName,
                lastName,
                authSession.AuthSessionId,
                user.CollegeId,
                user.CollegeName);
            var newRefreshToken = await _tokenService.GenerateRefreshTokenAsync();
            authSession.RefreshTokenHash = _tokenService.HashRefreshToken(newRefreshToken);
            authSession.LastActivityAt = now;
            authSession.ModifiedAt = now;
            await _context.SaveChangesAsync();

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

    public async Task LogoutAsync(Guid userId, string? refreshToken = null)
    {
        try
        {
            var sessions = _context.AuthSessions.Where(session => session.UserId == userId && session.RevokedAt == null);
            if (!string.IsNullOrWhiteSpace(refreshToken))
            {
                var refreshTokenHash = _tokenService.HashRefreshToken(refreshToken);
                sessions = sessions.Where(session => session.RefreshTokenHash == refreshTokenHash);
            }

            var activeSessions = await sessions.ToListAsync();
            var now = DateTime.UtcNow;
            foreach (var session in activeSessions)
            {
                session.RevokedAt = now;
                session.ModifiedAt = now;
            }

            if (activeSessions.Count > 0)
            {
                await _context.SaveChangesAsync();
            }

            _logger.LogInformation($"User logged out: {userId}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Logout error: {ex.Message}");
            throw;
        }
    }

    private bool ShouldRotateAccessToken(string? accessToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return true;
        }

        var tokenHandler = new JwtSecurityTokenHandler();
        if (!tokenHandler.CanReadToken(accessToken))
        {
            return true;
        }

        var jwtToken = tokenHandler.ReadJwtToken(accessToken);
        var expirationUnix = jwtToken.Claims.FirstOrDefault(claim => claim.Type == JwtRegisteredClaimNames.Exp)?.Value;
        if (!long.TryParse(expirationUnix, out var expiresAtUnix))
        {
            return true;
        }

        var expiresAt = DateTimeOffset.FromUnixTimeSeconds(expiresAtUnix).UtcDateTime;
        return expiresAt <= DateTime.UtcNow.AddMinutes(3);
    }

    private static string? GetLoginBlockMessage(User user)
    {
        var normalizedRole = (user.Role ?? string.Empty).Trim().Replace(" ", string.Empty).ToLowerInvariant();

        if (user.Status == UserStatus.REJECTED)
        {
            return "Your account is not allowed to sign in.";
        }

        if (user.Status == UserStatus.PENDING_APPROVAL)
        {
            return normalizedRole switch
            {
                "collegeadmin" => "Your account is awaiting approval from the super administrator.",
                "trainer" or "student" => "Your account is awaiting approval from your college administrator.",
                _ => "Your account is awaiting approval."
            };
        }

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
