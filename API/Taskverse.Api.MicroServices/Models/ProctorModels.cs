namespace Taskverse.Api.MicroServices.Models;

public record ProctorSessionModel(
    string SessionId,
    string ExamId,
    string UserId,
    string Status,
    DateTime? StartedAt,
    DateTime? EndedAt,
    List<ProctorFlagModel> Flags);

public record ProctorFlagModel(
    string FlagId,
    string Type,
    string Description,
    DateTime FlaggedAt,
    string Severity);

public record StartProctorSessionModel(string ExamId, string UserId);

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
