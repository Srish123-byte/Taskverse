using Taskverse.Api.Models;
using Taskverse.Business.DTOs;

namespace Taskverse.Api.Mappings;

public static class ReportMappings
{
    public static GenerateReportDto ToDto(this GenerateReportRequestModel model) => new()
    {
        Type = model.Type,
        UserId = model.UserId,
        AssessmentId = model.AssessmentId,
        ExamId = model.ExamId,
        DateFrom = model.DateFrom,
        DateTo = model.DateTo
    };

    public static ReportResponseModel ToResponseModel(this ReportDto dto) => new()
    {
        ReportId = dto.ReportId,
        Type = dto.Type,
        GeneratedFor = dto.GeneratedFor,
        GeneratedAt = dto.GeneratedAt,
        Status = dto.Status,
        DownloadUrl = dto.DownloadUrl
    };

    public static UserPerformanceReportResponseModel ToResponseModel(this UserPerformanceReportDto dto) => new()
    {
        UserId = dto.UserId,
        TotalAssessments = dto.TotalAssessments,
        Completed = dto.Completed,
        AverageScore = dto.AverageScore,
        HighestScore = dto.HighestScore,
        LowestScore = dto.LowestScore,
        ReportGeneratedAt = dto.ReportGeneratedAt
    };

    public static AssessmentReportResponseModel ToResponseModel(this AssessmentReportDto dto) => new()
    {
        AssessmentId = dto.AssessmentId,
        Title = dto.Title,
        TotalParticipants = dto.TotalParticipants,
        AverageScore = dto.AverageScore,
        PassRate = dto.PassRate,
        ReportGeneratedAt = dto.ReportGeneratedAt
    };
}
