using System.ComponentModel.DataAnnotations;

namespace Taskverse.Api.Models;

public class StartProctorSessionRequestModel
{
    [Required] public string ExamId { get; set; } = string.Empty;
    [Required] public string UserId { get; set; } = string.Empty;
}

public class ProctorSessionResponseModel
{
    public string SessionId { get; set; } = string.Empty;
    public string ExamId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public int TotalFlags { get; set; }
}

public class ProctorEventRequestModel
{
    [Required] public string SessionId { get; set; } = string.Empty;
    [Required] public string EventType { get; set; } = string.Empty;
    public string? Payload { get; set; }
}

public class ProctorSummaryResponseModel
{
    public string SessionId { get; set; } = string.Empty;
    public int TotalFlags { get; set; }
    public int HighSeverityFlags { get; set; }
    public bool IsApproved { get; set; }
    public string? ReviewedBy { get; set; }
}
