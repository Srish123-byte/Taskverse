using Taskverse.Api.Models;
using Taskverse.Business.DTOs;

namespace Taskverse.Api.Mappings;

public static class AssessmentMappings
{
    public static CreateAssessmentDto ToDto(this CreateAssessmentRequestModel model) => new()
    {
        Title = model.Title,
        Description = model.Description,
        Type = model.Type,
        ExamId = model.ExamId,
        ChallengeIds = model.ChallengeIds,
        AssignedTo = model.AssignedTo,
        DueDate = model.DueDate,
        CreatedBy = model.CreatedBy
    };

    public static AssessmentResponseModel ToResponseModel(this AssessmentDto dto) => new()
    {
        AssessmentId = dto.AssessmentId,
        Title = dto.Title,
        Description = dto.Description,
        Type = dto.Type,
        ExamId = dto.ExamId,
        ChallengeIds = dto.ChallengeIds,
        AssignedTo = dto.AssignedTo,
        DueDate = dto.DueDate,
        IsActive = dto.IsActive,
        CreatedBy = dto.CreatedBy,
        CreatedAt = dto.CreatedAt
    };

    public static AssessmentResultResponseModel ToResponseModel(this AssessmentResultDto dto) => new()
    {
        ResultId = dto.ResultId,
        AssessmentId = dto.AssessmentId,
        UserId = dto.UserId,
        Status = dto.Status,
        Score = dto.Score,
        CompletedAt = dto.CompletedAt
    };

    public static AssessmentSummaryResponseModel ToResponseModel(this AssessmentSummaryDto dto) => new()
    {
        AssessmentId = dto.AssessmentId,
        Title = dto.Title,
        TotalAssigned = dto.TotalAssigned,
        TotalCompleted = dto.TotalCompleted,
        AverageScore = dto.AverageScore
    };
}
