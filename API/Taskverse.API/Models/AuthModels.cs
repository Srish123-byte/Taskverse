using System.ComponentModel.DataAnnotations;

namespace Taskverse.Api.Models;

public class LoginRequestModel
{
    [Required] public string Email { get; set; } = string.Empty;
    [Required] public string Password { get; set; } = string.Empty;
}

public class LoginResponseModel
{
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public CurrentUserResponseModel User { get; set; } = new();
}

public class CurrentUserResponseModel
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? CollegeName { get; set; }
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class RefreshLoginResponseModel
{
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
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
