namespace Taskverse.Api.MicroServices.Models;

public record LoginRequestModel(string Email, string Password);

public record LoginResponseModel(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    string UserId,
    List<string> Roles);

public record RefreshTokenRequestModel(string RefreshToken);

public record LogoutRequestModel(string UserId, string RefreshToken);

public record ValidateTokenRequestModel(string Token);

public record ValidateTokenResponseModel(
    bool IsValid,
    string? UserId,
    List<string>? Roles,
    DateTime? ExpiresAt);
