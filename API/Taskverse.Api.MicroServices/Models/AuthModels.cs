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
    string CollegeId,
    string CollegeName,
    List<string> Roles,
    string Status,
    bool MustChangePassword);

public record RefreshTokenResponseModel(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt);

public record RefreshTokenRequestModel(string RefreshToken, string? AccessToken = null, bool ForceRotate = false);

public record LogoutRequestModel(string UserId, string RefreshToken);

public record ValidateTokenRequestModel(string Token);

public record ValidateTokenResponseModel(
    bool IsValid,
    string? UserId,
    List<string>? Roles,
    DateTime? ExpiresAt);

public record ChangePasswordRequestModel(
    string UserId,
    string CurrentPassword,
    string NewPassword,
    bool IsTemporaryPasswordChange);
