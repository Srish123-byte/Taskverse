namespace Taskverse.Business.DTOs;

public class ReportDto
{
    public string ReportId { get; set; } = default!;
    public string Type { get; set; } = default!;
    public string GeneratedFor { get; set; } = default!;
    public DateTime GeneratedAt { get; set; }
    public string Status { get; set; } = default!;
    public string? DownloadUrl { get; set; }
}

public class GenerateReportDto
{
    public string Type { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public string? AssessmentId { get; set; }
    public string? ExamId { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
}

public class UserPerformanceReportDto
{
    public string UserId { get; set; } = default!;
    public int TotalAssessments { get; set; }
    public int Completed { get; set; }
    public double AverageScore { get; set; }
    public int HighestScore { get; set; }
    public int LowestScore { get; set; }
    public DateTime ReportGeneratedAt { get; set; }
}

public class AssessmentReportDto
{
    public string AssessmentId { get; set; } = default!;
    public string Title { get; set; } = default!;
    public int TotalParticipants { get; set; }
    public double AverageScore { get; set; }
    public double PassRate { get; set; }
    public DateTime ReportGeneratedAt { get; set; }
}

public class StudentResultDto
{
    public Guid ResultId { get; set; }
    public Guid AssessmentId { get; set; }
    public string AssessmentName { get; set; } = default!;
    public Guid AttemptId { get; set; }
    public Guid StudentId { get; set; }
    public decimal TotalMarks { get; set; }
    public decimal ObtainedMarks { get; set; }
    public decimal Percentage { get; set; }
    public int Rank { get; set; }
    public string ResultStatus { get; set; } = default!;
    public DateTime GeneratedAt { get; set; }
    public bool HasPendingCodingEvaluation { get; set; }
}
