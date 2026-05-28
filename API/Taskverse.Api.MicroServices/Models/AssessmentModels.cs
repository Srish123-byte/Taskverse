using Newtonsoft.Json;

namespace Taskverse.Api.MicroServices.Models;

public record CreateQuestionBankAssessmentModel(
    [property: JsonProperty("college_id")]
    Guid CollegeId,
    [property: JsonProperty("created_by")]
    string CreatedBy,
    [property: JsonProperty("assessment_name")]
    string AssessmentName,
    [property: JsonProperty("subject_id")]
    Guid? SubjectId,
    [property: JsonProperty("subject_name")]
    string? SubjectName,
    [property: JsonProperty("topic_id")]
    Guid? TopicId,
    [property: JsonProperty("topic_name")]
    string? TopicName,
    [property: JsonProperty("assigned_batch_ids")]
    Guid[] AssignedBatchIds,
    [property: JsonProperty("question_ids")]
    List<Guid> QuestionIds,
    [property: JsonProperty("duration_minutes")]
    int DurationMinutes,
    [property: JsonProperty("total_marks")]
    int TotalMarks,
    [property: JsonProperty("start_datetime")]
    DateTime? StartDateTime,
    [property: JsonProperty("end_datetime")]
    DateTime? EndDateTime);

public record DeleteAssessmentModel(
    [property: JsonProperty("assessment_id")]
    Guid AssessmentId,
    [property: JsonProperty("deleted_by")]
    string DeletedBy,
    [property: JsonProperty("requester_role")]
    string RequesterRole,
    [property: JsonProperty("college_id")]
    Guid? CollegeId);

public record QuestionBankAssessmentModel(
    [property: JsonProperty("assessment_id")]
    Guid AssessmentId,
    [property: JsonProperty("college_id")]
    Guid CollegeId,
    [property: JsonProperty("subject_id")]
    Guid? SubjectId,
    [property: JsonProperty("subject_name")]
    string? SubjectName,
    [property: JsonProperty("topic_id")]
    Guid? TopicId,
    [property: JsonProperty("topic_name")]
    string? TopicName,
    [property: JsonProperty("assessment_name")]
    string AssessmentName,
    [property: JsonProperty("assessment_type")]
    string AssessmentType,
    [property: JsonProperty("assessment_status")]
    string AssessmentStatus,
    [property: JsonProperty("duration_minutes")]
    int DurationMinutes,
    [property: JsonProperty("total_marks")]
    int TotalMarks,
    [property: JsonProperty("difficulty_level")]
    int DifficultyLevel,
    [property: JsonProperty("start_datetime")]
    DateTime? StartDateTime,
    [property: JsonProperty("end_datetime")]
    DateTime? EndDateTime,
    [property: JsonProperty("instructions")]
    string? Instructions,
    [property: JsonProperty("assigned_batch_ids")]
    Guid[] AssignedBatchIds,
    [property: JsonProperty("allow_late_entry")]
    bool AllowLateEntry,
    [property: JsonProperty("show_results_immediately")]
    bool ShowResultsImmediately,
    [property: JsonProperty("allow_question_review")]
    bool AllowQuestionReview,
    [property: JsonProperty("negative_marking")]
    bool NegativeMarking,
    [property: JsonProperty("marks_per_question")]
    decimal MarksPerQuestion,
    [property: JsonProperty("is_total_marks_auto_calculated")]
    bool IsTotalMarksAutoCalculated,
    [property: JsonProperty("created_by")]
    string CreatedBy,
    [property: JsonProperty("created_at")]
    DateTime CreatedAt,
    [property: JsonProperty("modified_at")]
    DateTime? ModifiedAt,
    [property: JsonProperty("question_ids")]
    List<Guid> QuestionIds);

public record CreateQuestionModel(
    Guid CollegeId,
    string CreatedBy,
    string RequesterRole,
    string Stream,
    Guid? SubjectId,
    string? Subject,
    Guid? TopicId,
    string? Topic,
    string TopicTag,
    string QuestionType,
    string QuestionText,
    List<string>? Options,
    string Answer,
    string? Explanation,
    decimal Marks,
    decimal NegativeMarks,
    int DifficultyLevel,
    int? SourceRowNumber);

public record DeleteQuestionsModel(
    string CreatedBy,
    List<Guid> QuestionIds);

public record QuestionBankSearchModel(
    Guid CollegeId,
    int? DifficultyLevel,
    Guid? SubjectId,
    Guid? TopicId,
    string? Subject,
    string? Topic,
    int PageNumber = 1,
    int PageSize = 10);

public record PagedQuestionBankModel(
    List<AssessmentQuestionModel> Items,
    int TotalCount,
    int PageNumber,
    int PageSize);

public record AssessmentQuestionModel(
    Guid QuestionId,
    Guid CollegeId,
    Guid? SubjectId,
    Guid? TopicId,
    string? Stream,
    string? Subject,
    string? Topic,
    string? TopicTag,
    string QuestionType,
    string QuestionText,
    List<string>? Options,
    string? Answer,
    string? Explanation,
    decimal Marks,
    decimal NegativeMarks,
    int DifficultyLevel,
    int Version,
    string CreatedBy,
    DateTime CreatedAt,
    DateTime? ModifiedAt);

public record AssessmentQuestionListSearchModel(
    [property: JsonProperty("page_number")]
    int PageNumber,
    [property: JsonProperty("page_size")]
    int PageSize);

public record StudentAssessmentListSearchModel(
    [property: JsonProperty("student_user_id")]
    Guid StudentUserId);

public record StudentAssessmentListItemModel(
    [property: JsonProperty("assessment_id")]
    Guid AssessmentId,
    [property: JsonProperty("assessment_name")]
    string AssessmentName,
    [property: JsonProperty("assessment_status")]
    string AssessmentStatus,
    [property: JsonProperty("duration_minutes")]
    int DurationMinutes,
    [property: JsonProperty("total_marks")]
    int TotalMarks,
    [property: JsonProperty("difficulty_level")]
    int DifficultyLevel,
    [property: JsonProperty("start_datetime")]
    DateTime? StartDateTime,
    [property: JsonProperty("end_datetime")]
    DateTime? EndDateTime);

public record StudentAssessmentDetailModel(
    [property: JsonProperty("assessment_name")]
    string AssessmentName,
    [property: JsonProperty("duration_minutes")]
    int DurationMinutes,
    [property: JsonProperty("total_marks")]
    int TotalMarks,
    [property: JsonProperty("total_questions")]
    int TotalQuestions,
    [property: JsonProperty("start_time")]
    DateTime? StartTime,
    [property: JsonProperty("end_time")]
    DateTime? EndTime,
    [property: JsonProperty("instructions")]
    string? Instructions);

public record StudentAssessmentStartModel(
    [property: JsonProperty("attempt_id")]
    Guid AttemptId,
    [property: JsonProperty("assessment_id")]
    Guid AssessmentId,
    [property: JsonProperty("attempt_status")]
    string AttemptStatus,
    [property: JsonProperty("started_at")]
    DateTime? StartedAt);

public record SaveStudentAttemptAnswerModel(
    [property: JsonProperty("selected_answer")]
    string? SelectedAnswer);

public record StudentAttemptAnswerModel(
    [property: JsonProperty("question_id")]
    Guid QuestionId,
    [property: JsonProperty("selected_answer")]
    string? SelectedAnswer,
    [property: JsonProperty("answered_at")]
    DateTime? AnsweredAt);

public record StudentAttemptSubmitModel(
    [property: JsonProperty("attempt_id")]
    Guid AttemptId,
    [property: JsonProperty("attempt_status")]
    string AttemptStatus,
    [property: JsonProperty("submitted_at")]
    DateTime? SubmittedAt);

public record StudentAttemptRecoveryQuestionModel(
    [property: JsonProperty("question_id")]
    Guid QuestionId,
    [property: JsonProperty("display_order")]
    int DisplayOrder,
    [property: JsonProperty("question_type")]
    string QuestionType,
    [property: JsonProperty("question_text")]
    string QuestionText,
    [property: JsonProperty("options")]
    List<string>? Options,
    [property: JsonProperty("marks")]
    decimal Marks,
    [property: JsonProperty("negative_marks")]
    decimal NegativeMarks,
    [property: JsonProperty("difficulty_level")]
    int DifficultyLevel,
    [property: JsonProperty("selected_answer")]
    string? SelectedAnswer,
    [property: JsonProperty("answered_at")]
    DateTime? AnsweredAt);

public record StudentAttemptRecoveryModel(
    [property: JsonProperty("attempt_id")]
    Guid AttemptId,
    [property: JsonProperty("assessment_id")]
    Guid AssessmentId,
    [property: JsonProperty("assessment_name")]
    string AssessmentName,
    [property: JsonProperty("attempt_status")]
    string AttemptStatus,
    [property: JsonProperty("started_at")]
    DateTime? StartedAt,
    [property: JsonProperty("submitted_at")]
    DateTime? SubmittedAt,
    [property: JsonProperty("expires_at")]
    DateTime? ExpiresAt,
    [property: JsonProperty("remaining_seconds")]
    int RemainingSeconds,
    [property: JsonProperty("duration_minutes")]
    int DurationMinutes,
    [property: JsonProperty("total_marks")]
    int TotalMarks,
    [property: JsonProperty("total_questions")]
    int TotalQuestions,
    [property: JsonProperty("attempted_questions")]
    int AttemptedQuestions,
    [property: JsonProperty("unanswered_questions")]
    int UnansweredQuestions,
    [property: JsonProperty("instructions")]
    string? Instructions,
    [property: JsonProperty("questions")]
    List<StudentAttemptRecoveryQuestionModel> Questions);

public record AssessmentQuestionListItemModel(
    [property: JsonProperty("question_id")]
    Guid QuestionId,
    [property: JsonProperty("display_order")]
    int DisplayOrder,
    [property: JsonProperty("question_type")]
    string QuestionType,
    [property: JsonProperty("question_text")]
    string QuestionText,
    [property: JsonProperty("options")]
    List<string>? Options,
    [property: JsonProperty("marks")]
    decimal Marks,
    [property: JsonProperty("negative_marks")]
    decimal NegativeMarks,
    [property: JsonProperty("difficulty_level")]
    int DifficultyLevel);

public record PagedAssessmentQuestionListModel(
    [property: JsonProperty("items")]
    List<AssessmentQuestionListItemModel> Items,
    [property: JsonProperty("total_count")]
    int TotalCount,
    [property: JsonProperty("page_number")]
    int PageNumber,
    [property: JsonProperty("page_size")]
    int PageSize);
