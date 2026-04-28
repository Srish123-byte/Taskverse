using System.ComponentModel.DataAnnotations;

namespace Taskverse.Api.Models;

public class LoginRequestModel
{
    [Required] public string Email { get; set; } = string.Empty;
    [Required] public string Password { get; set; } = string.Empty;
}

public class LoginResponseModel
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string UserId { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
}

public class RefreshTokenRequestModel
{
    [Required] public string RefreshToken { get; set; } = string.Empty;
}

public class LogoutRequestModel
{
    [Required] public string UserId { get; set; } = string.Empty;
    [Required] public string RefreshToken { get; set; } = string.Empty;
}

public class ValidateTokenRequestModel
{
    [Required] public string Token { get; set; } = string.Empty;
}

public class ValidateTokenResponseModel
{
    public bool IsValid { get; set; }
    public string? UserId { get; set; }
    public List<string>? Roles { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
