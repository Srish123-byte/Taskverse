namespace Taskverse.Business.DTOs;

public record LoginRequestDto(string Email, string Password);

public record LoginResponseDto(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    string UserId,
    string Email,
    string FirstName,
    string LastName,
    string? CollegeId,
    string? CollegeName,
    List<string> Roles,
    string Status);

public record RefreshLoginResponseDto(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt);

public record RefreshTokenRequestDto(string RefreshToken, string? AccessToken = null, bool ForceRotate = false);

public record LogoutRequestDto(string UserId, string RefreshToken);

public record ValidateTokenRequestDto(string Token);

public record ValidateTokenResponseDto(
    bool IsValid,
    string? UserId,
    List<string>? Roles,
    DateTime? ExpiresAt);

public record ChallengeDto(
    string ChallengeId,
    string Title,
    string Description,
    string Difficulty,
    List<string> Languages,
    int TimeLimit,
    int MemoryLimit,
    bool IsActive);

public record CodeExecutionResultDto(
    string SubmissionId,
    string Status,
    string? Output,
    string? ErrorOutput,
    long ExecutionTimeMs,
    long MemoryUsedKb,
    int TestCasesPassed,
    int TotalTestCases,
    int Score);
