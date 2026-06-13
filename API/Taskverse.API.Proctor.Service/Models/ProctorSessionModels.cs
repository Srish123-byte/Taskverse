using System.Text.Json.Serialization;

namespace Taskverse.API.Proctor.Service.Models;

public record StartProctorSessionRequest(
    [property: JsonPropertyName("attempt_id")]
    Guid AttemptId,
    [property: JsonPropertyName("assessment_id")]
    Guid AssessmentId,
    [property: JsonPropertyName("student_id")]
    Guid? StudentId,
    [property: JsonPropertyName("started_at")]
    DateTime? StartedAt,
    [property: JsonPropertyName("browser_name")]
    string? BrowserName,
    [property: JsonPropertyName("browser_version")]
    string? BrowserVersion,
    [property: JsonPropertyName("operating_system")]
    string? OperatingSystem,
    [property: JsonPropertyName("device_type")]
    string? DeviceType,
    [property: JsonPropertyName("user_agent")]
    string? UserAgent,
    [property: JsonPropertyName("ip_address")]
    string? IpAddress);

public record ProctorSessionRecord(
    [property: JsonPropertyName("session_id")]
    Guid SessionId,
    [property: JsonPropertyName("attempt_id")]
    Guid AttemptId,
    [property: JsonPropertyName("assessment_id")]
    Guid AssessmentId,
    [property: JsonPropertyName("student_id")]
    Guid StudentId,
    [property: JsonPropertyName("status")]
    string Status,
    [property: JsonPropertyName("started_at")]
    DateTime? StartedAt,
    [property: JsonPropertyName("ended_at")]
    DateTime? EndedAt);

public record SessionHeartbeatRequest(
    [property: JsonPropertyName("attempt_id")]
    Guid AttemptId,
    [property: JsonPropertyName("client_timestamp")]
    DateTime? ClientTimestamp,
    [property: JsonPropertyName("visibility_state")]
    string VisibilityState,
    [property: JsonPropertyName("is_fullscreen")]
    bool IsFullscreen,
    [property: JsonPropertyName("network_status")]
    string NetworkStatus,
    [property: JsonPropertyName("question_id")]
    Guid? QuestionId);

public record SessionHeartbeatResponseRecord(
    [property: JsonPropertyName("session_id")]
    Guid SessionId,
    [property: JsonPropertyName("last_heartbeat_at")]
    DateTime LastHeartbeatAt,
    [property: JsonPropertyName("session_state")]
    ProctorSessionStateRecord SessionState);

public record ProctorEventBatchItemRequest(
    [property: JsonPropertyName("attempt_id")]
    Guid AttemptId,
    [property: JsonPropertyName("event_type")]
    string EventType,
    [property: JsonPropertyName("severity")]
    string Severity,
    [property: JsonPropertyName("client_timestamp")]
    DateTime? ClientTimestamp,
    [property: JsonPropertyName("question_id")]
    Guid? QuestionId,
    [property: JsonPropertyName("metadata_json")]
    string? MetadataJson);

public record ProctorEventBatchRequest(
    [property: JsonPropertyName("events")]
    List<ProctorEventBatchItemRequest> Events);

public record ProctorEventBatchFailureRecord(
    [property: JsonPropertyName("index")]
    int Index,
    [property: JsonPropertyName("message")]
    string Message);

public record ProctorEventBatchResultRecord(
    [property: JsonPropertyName("processed_count")]
    int ProcessedCount,
    [property: JsonPropertyName("failures")]
    List<ProctorEventBatchFailureRecord> Failures,
    [property: JsonPropertyName("session_state")]
    ProctorSessionStateRecord SessionState);

public record EndProctorSessionRequest(
    [property: JsonPropertyName("attempt_id")]
    Guid AttemptId,
    [property: JsonPropertyName("event_type")]
    string EventType,
    [property: JsonPropertyName("client_timestamp")]
    DateTime? ClientTimestamp,
    [property: JsonPropertyName("severity")]
    string Severity,
    [property: JsonPropertyName("metadata_json")]
    string? MetadataJson);

public record ProctorSessionSummaryRecord(
    [property: JsonPropertyName("tab_switch_count")]
    int TabSwitchCount,
    [property: JsonPropertyName("full_screen_exit_count")]
    int FullScreenExitCount,
    [property: JsonPropertyName("copy_attempt_count")]
    int CopyAttemptCount,
    [property: JsonPropertyName("paste_attempt_count")]
    int PasteAttemptCount,
    [property: JsonPropertyName("cut_attempt_count")]
    int CutAttemptCount,
    [property: JsonPropertyName("context_menu_attempt_count")]
    int ContextMenuAttemptCount,
    [property: JsonPropertyName("blocked_shortcut_count")]
    int BlockedShortcutCount,
    [property: JsonPropertyName("possible_devtools_count")]
    int PossibleDevtoolsCount,
    [property: JsonPropertyName("network_disconnect_count")]
    int NetworkDisconnectCount,
    [property: JsonPropertyName("risk_score")]
    int RiskScore,
    [property: JsonPropertyName("risk_level")]
    string RiskLevel,
    [property: JsonPropertyName("last_event_at")]
    DateTime? LastEventAt);

public record ProctorSessionRuleRecord(
    [property: JsonPropertyName("event_type")]
    string EventType,
    [property: JsonPropertyName("display_name")]
    string DisplayName,
    [property: JsonPropertyName("warning_message")]
    string WarningMessage,
    [property: JsonPropertyName("current_count")]
    int CurrentCount,
    [property: JsonPropertyName("max_allowed_count")]
    int? MaxAllowedCount,
    [property: JsonPropertyName("remaining_count")]
    int? RemainingCount,
    [property: JsonPropertyName("is_enabled")]
    bool IsEnabled,
    [property: JsonPropertyName("lock_attempt_on_limit_exceeded")]
    bool LockAttemptOnLimitExceeded,
    [property: JsonPropertyName("auto_submit_on_limit_exceeded")]
    bool AutoSubmitOnLimitExceeded,
    [property: JsonPropertyName("is_threshold_exceeded")]
    bool IsThresholdExceeded);

public record ProctorSessionEnforcementRecord(
    [property: JsonPropertyName("action")]
    string Action,
    [property: JsonPropertyName("triggered_by_event_type")]
    string? TriggeredByEventType,
    [property: JsonPropertyName("message")]
    string? Message);

public record ProctorSessionStateRecord(
    [property: JsonPropertyName("session_id")]
    Guid SessionId,
    [property: JsonPropertyName("attempt_id")]
    Guid AttemptId,
    [property: JsonPropertyName("assessment_id")]
    Guid AssessmentId,
    [property: JsonPropertyName("student_id")]
    Guid StudentId,
    [property: JsonPropertyName("status")]
    string Status,
    [property: JsonPropertyName("started_at")]
    DateTime? StartedAt,
    [property: JsonPropertyName("ended_at")]
    DateTime? EndedAt,
    [property: JsonPropertyName("browser_name")]
    string? BrowserName,
    [property: JsonPropertyName("browser_version")]
    string? BrowserVersion,
    [property: JsonPropertyName("operating_system")]
    string? OperatingSystem,
    [property: JsonPropertyName("device_type")]
    string? DeviceType,
    [property: JsonPropertyName("user_agent")]
    string? UserAgent,
    [property: JsonPropertyName("ip_address")]
    string? IpAddress,
    [property: JsonPropertyName("summary")]
    ProctorSessionSummaryRecord Summary,
    [property: JsonPropertyName("rules")]
    List<ProctorSessionRuleRecord> Rules,
    [property: JsonPropertyName("enforcement")]
    ProctorSessionEnforcementRecord Enforcement);
