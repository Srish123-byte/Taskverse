using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Taskverse.Api.MicroServices.Models;

namespace Taskverse.Business.Tests.Helpers;

public static class MockData
{
    public static UserModel GetUserModel() => new(
        UserId: TestConstants.UserId,
        Email: "john.doe@example.com",
        FirstName: "John",
        LastName: "Doe",
        Role: "Student",
        IsActive: true,
        CreatedAt: new DateTime(2025, 1, 15, 0, 0, 0, DateTimeKind.Utc),
        UpdatedAt: null);


    public static ExamModel GetExamModel() => new(
        ExamId: TestConstants.ExamId,
        Title: "Mid-Term Exam",
        Description: "Covers chapters 1 through 5",
        DurationMinutes: 90,
        TotalMarks: 100,
        PassingMarks: 60,
        IsActive: true,
        CreatedBy: "admin-001",
        CreatedAt: new DateTime(2025, 1, 20, 0, 0, 0, DateTimeKind.Utc));

    public static ExamResultModel GetExamResultModel() => new(
        SubmissionId: TestConstants.SubmissionId,
        ExamId: TestConstants.ExamId,
        UserId: TestConstants.UserId,
        Score: 75,
        TotalMarks: 100,
        IsPassed: true,
        CompletedAt: new DateTime(2025, 5, 10, 14, 30, 0, DateTimeKind.Utc));

    public static AssessmentModel GetAssessmentModel() => new(
        AssessmentId: TestConstants.AssessmentId,
        Title: "Mid-Term Assessment",
        Description: "Covers chapters 1 through 5",
        Type: "Exam",
        ExamId: TestConstants.ExamId,
        ChallengeIds: null,
        AssignedTo: [TestConstants.UserId, "user-456"],
        DueDate: new DateTime(2025, 6, 30, 0, 0, 0, DateTimeKind.Utc),
        IsActive: true,
        CreatedBy: "admin-001",
        CreatedAt: new DateTime(2025, 1, 20, 0, 0, 0, DateTimeKind.Utc));

    public static AssessmentResultModel GetAssessmentResultModel() => new(
        ResultId: "result-001",
        AssessmentId: TestConstants.AssessmentId,
        UserId: TestConstants.UserId,
        Status: "Completed",
        Score: 85,
        CompletedAt: new DateTime(2025, 5, 10, 14, 30, 0, DateTimeKind.Utc),
        ExamResult: GetExamResultModel(),
        CodingResults: null);

    public static AssessmentSummaryModel GetAssessmentSummaryModel() => new(
        AssessmentId: TestConstants.AssessmentId,
        Title: "Mid-Term Assessment",
        TotalAssigned: 30,
        TotalCompleted: 25,
        AverageScore: 78.4);

    public static ReportModel GetReportModel() => new(
        ReportId: TestConstants.ReportId,
        Type: "Performance",
        GeneratedFor: TestConstants.UserId,
        GeneratedAt: new DateTime(2025, 4, 27, 0, 0, 0, DateTimeKind.Utc),
        Status: "Completed",
        DownloadUrl: "https://reports.taskverse.io/report-001.pdf");

    public static UserPerformanceReportModel GetUserPerformanceReportModel() => new(
        UserId: TestConstants.UserId,
        TotalAssessments: 10,
        Completed: 8,
        AverageScore: 82.5,
        HighestScore: 95,
        LowestScore: 60,
        ReportGeneratedAt: new DateTime(2025, 4, 27, 0, 0, 0, DateTimeKind.Utc));

    public static AssessmentReportModel GetAssessmentReportModel() => new(
        AssessmentId: TestConstants.AssessmentId,
        Title: "Mid-Term Assessment",
        TotalParticipants: 30,
        AverageScore: 78.4,
        PassRate: 0.87,
        ReportGeneratedAt: new DateTime(2025, 4, 27, 0, 0, 0, DateTimeKind.Utc));

    public static LoginResponseModel GetLoginResponseModel() => new(
        AccessToken: TestConstants.AccessToken,
        RefreshToken: TestConstants.RefreshToken,
        ExpiresAt: new DateTime(2025, 4, 28, 0, 0, 0, DateTimeKind.Utc),
        UserId: TestConstants.UserId,
        Email: "test@example.com",
        FirstName: "Test",
        LastName: "User",
        CollegeName: "TestCollege",
        CollegeId: "college-123",
        Roles: ["Student"],
        Status: "APPROVED");

    public static ValidateTokenResponseModel GetValidateTokenResponseModel() => new(
        IsValid: true,
        UserId: TestConstants.UserId,
        Roles: ["Student"],
        ExpiresAt: new DateTime(2025, 4, 28, 0, 0, 0, DateTimeKind.Utc));

    public static ObjectResult GetObjectResult<T>(T value, int statusCode = 200)
        => new(value) { StatusCode = statusCode };

    public static ObjectResult GetJsonObjectResult<T>(T value, int statusCode = 200)
    {
        string json = JsonConvert.SerializeObject(value);
        return new ObjectResult(json) { StatusCode = statusCode };
    }
}
