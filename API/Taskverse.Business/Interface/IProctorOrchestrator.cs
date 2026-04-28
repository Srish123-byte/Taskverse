namespace Taskverse.Business.Interface;

public class ProctorSessionDto
{
    public string SessionId { get; set; } = default!;
    public string ExamId { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public string Status { get; set; } = default!;
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public int TotalFlags { get; set; }
}

public class ProctorSummaryDto
{
    public string SessionId { get; set; } = default!;
    public int TotalFlags { get; set; }
    public int HighSeverityFlags { get; set; }
    public bool IsApproved { get; set; }
    public string? ReviewedBy { get; set; }
}

public interface IProctorOrchestrator
{
    Task<ProctorSessionDto> StartSession(string examId, string userId);
    Task<ProctorSessionDto> GetSession(string sessionId);
    Task RecordEvent(string sessionId, string eventType, string? payload);
    Task EndSession(string sessionId);
    Task<ProctorSummaryDto> GetSummary(string sessionId);
}
