namespace Taskverse.Business.Interface;

public class StartProctorSessionDto
{
    public Guid AttemptId { get; set; }
    public Guid AssessmentId { get; set; }
    public Guid? StudentId { get; set; }
    public DateTime? StartedAt { get; set; }
    public string? BrowserName { get; set; }
    public string? BrowserVersion { get; set; }
    public string? OperatingSystem { get; set; }
    public string? DeviceType { get; set; }
    public string? UserAgent { get; set; }
    public string? IpAddress { get; set; }
}

public class ProctorSessionDto
{
    public Guid SessionId { get; set; }
    public Guid AttemptId { get; set; }
    public Guid AssessmentId { get; set; }
    public Guid StudentId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
}

public class SessionHeartbeatDto
{
    public Guid AttemptId { get; set; }
    public DateTime? ClientTimestamp { get; set; }
    public string VisibilityState { get; set; } = string.Empty;
    public bool IsFullscreen { get; set; }
    public string NetworkStatus { get; set; } = string.Empty;
    public Guid? QuestionId { get; set; }
}

public class SessionHeartbeatResponseDto
{
    public Guid SessionId { get; set; }
    public DateTime LastHeartbeatAt { get; set; }
    public ProctorSessionStateDto SessionState { get; set; } = new();
}

public class EndProctorSessionDto
{
    public Guid AttemptId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTime? ClientTimestamp { get; set; }
    public string Severity { get; set; } = string.Empty;
    public string? MetadataJson { get; set; }
}

public class ProctorEventBatchItemDto
{
    public Guid AttemptId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public DateTime? ClientTimestamp { get; set; }
    public Guid? QuestionId { get; set; }
    public string? MetadataJson { get; set; }
}

public class ProctorEventBatchDto
{
    public List<ProctorEventBatchItemDto> Events { get; set; } = [];
}

public class ProctorEventBatchFailureDto
{
    public int Index { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class ProctorEventBatchResultDto
{
    public int ProcessedCount { get; set; }
    public List<ProctorEventBatchFailureDto> Failures { get; set; } = [];
    public ProctorSessionStateDto SessionState { get; set; } = new();
}

public class ProctorSummaryDto
{
    public string SessionId { get; set; } = default!;
    public int TotalFlags { get; set; }
    public int HighSeverityFlags { get; set; }
    public bool IsApproved { get; set; }
    public string? ReviewedBy { get; set; }
}

public class ProctorSessionStateDto
{
    public Guid SessionId { get; set; }
    public Guid AttemptId { get; set; }
    public Guid AssessmentId { get; set; }
    public Guid StudentId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public string? BrowserName { get; set; }
    public string? BrowserVersion { get; set; }
    public string? OperatingSystem { get; set; }
    public string? DeviceType { get; set; }
    public string? UserAgent { get; set; }
    public string? IpAddress { get; set; }
    public ProctorSessionSummaryDto Summary { get; set; } = new();
    public List<ProctorSessionRuleDto> Rules { get; set; } = [];
    public ProctorSessionEnforcementDto Enforcement { get; set; } = new();
}

public class ProctorSessionSummaryDto
{
    public int TabSwitchCount { get; set; }
    public int FullScreenExitCount { get; set; }
    public int CopyAttemptCount { get; set; }
    public int PasteAttemptCount { get; set; }
    public int CutAttemptCount { get; set; }
    public int ContextMenuAttemptCount { get; set; }
    public int BlockedShortcutCount { get; set; }
    public int PossibleDevtoolsCount { get; set; }
    public int NetworkDisconnectCount { get; set; }
    public int RiskScore { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
    public DateTime? LastEventAt { get; set; }
}

public class ProctorSessionRuleDto
{
    public string EventType { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string WarningMessage { get; set; } = string.Empty;
    public int CurrentCount { get; set; }
    public int? MaxAllowedCount { get; set; }
    public int? RemainingCount { get; set; }
    public bool IsEnabled { get; set; }
    public bool LockAttemptOnLimitExceeded { get; set; }
    public bool AutoSubmitOnLimitExceeded { get; set; }
    public bool IsThresholdExceeded { get; set; }
}

public class ProctorSessionEnforcementDto
{
    public string Action { get; set; } = "NONE";
    public string? TriggeredByEventType { get; set; }
    public string? Message { get; set; }
}

public interface IProctorOrchestrator
{
    Task<ProctorSessionDto> StartSession(StartProctorSessionDto dto, Guid studentUserId);
    Task<SessionHeartbeatResponseDto> HeartbeatSession(Guid sessionId, SessionHeartbeatDto dto, Guid studentUserId);
    Task<ProctorEventBatchResultDto> RecordEvents(Guid sessionId, ProctorEventBatchDto dto, Guid studentUserId);
    Task<ProctorSessionDto> EndSession(Guid sessionId, EndProctorSessionDto dto, Guid studentUserId);
    Task<ProctorSessionStateDto> GetSessionByAttempt(Guid attemptId, Guid studentUserId);
    Task<ProctorSessionStateDto> GetSession(Guid sessionId, Guid studentUserId);
    Task<ProctorSessionStateDto> GetAttemptSession(Guid attemptId, Guid collegeId, string requesterRole, string requesterName);
    Task RecordEvent(string sessionId, string eventType, string? payload);
    Task<ProctorSummaryDto> GetSummary(string sessionId);
}
