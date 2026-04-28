namespace Taskverse.Business.DTOs;

public class AssessmentDto
{
    public string AssessmentId { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public string Type { get; set; } = default!;
    public string? ExamId { get; set; }
    public List<string>? ChallengeIds { get; set; }
    public List<string> AssignedTo { get; set; } = [];
    public DateTime? DueDate { get; set; }
    public bool IsActive { get; set; }
    public string CreatedBy { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
}

public class CreateAssessmentDto
{
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public string Type { get; set; } = default!;
    public string? ExamId { get; set; }
    public List<string>? ChallengeIds { get; set; }
    public List<string> AssignedTo { get; set; } = [];
    public DateTime? DueDate { get; set; }
    public string CreatedBy { get; set; } = default!;
}

public class AssessmentResultDto
{
    public string ResultId { get; set; } = default!;
    public string AssessmentId { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public string Status { get; set; } = default!;
    public int? Score { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class AssessmentSummaryDto
{
    public string AssessmentId { get; set; } = default!;
    public string Title { get; set; } = default!;
    public int TotalAssigned { get; set; }
    public int TotalCompleted { get; set; }
    public double? AverageScore { get; set; }
}
