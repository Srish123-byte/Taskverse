using System.ComponentModel.DataAnnotations;

namespace Taskverse.Api.Models;

public class AssessmentResponseModel
{
    public string AssessmentId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? ExamId { get; set; }
    public List<string>? ChallengeIds { get; set; }
    public List<string> AssignedTo { get; set; } = new();
    public DateTime? DueDate { get; set; }
    public bool IsActive { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CreateAssessmentRequestModel
{
    [Required] public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    [Required] public string Type { get; set; } = string.Empty;
    public string? ExamId { get; set; }
    public List<string>? ChallengeIds { get; set; }
    [Required] public List<string> AssignedTo { get; set; } = new();
    public DateTime? DueDate { get; set; }
    [Required] public string CreatedBy { get; set; } = string.Empty;
}

public class AssessmentResultResponseModel
{
    public string ResultId { get; set; } = string.Empty;
    public string AssessmentId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int? Score { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class AssessmentSummaryResponseModel
{
    public string AssessmentId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int TotalAssigned { get; set; }
    public int TotalCompleted { get; set; }
    public double? AverageScore { get; set; }
}
