// Taskverse.Auth.Service/Services/TokenService.cs
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Taskverse.Auth.Service.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<TokenService> _logger;

    public TokenService(IConfiguration configuration, ILogger<TokenService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string> GenerateTokenAsync(Guid userId, string email, string role, string firstName, string lastName)
    {
        try
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secret = jwtSettings["Key"] ?? jwtSettings["Secret"] ?? throw new InvalidOperationException("JWT secret not configured");
            var key = Encoding.ASCII.GetBytes(secret);

            var tokenHandler = new JwtSecurityTokenHandler();
            var expirationMinutes = ResolveExpiryMinutes(jwtSettings);
            var expiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                    new Claim(ClaimTypes.Email, email),
                    new Claim(ClaimTypes.Role, role),
                    new Claim(ClaimTypes.GivenName, firstName),
                    new Claim(ClaimTypes.Surname, lastName),
                    new Claim("service", "taskverse-api")
                }),
                Expires = expiresAt,
                Issuer = jwtSettings["Issuer"],
                Audience = jwtSettings["Audience"],
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key), 
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            _logger.LogInformation($"Token generated for user: {userId}");
            return await Task.FromResult(tokenString);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error generating token: {ex.Message}");
            throw;
        }
    }

    public DateTime GetExpiryUtc()
    {
        var expirationMinutes = ResolveExpiryMinutes(_configuration.GetSection("JwtSettings"));
        return DateTime.UtcNow.AddMinutes(expirationMinutes);
    }

    public async Task<ClaimsPrincipal?> ValidateTokenAsync(string token)
    {
        try
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secret = jwtSettings["Key"] ?? jwtSettings["Secret"] ?? throw new InvalidOperationException("JWT secret not configured");
            var key = Encoding.ASCII.GetBytes(secret);

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidateAudience = true,
                ValidAudience = jwtSettings["Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            return await Task.FromResult(principal);
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Token validation failed: {ex.Message}");
            return null;
        }
    }

    public async Task<string> GenerateRefreshTokenAsync()
    {
        var randomNumber = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomNumber);
            return await Task.FromResult(Convert.ToBase64String(randomNumber));
        }
    }

    public async Task<bool> ValidateRefreshTokenAsync(string refreshToken, Guid userId)
    {
        // TODO: Implement refresh token validation from database
        // For now, just return true to indicate format is valid
        return await Task.FromResult(!string.IsNullOrWhiteSpace(refreshToken));
    }

    private static int ResolveExpiryMinutes(IConfigurationSection jwtSettings)
    {
        if (int.TryParse(jwtSettings["ExpiresInMinutes"], out var expiresInMinutes))
        {
            return expiresInMinutes;
        }

        if (int.TryParse(jwtSettings["TokenExpiryTimeInMinutes"], out var tokenExpiryMinutes))
        {
            return tokenExpiryMinutes;
        }

        return 60;
    }
}
