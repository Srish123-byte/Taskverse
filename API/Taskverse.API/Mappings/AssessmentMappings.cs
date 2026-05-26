using Taskverse.Api.Models;
using Taskverse.Business.DTOs;
using Taskverse.Data.Utilities;

namespace Taskverse.Api.Mappings;

public static class AssessmentMappings
{
    public static CreateQuestionBankAssessmentDto ToDto(
        this CreateQuestionBankAssessmentRequestModel model,
        Guid collegeId,
        string createdBy)
    {
        return new CreateQuestionBankAssessmentDto
        {
            CollegeId = collegeId,
            CreatedBy = createdBy,
            AssessmentName = model.AssessmentName,
            SubjectId = model.SubjectId,
            SubjectName = model.SubjectName,
            TopicId = model.TopicId,
            TopicName = model.TopicName,
            AssignedBatchIds = model.AssignedBatchIds,
            QuestionIds = model.QuestionIds,
            DurationMinutes = model.DurationMinutes,
            TotalMarks = model.TotalMarks,
            StartDateTime = UtcDateTime.Normalize(model.StartDateTime),
            EndDateTime = UtcDateTime.Normalize(model.EndDateTime)
        };
    }

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
            SubjectId = model.SubjectId,
            Subject = model.Subject,
            TopicId = model.TopicId,
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

    public static QuestionBankSearchDto ToDto(
        this QuestionBankSearchRequestModel model,
        Guid collegeId)
    {
        return new QuestionBankSearchDto
        {
            CollegeId = collegeId,
            DifficultyLevel = model.DifficultyLevel,
            SubjectId = model.SubjectId,
            TopicId = model.TopicId,
            Subject = model.Subject,
            Topic = model.Topic,
            PageNumber = model.PageNumber > 0 ? model.PageNumber : 1,
            PageSize = model.PageSize > 0 ? model.PageSize : 10
        };
    }

    public static QuestionResponseModel ToResponseModel(this AssessmentQuestionDto dto)
    {
        return new QuestionResponseModel
        {
            QuestionId = dto.QuestionId,
            CollegeId = dto.CollegeId,
            SubjectId = dto.SubjectId,
            TopicId = dto.TopicId,
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
            CreatedAt = UtcDateTime.Normalize(dto.CreatedAt),
            ModifiedAt = UtcDateTime.Normalize(dto.ModifiedAt)
        };
    }

    public static PagedQuestionBankResponseModel ToResponseModel(this PagedQuestionBankDto dto)
    {
        return new PagedQuestionBankResponseModel
        {
            Items = dto.Items.Select(item => item.ToResponseModel()).ToList(),
            TotalCount = dto.TotalCount,
            PageNumber = dto.PageNumber,
            PageSize = dto.PageSize
        };
    }

    public static QuestionBankAssessmentResponseModel ToResponseModel(this QuestionBankAssessmentDto dto)
    {
        return new QuestionBankAssessmentResponseModel
        {
            AssessmentId = dto.AssessmentId,
            CollegeId = dto.CollegeId,
            SubjectId = dto.SubjectId,
            SubjectName = dto.SubjectName,
            TopicId = dto.TopicId,
            TopicName = dto.TopicName,
            AssessmentName = dto.AssessmentName,
            AssessmentType = dto.AssessmentType,
            AssessmentStatus = dto.AssessmentStatus,
            DurationMinutes = dto.DurationMinutes,
            TotalMarks = dto.TotalMarks,
            DifficultyLevel = dto.DifficultyLevel,
            StartDateTime = UtcDateTime.Normalize(dto.StartDateTime),
            EndDateTime = UtcDateTime.Normalize(dto.EndDateTime),
            Instructions = dto.Instructions,
            AssignedBatchIds = dto.AssignedBatchIds,
            AllowLateEntry = dto.AllowLateEntry,
            ShowResultsImmediately = dto.ShowResultsImmediately,
            AllowQuestionReview = dto.AllowQuestionReview,
            NegativeMarking = dto.NegativeMarking,
            MarksPerQuestion = dto.MarksPerQuestion,
            IsTotalMarksAutoCalculated = dto.IsTotalMarksAutoCalculated,
            CreatedBy = dto.CreatedBy,
            CreatedAt = UtcDateTime.Normalize(dto.CreatedAt),
            ModifiedAt = UtcDateTime.Normalize(dto.ModifiedAt),
            QuestionIds = dto.QuestionIds
        };
    }

    public static DeleteQuestionsResponseModel ToResponseModel(this IEnumerable<Guid> deletedQuestionIds)
    {
        return new DeleteQuestionsResponseModel
        {
            DeletedQuestionIds = deletedQuestionIds.ToList()
        };
    }

    public static AssessmentQuestionListItemResponseModel ToResponseModel(
        this AssessmentQuestionListItemDto dto)
    {
        return new AssessmentQuestionListItemResponseModel
        {
            QuestionId      = dto.QuestionId,
            DisplayOrder    = dto.DisplayOrder,
            QuestionType    = dto.QuestionType,
            QuestionText    = dto.QuestionText,
            Options         = dto.Options,
            Marks           = dto.Marks,
            NegativeMarks   = dto.NegativeMarks,
            DifficultyLevel = dto.DifficultyLevel
        };
    }

    public static PagedAssessmentQuestionListResponseModel ToResponseModel(
        this PagedAssessmentQuestionListDto dto)
    {
        return new PagedAssessmentQuestionListResponseModel
        {
            Items      = dto.Items.Select(item => item.ToResponseModel()).ToList(),
            TotalCount = dto.TotalCount,
            PageNumber = dto.PageNumber,
            PageSize   = dto.PageSize
        };
    }

    public static StudentAssessmentListResponseModel ToResponseModel(
        this StudentAssessmentListItemDto dto)
    {
        return new StudentAssessmentListResponseModel
        {
            AssessmentId = dto.AssessmentId,
            AssessmentName = dto.AssessmentName,
            AssessmentStatus = dto.AssessmentStatus,
            DurationMinutes = dto.DurationMinutes,
            TotalMarks = dto.TotalMarks,
            DifficultyLevel = dto.DifficultyLevel,
            StartDateTime = UtcDateTime.Normalize(dto.StartDateTime),
            EndDateTime = UtcDateTime.Normalize(dto.EndDateTime)
        };
    }

    public static StudentAssessmentDetailResponseModel ToResponseModel(
        this StudentAssessmentDetailDto dto)
    {
        return new StudentAssessmentDetailResponseModel
        {
            AssessmentName = dto.AssessmentName,
            DurationMinutes = dto.DurationMinutes,
            TotalMarks = dto.TotalMarks,
            TotalQuestions = dto.TotalQuestions,
            StartTime = UtcDateTime.Normalize(dto.StartTime),
            EndTime = UtcDateTime.Normalize(dto.EndTime),
            Instructions = dto.Instructions
        };
    }

    public static StudentAssessmentStartResponseModel ToResponseModel(
        this StudentAssessmentStartDto dto)
    {
        return new StudentAssessmentStartResponseModel
        {
            AttemptId = dto.AttemptId,
            AssessmentId = dto.AssessmentId,
            AttemptStatus = dto.AttemptStatus,
            StartedAt = UtcDateTime.Normalize(dto.StartedAt)
        };
    }

    public static SaveStudentAttemptAnswerDto ToDto(this SaveStudentAttemptAnswerRequestModel model)
    {
        return new SaveStudentAttemptAnswerDto
        {
            SelectedAnswer = model.SelectedAnswer
        };
    }

    public static StudentAttemptAnswerResponseModel ToResponseModel(
        this StudentAttemptAnswerDto dto)
    {
        return new StudentAttemptAnswerResponseModel
        {
            QuestionId = dto.QuestionId,
            SelectedAnswer = dto.SelectedAnswer,
            AnsweredAt = UtcDateTime.Normalize(dto.AnsweredAt)
        };
    }

    public static StudentAttemptSubmitResponseModel ToResponseModel(
        this StudentAttemptSubmitDto dto)
    {
        return new StudentAttemptSubmitResponseModel
        {
            AttemptId = dto.AttemptId,
            AttemptStatus = dto.AttemptStatus,
            SubmittedAt = UtcDateTime.Normalize(dto.SubmittedAt)
        };
    }

    public static StudentAttemptRecoveryQuestionResponseModel ToResponseModel(
        this StudentAttemptRecoveryQuestionDto dto)
    {
        return new StudentAttemptRecoveryQuestionResponseModel
        {
            QuestionId = dto.QuestionId,
            DisplayOrder = dto.DisplayOrder,
            QuestionType = dto.QuestionType,
            QuestionText = dto.QuestionText,
            Options = dto.Options,
            Marks = dto.Marks,
            NegativeMarks = dto.NegativeMarks,
            DifficultyLevel = dto.DifficultyLevel,
            SelectedAnswer = dto.SelectedAnswer,
            AnsweredAt = UtcDateTime.Normalize(dto.AnsweredAt)
        };
    }

    public static StudentAttemptRecoveryResponseModel ToResponseModel(
        this StudentAttemptRecoveryDto dto)
    {
        return new StudentAttemptRecoveryResponseModel
        {
            AttemptId = dto.AttemptId,
            AssessmentId = dto.AssessmentId,
            AssessmentName = dto.AssessmentName,
            AttemptStatus = dto.AttemptStatus,
            StartedAt = UtcDateTime.Normalize(dto.StartedAt),
            SubmittedAt = UtcDateTime.Normalize(dto.SubmittedAt),
            ExpiresAt = UtcDateTime.Normalize(dto.ExpiresAt),
            RemainingSeconds = dto.RemainingSeconds,
            DurationMinutes = dto.DurationMinutes,
            TotalMarks = dto.TotalMarks,
            TotalQuestions = dto.TotalQuestions,
            AttemptedQuestions = dto.AttemptedQuestions,
            UnansweredQuestions = dto.UnansweredQuestions,
            Instructions = dto.Instructions,
            Questions = dto.Questions.Select(item => item.ToResponseModel()).ToList()
        };
    }
}
