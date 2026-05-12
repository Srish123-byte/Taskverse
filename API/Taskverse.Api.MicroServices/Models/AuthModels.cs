namespace Taskverse.Api.MicroServices.Models;

public record LoginRequestModel(string Email, string Password);

public record LoginResponseModel(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    string UserId,
    string Email,
    string FirstName,
    string LastName,
    List<string> Roles,
    string Status);

public record RefreshTokenResponseModel(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt);

public record RefreshTokenRequestModel(string RefreshToken);

public record LogoutRequestModel(string UserId, string RefreshToken);

public record ValidateTokenRequestModel(string Token);

public record ValidateTokenResponseModel(
    bool IsValid,
    string? UserId,
    List<string>? Roles,
    DateTime? ExpiresAt);
