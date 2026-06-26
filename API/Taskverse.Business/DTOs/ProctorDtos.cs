namespace Taskverse.Business.DTOs;

public record ProctorSessionDto(
    string SessionId,
    string ExamId,
    string UserId,
    string Status,
    DateTime? StartedAt,
    DateTime? EndedAt,
    int TotalFlags);

public record StartProctorSessionDto(string ExamId, string UserId);

public record ProctorEventDto(
    string SessionId,
    string EventType,
    string? Payload);

public record ProctorSummaryDto(
    string SessionId,
    int TotalFlags,
    int HighSeverityFlags,
    bool IsApproved,
    string? ReviewedBy);
