using Newtonsoft.Json;

namespace Taskverse.Api.MicroServices.Models;

public record AssessmentModel(
    string AssessmentId,
    string Title,
    string? Description,
    string Type,
    string? ExamId,
    List<string>? ChallengeIds,
    List<string> AssignedTo,
    DateTime? DueDate,
    bool IsActive,
    string CreatedBy,
    DateTime CreatedAt);

public record AssessmentResultModel(
    string ResultId,
    string AssessmentId,
    string UserId,
    string Status,
    int? Score,
    DateTime? CompletedAt,
    ExamResultModel? ExamResult,
    List<CodeExecutionResultModel>? CodingResults);

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

public record AssessmentSummaryModel(
    string AssessmentId,
    string Title,
    int TotalAssigned,
    int TotalCompleted,
    double? AverageScore);

public record CreateQuestionModel(
    Guid CollegeId,
    string CreatedBy,
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
    int DifficultyLevel);

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
