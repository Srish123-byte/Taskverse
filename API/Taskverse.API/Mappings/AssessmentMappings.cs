using Taskverse.Api.Models;
using Taskverse.Business.DTOs;

namespace Taskverse.Api.Mappings;

public static class AssessmentMappings
{
    public static CreateQuestionDto ToDto(
        this CreateQuestionRequestModel model,
        Guid collegeId,
        string createdBy)
    {
        return new CreateQuestionDto
        {
            CollegeId = collegeId,
            CreatedBy = createdBy,
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
            DifficultyLevel = model.DifficultyLevel
        };
    }

    public static List<CreateQuestionDto> ToDtos(
        this IEnumerable<CreateQuestionRequestModel> models,
        Guid collegeId,
        string createdBy)
    {
        return models.Select(model => model.ToDto(collegeId, createdBy)).ToList();
    }

    public static DeleteQuestionsDto ToDto(
        this DeleteQuestionsRequestModel model,
        string createdBy)
    {
        return new DeleteQuestionsDto
        {
            CreatedBy = createdBy,
            QuestionIds = model.QuestionIds
        };
    }

    public static QuestionResponseModel ToResponseModel(this AssessmentQuestionDto dto)
    {
        return new QuestionResponseModel
        {
            QuestionId = dto.QuestionId,
            CollegeId = dto.CollegeId,
            Stream = dto.Stream,
            Subject = dto.Subject,
            Topic = dto.Topic,
            TopicTag = dto.TopicTag,
            QuestionType = dto.QuestionType,
            QuestionText = dto.QuestionText,
            Options = dto.Options,
            Answer = dto.Answer,
            Explanation = dto.Explanation,
            Marks = dto.Marks,
            NegativeMarks = dto.NegativeMarks,
            DifficultyLevel = dto.DifficultyLevel,
            Version = dto.Version,
            CreatedBy = dto.CreatedBy,
            CreatedAt = dto.CreatedAt,
            ModifiedAt = dto.ModifiedAt
        };
    }

    public static DeleteQuestionsResponseModel ToResponseModel(this IEnumerable<Guid> deletedQuestionIds)
    {
        return new DeleteQuestionsResponseModel
        {
            DeletedQuestionIds = deletedQuestionIds.ToList()
        };
    }
}
