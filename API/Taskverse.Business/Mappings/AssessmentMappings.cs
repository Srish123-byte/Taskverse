using Taskverse.Api.MicroServices.Models;
using Taskverse.Business.DTOs;

namespace Taskverse.Business.Mappings;

public static class AssessmentMappings
{
    public static AssessmentDto ToDto(this AssessmentModel model)
        => new()
        {
            AssessmentId = model.AssessmentId,
            Title = model.Title,
            Description = model.Description,
            Type = model.Type,
            ExamId = model.ExamId,
            ChallengeIds = model.ChallengeIds,
            AssignedTo = model.AssignedTo,
            DueDate = model.DueDate,
            IsActive = model.IsActive,
            CreatedBy = model.CreatedBy,
            CreatedAt = model.CreatedAt
        };

    public static AssessmentResultDto ToDto(this AssessmentResultModel model)
        => new()
        {
            ResultId = model.ResultId,
            AssessmentId = model.AssessmentId,
            UserId = model.UserId,
            Status = model.Status,
            Score = model.Score,
            CompletedAt = model.CompletedAt
        };

    public static AssessmentSummaryDto ToDto(this AssessmentSummaryModel model)
        => new()
        {
            AssessmentId = model.AssessmentId,
            Title = model.Title,
            TotalAssigned = model.TotalAssigned,
            TotalCompleted = model.TotalCompleted,
            AverageScore = model.AverageScore
        };

    public static CreateAssessmentModel ToMicroServiceModel(this CreateAssessmentDto dto)
        => new(
            dto.Title,
            dto.Description,
            dto.Type,
            dto.ExamId,
            dto.ChallengeIds,
            dto.AssignedTo,
            dto.DueDate,
            dto.CreatedBy);
}
