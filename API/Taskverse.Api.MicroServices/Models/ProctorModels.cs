using Newtonsoft.Json;

namespace Taskverse.Api.MicroServices.Models;

public record ProctorSessionModel(
    [property: JsonProperty("session_id")]
    Guid SessionId,
    [property: JsonProperty("attempt_id")]
    Guid AttemptId,
    [property: JsonProperty("assessment_id")]
    Guid AssessmentId,
    [property: JsonProperty("student_id")]
    Guid StudentId,
    [property: JsonProperty("status")]
    string Status,
    [property: JsonProperty("started_at")]
    DateTime? StartedAt,
    [property: JsonProperty("ended_at")]
    DateTime? EndedAt);

public record SessionHeartbeatModel(
    [property: JsonProperty("attempt_id")]
    Guid AttemptId,
    [property: JsonProperty("client_timestamp")]
    DateTime? ClientTimestamp,
    [property: JsonProperty("visibility_state")]
    string VisibilityState,
    [property: JsonProperty("is_fullscreen")]
    bool IsFullscreen,
    [property: JsonProperty("network_status")]
    string NetworkStatus,
    [property: JsonProperty("question_id")]
    Guid? QuestionId);

public record SessionHeartbeatResponseModel(
    [property: JsonProperty("session_id")]
    Guid SessionId,
    [property: JsonProperty("last_heartbeat_at")]
    DateTime LastHeartbeatAt,
    [property: JsonProperty("session_state")]
    ProctorSessionStateModel SessionState);

public record ProctorEventBatchItemModel(
    [property: JsonProperty("attempt_id")]
    Guid AttemptId,
    [property: JsonProperty("event_type")]
    string EventType,
    [property: JsonProperty("severity")]
    string Severity,
    [property: JsonProperty("client_timestamp")]
    DateTime? ClientTimestamp,
    [property: JsonProperty("question_id")]
    Guid? QuestionId,
    [property: JsonProperty("metadata_json")]
    string? MetadataJson);

public record ProctorEventBatchModel(
    [property: JsonProperty("events")]
    List<ProctorEventBatchItemModel> Events);

public record ProctorEventBatchFailureModel(
    [property: JsonProperty("index")]
    int Index,
    [property: JsonProperty("message")]
    string Message);

public record ProctorEventBatchResultModel(
    [property: JsonProperty("processed_count")]
    int ProcessedCount,
    [property: JsonProperty("failures")]
    List<ProctorEventBatchFailureModel> Failures,
    [property: JsonProperty("session_state")]
    ProctorSessionStateModel SessionState);

public record EndProctorSessionModel(
    [property: JsonProperty("attempt_id")]
    Guid AttemptId,
    [property: JsonProperty("event_type")]
    string EventType,
    [property: JsonProperty("client_timestamp")]
    DateTime? ClientTimestamp,
    [property: JsonProperty("severity")]
    string Severity,
    [property: JsonProperty("metadata_json")]
    string? MetadataJson);

public record ProctorSessionSummaryModel(
    [property: JsonProperty("tab_switch_count")]
    int TabSwitchCount,
    [property: JsonProperty("full_screen_exit_count")]
    int FullScreenExitCount,
    [property: JsonProperty("copy_attempt_count")]
    int CopyAttemptCount,
    [property: JsonProperty("paste_attempt_count")]
    int PasteAttemptCount,
    [property: JsonProperty("cut_attempt_count")]
    int CutAttemptCount,
    [property: JsonProperty("context_menu_attempt_count")]
    int ContextMenuAttemptCount,
    [property: JsonProperty("blocked_shortcut_count")]
    int BlockedShortcutCount,
    [property: JsonProperty("possible_devtools_count")]
    int PossibleDevtoolsCount,
    [property: JsonProperty("network_disconnect_count")]
    int NetworkDisconnectCount,
    [property: JsonProperty("risk_score")]
    int RiskScore,
    [property: JsonProperty("risk_level")]
    string RiskLevel,
    [property: JsonProperty("last_event_at")]
    DateTime? LastEventAt);

public record ProctorSessionRuleModel(
    [property: JsonProperty("event_type")]
    string EventType,
    [property: JsonProperty("display_name")]
    string DisplayName,
    [property: JsonProperty("warning_message")]
    string WarningMessage,
    [property: JsonProperty("current_count")]
    int CurrentCount,
    [property: JsonProperty("max_allowed_count")]
    int? MaxAllowedCount,
    [property: JsonProperty("remaining_count")]
    int? RemainingCount,
    [property: JsonProperty("is_enabled")]
    bool IsEnabled,
    [property: JsonProperty("lock_attempt_on_limit_exceeded")]
    bool LockAttemptOnLimitExceeded,
    [property: JsonProperty("auto_submit_on_limit_exceeded")]
    bool AutoSubmitOnLimitExceeded,
    [property: JsonProperty("is_threshold_exceeded")]
    bool IsThresholdExceeded);

public record ProctorSessionEnforcementModel(
    [property: JsonProperty("action")]
    string Action,
    [property: JsonProperty("triggered_by_event_type")]
    string? TriggeredByEventType,
    [property: JsonProperty("message")]
    string? Message);

public record ProctorSessionStateModel(
    [property: JsonProperty("session_id")]
    Guid SessionId,
    [property: JsonProperty("attempt_id")]
    Guid AttemptId,
    [property: JsonProperty("assessment_id")]
    Guid AssessmentId,
    [property: JsonProperty("student_id")]
    Guid StudentId,
    [property: JsonProperty("status")]
    string Status,
    [property: JsonProperty("started_at")]
    DateTime? StartedAt,
    [property: JsonProperty("ended_at")]
    DateTime? EndedAt,
    [property: JsonProperty("browser_name")]
    string? BrowserName,
    [property: JsonProperty("browser_version")]
    string? BrowserVersion,
    [property: JsonProperty("operating_system")]
    string? OperatingSystem,
    [property: JsonProperty("device_type")]
    string? DeviceType,
    [property: JsonProperty("user_agent")]
    string? UserAgent,
    [property: JsonProperty("ip_address")]
    string? IpAddress,
    [property: JsonProperty("summary")]
    ProctorSessionSummaryModel Summary,
    [property: JsonProperty("rules")]
    List<ProctorSessionRuleModel> Rules,
    [property: JsonProperty("enforcement")]
    ProctorSessionEnforcementModel Enforcement);

public record StartProctorSessionModel(
    [property: JsonProperty("attempt_id")]
    Guid AttemptId,
    [property: JsonProperty("assessment_id")]
    Guid AssessmentId,
    [property: JsonProperty("student_id")]
    Guid? StudentId,
    [property: JsonProperty("started_at")]
    DateTime? StartedAt,
    [property: JsonProperty("browser_name")]
    string? BrowserName,
    [property: JsonProperty("browser_version")]
    string? BrowserVersion,
    [property: JsonProperty("operating_system")]
    string? OperatingSystem,
    [property: JsonProperty("device_type")]
    string? DeviceType,
    [property: JsonProperty("user_agent")]
    string? UserAgent,
    [property: JsonProperty("ip_address")]
    string? IpAddress);

public record ProctorEventModel(
    string SessionId,
    string EventType,
    string? Payload,
    DateTime OccurredAt);

public record ProctorSummaryModel(
    string SessionId,
    int TotalFlags,
    int HighSeverityFlags,
    bool IsApproved,
    string? ReviewedBy);
