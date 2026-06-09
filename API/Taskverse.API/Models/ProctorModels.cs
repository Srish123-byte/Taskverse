using System.Text.Json;

namespace Taskverse.Api.Models;

public class StartProctorSessionRequestModel
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

public class ProctorSessionResponseModel
{
    public Guid SessionId { get; set; }
    public Guid AttemptId { get; set; }
    public Guid AssessmentId { get; set; }
    public Guid StudentId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
}

public class SessionHeartbeatRequestModel
{
    public Guid AttemptId { get; set; }
    public DateTime? ClientTimestamp { get; set; }
    public string VisibilityState { get; set; } = string.Empty;
    public bool IsFullscreen { get; set; }
    public string NetworkStatus { get; set; } = string.Empty;
    public Guid? QuestionId { get; set; }
}

public class SessionHeartbeatResponseModel
{
    public Guid SessionId { get; set; }
    public DateTime LastHeartbeatAt { get; set; }
}

public class EndProctorSessionRequestModel
{
    public Guid AttemptId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTime? ClientTimestamp { get; set; }
    public string Severity { get; set; } = string.Empty;
    public JsonElement? Metadata { get; set; }
}

public class ProctorEventBatchRequestItemModel
{
    public Guid AttemptId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public DateTime? ClientTimestamp { get; set; }
    public Guid? QuestionId { get; set; }
    public JsonElement? Metadata { get; set; }
}

public class ProctorEventBatchRequestModel
{
    public List<ProctorEventBatchRequestItemModel> Events { get; set; } = [];
}

public class ProctorEventBatchFailureResponseModel
{
    public int Index { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class ProctorEventBatchResponseModel
{
    public int ProcessedCount { get; set; }
    public List<ProctorEventBatchFailureResponseModel> Failures { get; set; } = [];
}

public class ProctorSessionSummaryResponseModel
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

public class ProctorSessionStateResponseModel
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
    public ProctorSessionSummaryResponseModel Summary { get; set; } = new();
}
