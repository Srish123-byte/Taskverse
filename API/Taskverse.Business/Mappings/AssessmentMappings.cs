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

    public static AssessmentQuestionDto ToDto(this AssessmentQuestionModel model)
        => new()
        {
            QuestionId = model.QuestionId,
            CollegeId = model.CollegeId,
            Stream = model.Stream,
            Subject = model.Subject,
            Topic = model.Topic,
            TopicTag = model.TopicTag,
            QuestionType = model.QuestionType,
            QuestionText = model.QuestionText,
            Options = model.Options,
            Answer = model.Answer,
            Explanation = model.Explanation,
            Marks = model.Marks,
            NegativeMarks = model.NegativeMarks,
            DifficultyLevel = model.DifficultyLevel,
            Version = model.Version,
            CreatedBy = model.CreatedBy,
            CreatedAt = model.CreatedAt,
            ModifiedAt = model.ModifiedAt
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

    public static CreateQuestionModel ToMicroServiceModel(this CreateQuestionDto dto)
        => new(
            dto.CollegeId,
            dto.CreatedBy,
            dto.Stream,
            dto.Subject,
            dto.Topic,
            dto.TopicTag,
            dto.QuestionType,
            dto.QuestionText,
            dto.Options,
            dto.Answer,
            dto.Explanation,
            dto.Marks,
            dto.NegativeMarks,
            dto.DifficultyLevel);

    public static List<CreateQuestionModel> ToMicroServiceModels(this IEnumerable<CreateQuestionDto> dtos)
        => dtos.Select(dto => dto.ToMicroServiceModel()).ToList();

    public static DeleteQuestionsModel ToMicroServiceModel(this DeleteQuestionsDto dto)
        => new(
            dto.CreatedBy,
            dto.QuestionIds);
}
