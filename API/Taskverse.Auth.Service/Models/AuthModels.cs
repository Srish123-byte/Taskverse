// Taskverse.Auth.Service/Models/AuthModels.cs
namespace Taskverse.Auth.Service.Models;

public class LoginRequest
{
    public string Email { get; set; } = default!;
    public string Password { get; set; } = default!;
}

public class LoginResponse
{
    public string AccessToken { get; set; } = default!;
    public string RefreshToken { get; set; } = default!;
    public int ExpiresIn { get; set; }
    public string TokenType { get; set; } = "Bearer";
}

public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = default!;
}

public class RefreshTokenResponse
{
    public string AccessToken { get; set; } = default!;
    public int ExpiresIn { get; set; }
    public string TokenType { get; set; } = "Bearer";
}

public class ValidateTokenRequest
{
    public string Token { get; set; } = default!;
}

public class ValidateTokenResponse
{
    public bool IsValid { get; set; }
    public string? Message { get; set; }
    public Dictionary<string, object>? Claims { get; set; }
}

public class LogoutRequest
{
    public string? RefreshToken { get; set; }
}
