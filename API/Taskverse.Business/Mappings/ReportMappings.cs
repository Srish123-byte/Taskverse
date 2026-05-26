using Taskverse.Api.MicroServices.Models;
using Taskverse.Business.DTOs;

namespace Taskverse.Business.Mappings;

public static class ReportMappings
{
    public static ReportDto ToDto(this ReportModel model)
        => new()
        {
            ReportId = model.ReportId,
            Type = model.Type,
            GeneratedFor = model.GeneratedFor,
            GeneratedAt = model.GeneratedAt,
            Status = model.Status,
            DownloadUrl = model.DownloadUrl
        };

    public static UserPerformanceReportDto ToDto(this UserPerformanceReportModel model)
        => new()
        {
            UserId = model.UserId,
            TotalAssessments = model.TotalAssessments,
            Completed = model.Completed,
            AverageScore = model.AverageScore,
            HighestScore = model.HighestScore,
            LowestScore = model.LowestScore,
            ReportGeneratedAt = model.ReportGeneratedAt
        };

    public static AssessmentReportDto ToDto(this AssessmentReportModel model)
        => new()
        {
            AssessmentId = model.AssessmentId,
            Title = model.Title,
            TotalParticipants = model.TotalParticipants,
            AverageScore = model.AverageScore,
            PassRate = model.PassRate,
            ReportGeneratedAt = model.ReportGeneratedAt
        };

    public static StudentResultDto ToDto(this StudentResultModel model)
        => new()
        {
            ResultId = model.ResultId,
            AssessmentId = model.AssessmentId,
            AssessmentName = model.AssessmentName,
            AttemptId = model.AttemptId,
            StudentId = model.StudentId,
            TotalMarks = model.TotalMarks,
            ObtainedMarks = model.ObtainedMarks,
            Percentage = model.Percentage,
            Rank = model.Rank,
            ResultStatus = model.ResultStatus,
            GeneratedAt = model.GeneratedAt,
            HasPendingCodingEvaluation = model.HasPendingCodingEvaluation
        };

    public static GenerateReportRequestModel ToMicroServiceModel(this GenerateReportDto dto)
        => new(dto.Type, dto.UserId, dto.AssessmentId, dto.ExamId, dto.DateFrom, dto.DateTo);
}
