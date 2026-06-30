using System;
using System.ComponentModel.DataAnnotations;

namespace Taskverse.Api.Models;

public class ReportEmailRequest
{
    [Required]
    public string TargetEmail { get; set; } = string.Empty;

    public List<string> TargetEmails { get; set; } = [];

    [Required]
    public string ReportType { get; set; } = string.Empty; // "college", "branch", "student"

    [Required]
    public Guid EntityId { get; set; }

    public string Format { get; set; } = "pdf"; // "pdf" or "excel"
}

public class ReportContextResponse
{
    public ReportContextTotalsResponse Totals { get; set; } = new();
    public List<ReportClassResponse> Classes { get; set; } = [];
}

public class ReportContextTotalsResponse
{
    public int TotalClasses { get; set; }
    public int TotalBatches { get; set; }
    public int TotalStudents { get; set; }
    public decimal AveragePercentage { get; set; }
    public decimal PassRate { get; set; }
}

public class ReportClassResponse
{
    public Guid ClassId { get; set; }
    public Guid CollegeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<ReportBatchResponse> Batches { get; set; } = [];
}

public class ReportBatchResponse
{
    public Guid BatchId { get; set; }
    public Guid ClassId { get; set; }
    public Guid CollegeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int StudentCount { get; set; }
    public List<ReportStudentResponse> Students { get; set; } = [];
}

public class ReportStudentResponse
{
    public Guid StudentId { get; set; }
    public Guid UserId { get; set; }
    public Guid CollegeId { get; set; }
    public Guid? ClassId { get; set; }
    public Guid? BatchId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? EnrollmentNumber { get; set; }
    public decimal AveragePercentage { get; set; }
    public int AssessmentCount { get; set; }
}
