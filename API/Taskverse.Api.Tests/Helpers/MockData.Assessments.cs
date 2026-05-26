using Taskverse.Business.DTOs;

namespace Taskverse.Api.Tests.Helpers;

public static partial class MockData
{
    public static QuestionBankAssessmentDto GetQuestionBankAssessmentDto() => new()
    {
        AssessmentId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
        CollegeId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
        AssessmentName = "Mid-Term Assessment",
        AssessmentType = "mcq",
        AssessmentStatus = "scheduled",
        DurationMinutes = 60,
        TotalMarks = 100,
        DifficultyLevel = 3,
        AssignedBatchIds = [Guid.Parse("33333333-3333-3333-3333-333333333333")],
        QuestionIds = [Guid.Parse("44444444-4444-4444-4444-444444444444")],
        CreatedBy = "admin-001",
        CreatedAt = new DateTime(2025, 1, 20, 0, 0, 0, DateTimeKind.Utc)
    };
}
