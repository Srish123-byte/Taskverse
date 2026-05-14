// Taskverse.API.Auth.Service/Models/AuthModels.cs
namespace Taskverse.API.Auth.Service.Models;

public class LoginRequest
{
    public string Email { get; set; } = default!;
    public string Password { get; set; } = default!;
}

public class LoginResponse
{
    public string AccessToken { get; set; } = default!;
    public string RefreshToken { get; set; } = default!;
    public DateTime ExpiresAt { get; set; }
    public string UserId { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public List<string> Roles { get; set; } = [];
    public string Status { get; set; }
}

public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = default!;
}

public class RefreshTokenResponse
{
    public string AccessToken { get; set; } = default!;
    public string RefreshToken { get; set; } = default!;
    public DateTime ExpiresAt { get; set; }
}

public class ValidateTokenRequest
{
    public string Token { get; set; } = default!;
}

public class ValidateTokenResponse
{
    public bool IsValid { get; set; }
    public string? UserId { get; set; }
    public List<string>? Roles { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? Message { get; set; }
}

public class LogoutRequest
{
    public string? UserId { get; set; }
    public string? RefreshToken { get; set; }
}
