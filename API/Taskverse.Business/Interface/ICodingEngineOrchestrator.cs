namespace Taskverse.Business.Interface;

public class ChallengeDto
{
    public string ChallengeId { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string Difficulty { get; set; } = default!;
    public List<string> Languages { get; set; } = [];
    public int TimeLimit { get; set; }
    public int MemoryLimit { get; set; }
    public bool IsActive { get; set; }
}

public class CodeExecutionResultDto
{
    public string SubmissionId { get; set; } = default!;
    public string Status { get; set; } = default!;
    public string? Output { get; set; }
    public string? ErrorOutput { get; set; }
    public long ExecutionTimeMs { get; set; }
    public long MemoryUsedKb { get; set; }
    public int TestCasesPassed { get; set; }
    public int TotalTestCases { get; set; }
    public int Score { get; set; }
}

public interface ICodingEngineOrchestrator
{
    Task<ChallengeDto> GetChallenge(string challengeId);
    Task<CodeExecutionResultDto> ExecuteCode(string challengeId, string userId, string language, string code);
    Task<CodeExecutionResultDto> GetSubmission(string submissionId);
    Task<List<CodeExecutionResultDto>> GetSubmissionsByUser(string userId);
    Task<List<ChallengeDto>> GetChallengesByAssessment(string assessmentId);
}
