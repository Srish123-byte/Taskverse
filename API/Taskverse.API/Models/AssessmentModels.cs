namespace Taskverse.Api.Models;

public class CreateQuestionBankAssessmentRequestModel
{
    public string AssessmentName { get; set; } = string.Empty;
    public Guid? SubjectId { get; set; }
    public string? SubjectName { get; set; }
    public Guid? TopicId { get; set; }
    public string? TopicName { get; set; }
    public string? Instructions { get; set; }
    public bool AllowLateEntry { get; set; }
    public bool AllowQuestionReview { get; set; }
    public bool NegativeMarking { get; set; }
    public int PassingPercentage { get; set; }
    public Guid[] AssignedBatchIds { get; set; } = [];
    public List<Guid> QuestionIds { get; set; } = [];
    public int DurationMinutes { get; set; }
    public int TotalMarks { get; set; }
    public DateTime? StartDateTime { get; set; }
    public DateTime? EndDateTime { get; set; }
    public bool IsDraftSave { get; set; }
}

public class PublishQuestionBankAssessmentRequestModel
{
    public Guid? AssessmentId { get; set; }
    public string AssessmentName { get; set; } = string.Empty;
    public Guid? SubjectId { get; set; }
    public string? SubjectName { get; set; }
    public Guid? TopicId { get; set; }
    public string? TopicName { get; set; }
    public string? Instructions { get; set; }
    public bool AllowLateEntry { get; set; }
    public bool AllowQuestionReview { get; set; }
    public bool NegativeMarking { get; set; }
    public int PassingPercentage { get; set; }
    public Guid[] AssignedBatchIds { get; set; } = [];
    public List<Guid> QuestionIds { get; set; } = [];
    public int DurationMinutes { get; set; }
    public int TotalMarks { get; set; }
    public DateTime? StartDateTime { get; set; }
    public DateTime? EndDateTime { get; set; }
}

public class UpdateQuestionBankAssessmentRequestModel
{
    public string AssessmentName { get; set; } = string.Empty;
    public Guid? SubjectId { get; set; }
    public string? SubjectName { get; set; }
    public Guid? TopicId { get; set; }
    public string? TopicName { get; set; }
    public string? Instructions { get; set; }
    public bool AllowLateEntry { get; set; }
    public bool AllowQuestionReview { get; set; }
    public bool NegativeMarking { get; set; }
    public int PassingPercentage { get; set; }
    public Guid[] AssignedBatchIds { get; set; } = [];
    public List<Guid> QuestionIds { get; set; } = [];
    public int DurationMinutes { get; set; }
    public int TotalMarks { get; set; }
    public DateTime? StartDateTime { get; set; }
    public DateTime? EndDateTime { get; set; }
    public bool IsDraftSave { get; set; }
}

public class QuestionBankAssessmentResponseModel
{
    public Guid AssessmentId { get; set; }
    public Guid CollegeId { get; set; }
    public Guid? SubjectId { get; set; }
    public string? SubjectName { get; set; }
    public Guid? TopicId { get; set; }
    public string? TopicName { get; set; }
    public string AssessmentName { get; set; } = string.Empty;
    public string AssessmentType { get; set; } = string.Empty;
    public string AssessmentStatus { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public int TotalMarks { get; set; }
    public int DifficultyLevel { get; set; }
    public DateTime? StartDateTime { get; set; }
    public DateTime? EndDateTime { get; set; }
    public string? Instructions { get; set; }
    public Guid[] AssignedBatchIds { get; set; } = [];
    public bool AllowLateEntry { get; set; }
    public bool ShowResultsImmediately { get; set; }
    public int PassingPercentage { get; set; }
    public bool AllowQuestionReview { get; set; }
    public bool NegativeMarking { get; set; }
    public bool? IsTotalMarksAutoCalculated { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public List<Guid> QuestionIds { get; set; } = [];
}

public class CreateQuestionRequestModel
{
    public string Stream { get; set; } = string.Empty;
    public string? RequesterRole { get; set; }
    public Guid? SubjectId { get; set; }
    public string? Subject { get; set; }
    public Guid? TopicId { get; set; }
    public string? Topic { get; set; }
    public List<string> TopicTag { get; set; } = [];
    public string QuestionType { get; set; } = string.Empty;
    public string QuestionText { get; set; } = string.Empty;
    public List<string>? Options { get; set; }
    public string? Answer { get; set; }
    public List<string>? CorrectAnswers { get; set; }
    public string? Explanation { get; set; }
    public decimal Marks { get; set; }
    public decimal NegativeMarks { get; set; }
    public int DifficultyLevel { get; set; }
    public int? SourceRowNumber { get; set; }
}

public class DeleteQuestionsRequestModel
{
    public List<Guid> QuestionIds { get; set; } = [];
}

public class DeleteQuestionsResponseModel
{
    public List<Guid> DeletedQuestionIds { get; set; } = [];
}

public class QuestionBankSearchRequestModel
{
    public int? DifficultyLevel { get; set; }
    public Guid? SubjectId { get; set; }
    public Guid? TopicId { get; set; }
    public string? Subject { get; set; }
    public string? Topic { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class QuestionTopicCatalogResponseModel
{
    public Guid TopicId { get; set; }
    public string TopicName { get; set; } = string.Empty;
}

public class QuestionSubjectCatalogResponseModel
{
    public Guid SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public List<QuestionTopicCatalogResponseModel> Topics { get; set; } = [];
}

public class QuestionClassificationCatalogResponseModel
{
    public List<QuestionSubjectCatalogResponseModel> Subjects { get; set; } = [];
}

public class AssessmentSearchRequestModel
{
    public string? SearchTerm { get; set; }
    public string? AssessmentStatus { get; set; }
    public int? DifficultyLevel { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class AssessmentSearchItemResponseModel
{
    public Guid AssessmentId { get; set; }
    public string AssessmentName { get; set; } = string.Empty;
    public string? SubjectName { get; set; }
    public string? TopicName { get; set; }
    public string AssessmentStatus { get; set; } = string.Empty;
    public DateTime? AssessmentDate { get; set; }
    public DateTime? StartDateTime { get; set; }
    public int TotalMarks { get; set; }
    public int DifficultyLevel { get; set; }
}

public class PagedAssessmentSearchResponseModel
{
    public List<AssessmentSearchItemResponseModel> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int ActiveCount { get; set; }
    public int CompletedCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}

public class PagedQuestionBankResponseModel
{
    public List<QuestionResponseModel> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}

public class AssessmentAssignmentBatchResponseModel
{
    public string BatchId { get; set; } = string.Empty;
    public string ClassId { get; set; } = string.Empty;
    public string CollegeId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class AssessmentAssignmentClassResponseModel
{
    public string ClassId { get; set; } = string.Empty;
    public string CollegeId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? AcademicYear { get; set; }
    public List<AssessmentAssignmentBatchResponseModel> Batches { get; set; } = [];
}

public class AssessmentAssignmentCatalogResponseModel
{
    public List<AssessmentAssignmentClassResponseModel> Classes { get; set; } = [];
}

public class QuestionResponseModel
{
    public Guid QuestionId { get; set; }
    public Guid CollegeId { get; set; }
    public Guid? SubjectId { get; set; }
    public Guid? TopicId { get; set; }
    public string? Stream { get; set; }
    public string? Subject { get; set; }
    public string? Topic { get; set; }
    public List<string>? TopicTag { get; set; }
    public string QuestionType { get; set; } = string.Empty;
    public string QuestionText { get; set; } = string.Empty;
    public List<string>? Options { get; set; }
    public string? Answer { get; set; }
    public string? Explanation { get; set; }
    public decimal Marks { get; set; }
    public decimal NegativeMarks { get; set; }
    public int DifficultyLevel { get; set; }
    public int Version { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
}

public class AssessmentQuestionListRequestModel
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class AssessmentQuestionListItemResponseModel
{
    public Guid QuestionId { get; set; }
    public int DisplayOrder { get; set; }
    public string QuestionType { get; set; } = string.Empty;
    public string QuestionText { get; set; } = string.Empty;
    public List<string>? Options { get; set; }
    public decimal Marks { get; set; }
    public decimal NegativeMarks { get; set; }
    public int DifficultyLevel { get; set; }
}

public class PagedAssessmentQuestionListResponseModel
{
    public List<AssessmentQuestionListItemResponseModel> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}

public class StudentAssessmentListResponseModel
{
    public Guid AssessmentId { get; set; }
    public string AssessmentName { get; set; } = string.Empty;
    public string? SubjectName { get; set; }
    public string? TopicName { get; set; }
    public string AssessmentStatus { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public int TotalMarks { get; set; }
    public int DifficultyLevel { get; set; }
    public DateTime? StartDateTime { get; set; }
    public DateTime? EndDateTime { get; set; }
}

public class StudentAssessmentDetailResponseModel
{
    public string AssessmentName { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public int TotalMarks { get; set; }
    public int TotalQuestions { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string? Instructions { get; set; }
}

public class StudentAssessmentStartResponseModel
{
    public Guid AttemptId { get; set; }
    public Guid AssessmentId { get; set; }
    public string AttemptStatus { get; set; } = string.Empty;
    public DateTime? StartedAt { get; set; }
}

public class SaveStudentAttemptAnswerRequestModel
{
    public string? SelectedAnswer { get; set; }
    public List<string>? SelectedAnswers { get; set; }
}

public class StudentAttemptAnswerResponseModel
{
    public Guid QuestionId { get; set; }
    public string? SelectedAnswer { get; set; }
    public List<string>? SelectedAnswers { get; set; }
    public DateTime? AnsweredAt { get; set; }
}

public class StudentAttemptSubmitResponseModel
{
    public Guid AttemptId { get; set; }
    public string AttemptStatus { get; set; } = string.Empty;
    public DateTime? SubmittedAt { get; set; }
}

public class StudentAttemptRecoveryQuestionResponseModel
{
    public Guid QuestionId { get; set; }
    public int DisplayOrder { get; set; }
    public string QuestionType { get; set; } = string.Empty;
    public string QuestionText { get; set; } = string.Empty;
    public List<string>? Options { get; set; }
    public decimal Marks { get; set; }
    public decimal NegativeMarks { get; set; }
    public int DifficultyLevel { get; set; }
    public bool AllowsMultipleAnswers { get; set; }
    public string? SelectedAnswer { get; set; }
    public List<string>? SelectedAnswers { get; set; }
    public DateTime? AnsweredAt { get; set; }
}

public class StudentAttemptRecoveryResponseModel
{
    public Guid AttemptId { get; set; }
    public Guid AssessmentId { get; set; }
    public string AssessmentName { get; set; } = string.Empty;
    public string AttemptStatus { get; set; } = string.Empty;
    public DateTime? StartedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public int RemainingSeconds { get; set; }
    public int DurationMinutes { get; set; }
    public int TotalMarks { get; set; }
    public int TotalQuestions { get; set; }
    public int AttemptedQuestions { get; set; }
    public int UnansweredQuestions { get; set; }
    public string? Instructions { get; set; }
    public List<StudentAttemptRecoveryQuestionResponseModel> Questions { get; set; } = [];
}

public class StudentResultResponseModel
{
    public Guid ResultId { get; set; }
    public Guid AssessmentId { get; set; }
    public string AssessmentName { get; set; } = string.Empty;
    public Guid AttemptId { get; set; }
    public Guid StudentId { get; set; }
    public decimal TotalMarks { get; set; }
    public decimal ObtainedMarks { get; set; }
    public decimal Percentage { get; set; }
    public int Rank { get; set; }
    public string ResultStatus { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public bool HasPendingCodingEvaluation { get; set; }
}
