namespace Taskverse.Api.MicroServices.Models;

public record CodeSubmissionModel(
    string SubmissionId,
    string UserId,
    string ChallengeId,
    string Language,
    string Code,
    DateTime SubmittedAt);

public record CodeExecutionRequestModel(
    string ChallengeId,
    string UserId,
    string Language,
    string Code);

public record CodeExecutionResultModel(
    string SubmissionId,
    string Status,
    string? Output,
    string? ErrorOutput,
    long ExecutionTimeMs,
    long MemoryUsedKb,
    int TestCasesPassed,
    int TotalTestCases,
    int Score);

public record ChallengeModel(
    string ChallengeId,
    string Title,
    string Description,
    string Difficulty,
    List<string> Languages,
    int TimeLimit,
    int MemoryLimit,
    bool IsActive);

public record TestCaseModel(
    string TestCaseId,
    string ChallengeId,
    string Input,
    string ExpectedOutput,
    bool IsHidden);
