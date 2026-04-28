using Taskverse.Business.DTOs;

namespace Taskverse.Api.Tests.Helpers;

public static partial class MockData
{
    public static AssessmentDto GetAssessmentDto(string assessmentId = "assess-123") => new()
    {
        AssessmentId = assessmentId,
        Title = "Mid-Term Assessment",
        Description = "Covers chapters 1 through 5",
        Type = "Exam",
        ExamId = "exam-456",
        ChallengeIds = null,
        AssignedTo = ["user-123", "user-456"],
        DueDate = new DateTime(2025, 6, 30, 0, 0, 0, DateTimeKind.Utc),
        IsActive = true,
        CreatedBy = "admin-001",
        CreatedAt = new DateTime(2025, 1, 20, 0, 0, 0, DateTimeKind.Utc)
    };

    public static AssessmentResultDto GetAssessmentResultDto() => new()
    {
        ResultId = "result-001",
        AssessmentId = "assess-123",
        UserId = "user-123",
        Status = "Completed",
        Score = 85,
        CompletedAt = new DateTime(2025, 5, 10, 14, 30, 0, DateTimeKind.Utc)
    };

    public static AssessmentSummaryDto GetAssessmentSummaryDto() => new()
    {
        AssessmentId = "assess-123",
        Title = "Mid-Term Assessment",
        TotalAssigned = 30,
        TotalCompleted = 25,
        AverageScore = 78.4
    };

    public static CreateAssessmentDto GetCreateAssessmentDto() => new()
    {
        Title = "New Assessment",
        Description = "A new assessment for testing",
        Type = "Exam",
        ExamId = "exam-456",
        ChallengeIds = null,
        AssignedTo = ["user-123"],
        DueDate = new DateTime(2025, 7, 31, 0, 0, 0, DateTimeKind.Utc),
        CreatedBy = "admin-001"
    };
}
