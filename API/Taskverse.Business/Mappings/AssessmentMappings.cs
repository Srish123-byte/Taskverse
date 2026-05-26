using Taskverse.Api.MicroServices.Models;
using Taskverse.Business.DTOs;
using Taskverse.Data.Utilities;

namespace Taskverse.Business.Mappings;

public static class AssessmentMappings
{
    public static AssessmentQuestionDto ToDto(this AssessmentQuestionModel model)
        => new()
        {
            QuestionId = model.QuestionId,
            CollegeId = model.CollegeId,
            SubjectId = model.SubjectId,
            TopicId = model.TopicId,
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
            CreatedAt = UtcDateTime.Normalize(model.CreatedAt),
            ModifiedAt = UtcDateTime.Normalize(model.ModifiedAt)
        };

    public static QuestionBankAssessmentDto ToDto(this QuestionBankAssessmentModel model)
        => new()
        {
            AssessmentId = model.AssessmentId,
            CollegeId = model.CollegeId,
            SubjectId = model.SubjectId,
            SubjectName = model.SubjectName,
            TopicId = model.TopicId,
            TopicName = model.TopicName,
            AssessmentName = model.AssessmentName,
            AssessmentType = model.AssessmentType,
            AssessmentStatus = model.AssessmentStatus,
            DurationMinutes = model.DurationMinutes,
            TotalMarks = model.TotalMarks,
            DifficultyLevel = model.DifficultyLevel,
            StartDateTime = UtcDateTime.Normalize(model.StartDateTime),
            EndDateTime = UtcDateTime.Normalize(model.EndDateTime),
            Instructions = model.Instructions,
            AssignedBatchIds = model.AssignedBatchIds,
            AllowLateEntry = model.AllowLateEntry,
            ShowResultsImmediately = model.ShowResultsImmediately,
            AllowQuestionReview = model.AllowQuestionReview,
            NegativeMarking = model.NegativeMarking,
            MarksPerQuestion = model.MarksPerQuestion,
            IsTotalMarksAutoCalculated = model.IsTotalMarksAutoCalculated,
            CreatedBy = model.CreatedBy,
            CreatedAt = UtcDateTime.Normalize(model.CreatedAt),
            ModifiedAt = UtcDateTime.Normalize(model.ModifiedAt),
            QuestionIds = model.QuestionIds
        };

    public static PagedQuestionBankDto ToDto(this PagedQuestionBankModel model)
        => new()
        {
            Items = model.Items.Select(item => item.ToDto()).ToList(),
            TotalCount = model.TotalCount,
            PageNumber = model.PageNumber,
            PageSize = model.PageSize
        };

    public static CreateQuestionBankAssessmentModel ToMicroServiceModel(this CreateQuestionBankAssessmentDto dto)
        => new(
            dto.CollegeId,
            dto.CreatedBy,
            dto.AssessmentName,
            dto.SubjectId,
            dto.SubjectName,
            dto.TopicId,
            dto.TopicName,
            dto.AssignedBatchIds,
            dto.QuestionIds,
            dto.DurationMinutes,
            dto.TotalMarks,
            UtcDateTime.Normalize(dto.StartDateTime),
            UtcDateTime.Normalize(dto.EndDateTime));

    public static DeleteAssessmentModel ToMicroServiceModel(this DeleteAssessmentDto dto)
        => new(
            dto.AssessmentId,
            dto.DeletedBy,
            dto.RequesterRole,
            dto.CollegeId);

    public static CreateQuestionModel ToMicroServiceModel(this CreateQuestionDto dto)
        => new(
            dto.CollegeId,
            dto.CreatedBy,
            dto.Stream,
            dto.SubjectId,
            dto.Subject,
            dto.TopicId,
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

    public static QuestionBankSearchModel ToMicroServiceModel(this QuestionBankSearchDto dto)
        => new(
            dto.CollegeId,
            dto.DifficultyLevel,
            dto.SubjectId,
            dto.TopicId,
            dto.Subject,
            dto.Topic,
            dto.PageNumber,
            dto.PageSize);

    public static AssessmentQuestionListItemDto ToDto(this AssessmentQuestionListItemModel model)
        => new()
        {
            QuestionId     = model.QuestionId,
            DisplayOrder   = model.DisplayOrder,
            QuestionType   = model.QuestionType,
            QuestionText   = model.QuestionText,
            Options        = model.Options,
            Marks          = model.Marks,
            NegativeMarks  = model.NegativeMarks,
            DifficultyLevel = model.DifficultyLevel
        };

    public static PagedAssessmentQuestionListDto ToDto(this PagedAssessmentQuestionListModel model)
        => new()
        {
            Items      = model.Items.Select(item => item.ToDto()).ToList(),
            TotalCount = model.TotalCount,
            PageNumber = model.PageNumber,
            PageSize   = model.PageSize
        };

    public static StudentAssessmentListItemDto ToDto(this StudentAssessmentListItemModel model)
        => new()
        {
            AssessmentId = model.AssessmentId,
            AssessmentName = model.AssessmentName,
            AssessmentStatus = model.AssessmentStatus,
            DurationMinutes = model.DurationMinutes,
            TotalMarks = model.TotalMarks,
            DifficultyLevel = model.DifficultyLevel,
            StartDateTime = UtcDateTime.Normalize(model.StartDateTime),
            EndDateTime = UtcDateTime.Normalize(model.EndDateTime)
        };

    public static StudentAssessmentDetailDto ToDto(this StudentAssessmentDetailModel model)
        => new()
        {
            AssessmentName = model.AssessmentName,
            DurationMinutes = model.DurationMinutes,
            TotalMarks = model.TotalMarks,
            TotalQuestions = model.TotalQuestions,
            StartTime = UtcDateTime.Normalize(model.StartTime),
            EndTime = UtcDateTime.Normalize(model.EndTime),
            Instructions = model.Instructions
        };

    public static StudentAssessmentStartDto ToDto(this StudentAssessmentStartModel model)
        => new()
        {
            AttemptId = model.AttemptId,
            AssessmentId = model.AssessmentId,
            AttemptStatus = model.AttemptStatus,
            StartedAt = UtcDateTime.Normalize(model.StartedAt)
        };

    public static SaveStudentAttemptAnswerModel ToMicroServiceModel(this SaveStudentAttemptAnswerDto dto)
        => new(
            dto.QuestionId,
            dto.SelectedAnswer);

    public static StudentAttemptAnswerDto ToDto(this StudentAttemptAnswerModel model)
        => new()
        {
            QuestionId = model.QuestionId,
            SelectedAnswer = model.SelectedAnswer,
            AnsweredAt = UtcDateTime.Normalize(model.AnsweredAt)
        };

    public static StudentAttemptRecoveryQuestionDto ToDto(this StudentAttemptRecoveryQuestionModel model)
        => new()
        {
            QuestionId = model.QuestionId,
            DisplayOrder = model.DisplayOrder,
            QuestionType = model.QuestionType,
            QuestionText = model.QuestionText,
            Options = model.Options,
            Marks = model.Marks,
            NegativeMarks = model.NegativeMarks,
            DifficultyLevel = model.DifficultyLevel,
            SelectedAnswer = model.SelectedAnswer,
            AnsweredAt = UtcDateTime.Normalize(model.AnsweredAt)
        };

    public static StudentAttemptRecoveryDto ToDto(this StudentAttemptRecoveryModel model)
        => new()
        {
            AttemptId = model.AttemptId,
            AssessmentId = model.AssessmentId,
            AssessmentName = model.AssessmentName,
            AttemptStatus = model.AttemptStatus,
            StartedAt = UtcDateTime.Normalize(model.StartedAt),
            SubmittedAt = UtcDateTime.Normalize(model.SubmittedAt),
            ExpiresAt = UtcDateTime.Normalize(model.ExpiresAt),
            RemainingSeconds = model.RemainingSeconds,
            DurationMinutes = model.DurationMinutes,
            TotalMarks = model.TotalMarks,
            TotalQuestions = model.TotalQuestions,
            AttemptedQuestions = model.AttemptedQuestions,
            UnansweredQuestions = model.UnansweredQuestions,
            Instructions = model.Instructions,
            Questions = model.Questions.Select(item => item.ToDto()).ToList()
        };
}
