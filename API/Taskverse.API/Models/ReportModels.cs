using System.ComponentModel.DataAnnotations;

namespace Taskverse.Api.Models;

public class GenerateReportRequestModel
{
    [Required] public string Type { get; set; } = string.Empty;
    [Required] public string UserId { get; set; } = string.Empty;
    public string? AssessmentId { get; set; }
    public string? ExamId { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
}

public class ReportResponseModel
{
    public string ReportId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string GeneratedFor { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? DownloadUrl { get; set; }
}

public class UserPerformanceReportResponseModel
{
    public string UserId { get; set; } = string.Empty;
    public int TotalAssessments { get; set; }
    public int Completed { get; set; }
    public double AverageScore { get; set; }
    public int HighestScore { get; set; }
    public int LowestScore { get; set; }
    public DateTime ReportGeneratedAt { get; set; }
}

public class AssessmentReportResponseModel
{
    public string AssessmentId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int TotalParticipants { get; set; }
    public double AverageScore { get; set; }
    public double PassRate { get; set; }
    public DateTime ReportGeneratedAt { get; set; }
}
