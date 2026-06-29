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

    public static QuestionTopicCatalogDto ToDto(this QuestionTopicCatalogModel model)
        => new()
        {
            TopicId = model.TopicId,
            TopicName = model.TopicName
        };

    public static QuestionSubjectCatalogDto ToDto(this QuestionSubjectCatalogModel model)
        => new()
        {
            SubjectId = model.SubjectId,
            SubjectName = model.SubjectName,
            Topics = model.Topics.Select(item => item.ToDto()).ToList()
        };

    public static QuestionClassificationCatalogDto ToDto(this QuestionClassificationCatalogModel model)
        => new()
        {
            Subjects = model.Subjects.Select(item => item.ToDto()).ToList()
        };

    public static CreateQuestionClassificationEntryModel ToMicroServiceModel(this CreateQuestionClassificationEntryDto dto)
        => new(
            dto.SubjectId,
            dto.SubjectName,
            dto.TopicName);

    public static QuestionClassificationEntryDto ToDto(this QuestionClassificationEntryModel model)
        => new()
        {
            SubjectId = model.SubjectId,
            SubjectName = model.SubjectName,
            TopicId = model.TopicId,
            TopicName = model.TopicName
        };

    public static QuestionBankAssessmentDto ToDto(this QuestionBankAssessmentModel model)
        => new()
        {
            AssessmentId = model.AssessmentId,
            CollegeId = model.CollegeId,
            SubjectIds = model.SubjectIds,
            SubjectNames = model.SubjectNames,
            SubjectName = model.SubjectName,
            SubjectDisplayLabel = model.SubjectDisplayLabel,
            TopicIds = model.TopicIds,
            TopicNames = model.TopicNames,
            TopicName = model.TopicName,
            TopicDisplayLabel = model.TopicDisplayLabel,
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
            PassingPercentage = model.PassingPercentage,
            AllowQuestionReview = model.AllowQuestionReview,
            NegativeMarking = model.NegativeMarking,
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

    public static AssessmentSearchItemDto ToDto(this AssessmentSearchItemModel model)
        => new()
        {
            AssessmentId = model.AssessmentId,
            AssessmentName = model.AssessmentName,
            SubjectIds = model.SubjectIds,
            SubjectNames = model.SubjectNames,
            SubjectName = model.SubjectName,
            SubjectDisplayLabel = model.SubjectDisplayLabel,
            TopicIds = model.TopicIds,
            TopicNames = model.TopicNames,
            TopicName = model.TopicName,
            TopicDisplayLabel = model.TopicDisplayLabel,
            AssessmentStatus = model.AssessmentStatus,
            AssessmentDate = UtcDateTime.Normalize(model.AssessmentDate),
            StartDateTime = UtcDateTime.Normalize(model.StartDateTime),
            TotalMarks = model.TotalMarks,
            DifficultyLevel = model.DifficultyLevel
        };

    public static PagedAssessmentSearchDto ToDto(this PagedAssessmentSearchModel model)
        => new()
        {
            Items = model.Items.Select(item => item.ToDto()).ToList(),
            TotalCount = model.TotalCount,
            ActiveCount = model.ActiveCount,
            CompletedCount = model.CompletedCount,
            PageNumber = model.PageNumber,
            PageSize = model.PageSize
        };

    public static AssessmentAssignmentBatchDto ToDto(this AssessmentAssignmentBatchModel model)
        => new()
        {
            BatchId = model.BatchId.ToString(),
            ClassId = model.ClassId.ToString(),
            CollegeId = model.CollegeId.ToString(),
            Name = model.Name
        };

    public static AssessmentAssignmentClassDto ToDto(this AssessmentAssignmentClassModel model)
        => new()
        {
            ClassId = model.ClassId.ToString(),
            CollegeId = model.CollegeId.ToString(),
            Name = model.Name,
            AcademicYear = model.AcademicYear,
            Batches = model.Batches.Select(item => item.ToDto()).ToList()
        };

    public static AssessmentAssignmentCatalogDto ToDto(this AssessmentAssignmentCatalogModel model)
        => new()
        {
            Classes = model.Classes.Select(item => item.ToDto()).ToList()
        };

    public static CreateQuestionBankAssessmentModel ToMicroServiceModel(this CreateQuestionBankAssessmentDto dto)
        => new(
            dto.CollegeId,
            dto.CreatedBy,
            dto.AssessmentName,
            dto.SubjectIds,
            dto.TopicIds,
            dto.Instructions,
            dto.AllowLateEntry,
            dto.AllowQuestionReview,
            dto.NegativeMarking,
            dto.PassingPercentage,
            dto.AssignedBatchIds,
            dto.QuestionIds,
            dto.DurationMinutes,
            dto.TotalMarks,
            UtcDateTime.Normalize(dto.StartDateTime),
            UtcDateTime.Normalize(dto.EndDateTime));

    public static PublishQuestionBankAssessmentModel ToMicroServiceModel(this PublishQuestionBankAssessmentDto dto)
        => new(
            dto.AssessmentId,
            dto.CollegeId,
            dto.CreatedBy,
            dto.AssessmentName,
            dto.SubjectIds,
            dto.TopicIds,
            dto.Instructions,
            dto.AllowLateEntry,
            dto.AllowQuestionReview,
            dto.NegativeMarking,
            dto.PassingPercentage,
            dto.AssignedBatchIds,
            dto.QuestionIds,
            dto.DurationMinutes,
            dto.TotalMarks,
            UtcDateTime.Normalize(dto.StartDateTime),
            UtcDateTime.Normalize(dto.EndDateTime));

    public static UpdateQuestionBankAssessmentModel ToMicroServiceModel(this UpdateQuestionBankAssessmentDto dto)
        => new(
            dto.AssessmentId,
            dto.CollegeId,
            dto.UpdatedBy,
            dto.RequesterRole,
            dto.AssessmentName,
            dto.SubjectIds,
            dto.TopicIds,
            dto.Instructions,
            dto.AllowLateEntry,
            dto.AllowQuestionReview,
            dto.NegativeMarking,
            dto.PassingPercentage,
            dto.AssignedBatchIds,
            dto.QuestionIds,
            dto.DurationMinutes,
            dto.TotalMarks,
            UtcDateTime.Normalize(dto.StartDateTime),
            UtcDateTime.Normalize(dto.EndDateTime),
            dto.IsDraftSave);

    public static DeleteAssessmentModel ToMicroServiceModel(this DeleteAssessmentDto dto)
        => new(
            dto.AssessmentId,
            dto.IsDeleted,
            dto.DeletedBy,
            dto.RequesterRole,
            dto.CollegeId);

    public static CreateQuestionModel ToMicroServiceModel(this CreateQuestionDto dto)
        => new(
            dto.CollegeId,
            dto.CreatedBy,
            dto.RequesterRole,
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
            dto.CorrectAnswers,
            dto.Explanation,
            dto.Marks,
            dto.NegativeMarks,
            dto.DifficultyLevel,
            dto.SourceRowNumber);

    public static List<CreateQuestionModel> ToMicroServiceModels(this IEnumerable<CreateQuestionDto> dtos)
        => dtos.Select(dto => dto.ToMicroServiceModel()).ToList();

    public static DeleteQuestionsModel ToMicroServiceModel(this DeleteQuestionsDto dto)
        => new(
            dto.CreatedBy,
            dto.RequesterRole,
            dto.CollegeId,
            dto.QuestionIds);

    public static AssessmentBootstrapModel ToMicroServiceModel(this AssessmentBootstrapDto dto)
        => new(
            dto.CollegeId,
            dto.RequesterRole,
            dto.RequesterUserId);

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

    public static AssessmentSearchModel ToMicroServiceModel(this AssessmentSearchDto dto)
        => new(
            dto.CollegeId,
            dto.RequesterRole,
            dto.RequesterName,
            dto.SearchTerm,
            dto.AssessmentStatus,
            dto.DifficultyLevel,
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
            SubjectName = model.SubjectName,
            TopicName = model.TopicName,
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
            dto.SelectedAnswer,
            dto.SelectedAnswers);

    public static StudentAttemptAnswerDto ToDto(this StudentAttemptAnswerModel model)
        => new()
        {
            QuestionId = model.QuestionId,
            SelectedAnswer = model.SelectedAnswer,
            SelectedAnswers = model.SelectedAnswers,
            AnsweredAt = UtcDateTime.Normalize(model.AnsweredAt)
        };

    public static StudentAttemptSubmitDto ToDto(this StudentAttemptSubmitModel model)
        => new()
        {
            AttemptId = model.AttemptId,
            AttemptStatus = model.AttemptStatus,
            SubmittedAt = UtcDateTime.Normalize(model.SubmittedAt)
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
            AllowsMultipleAnswers = model.AllowsMultipleAnswers,
            SelectedAnswer = model.SelectedAnswer,
            SelectedAnswers = model.SelectedAnswers,
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

    public static StudentStreakDto ToDto(this StudentStreakModel model)
        => new()
        {
            CurrentStreak = model.CurrentStreak,
            LongestStreak = model.LongestStreak,
            TotalAssessmentsTaken = model.TotalAssessmentsTaken
        };
}
