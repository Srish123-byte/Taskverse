using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Taskverse.API.Assessments.Service.Models;

public class CreateAssessmentRequest
{
    [Required]
    [JsonPropertyName("college_id")]
    public Guid CollegeId { get; set; }

    [Required]
    [MaxLength(200)]
    [JsonPropertyName("created_by")]
    public string CreatedBy { get; set; } = string.Empty;

    [Required]
    [MaxLength(120)]
    [JsonPropertyName("assessment_name")]
    public string AssessmentName { get; set; } = string.Empty;

    [JsonPropertyName("subject_id")]
    public Guid? SubjectId { get; set; }

    [MaxLength(100)]
    [JsonPropertyName("subject_name")]
    public string? SubjectName { get; set; }

    [JsonPropertyName("topic_id")]
    public Guid? TopicId { get; set; }

    [MaxLength(200)]
    [JsonPropertyName("topic_name")]
    public string? TopicName { get; set; }

    [JsonPropertyName("assigned_batch_ids")]
    public Guid[] AssignedBatchIds { get; set; } = [];

    [Required]
    [JsonPropertyName("question_ids")]
    public List<Guid> QuestionIds { get; set; } = [];

    [Range(1, int.MaxValue)]
    [JsonPropertyName("duration_minutes")]
    public int DurationMinutes { get; set; }

    [Range(0, int.MaxValue)]
    [JsonPropertyName("total_marks")]
    public int TotalMarks { get; set; }

    [JsonPropertyName("start_datetime")]
    public DateTime? StartDateTime { get; set; }

    [JsonPropertyName("end_datetime")]
    public DateTime? EndDateTime { get; set; }
}

public class DeleteAssessmentRequest
{
    [Required]
    [JsonPropertyName("assessment_id")]
    public Guid AssessmentId { get; set; }

    [Required]
    [MaxLength(200)]
    [JsonPropertyName("deleted_by")]
    public string DeletedBy { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    [JsonPropertyName("requester_role")]
    public string RequesterRole { get; set; } = string.Empty;

    [JsonPropertyName("college_id")]
    public Guid? CollegeId { get; set; }
}

public record AssessmentRecord(
    [property: JsonPropertyName("assessment_id")]
    Guid AssessmentId,
    [property: JsonPropertyName("college_id")]
    Guid CollegeId,
    [property: JsonPropertyName("subject_id")]
    Guid? SubjectId,
    [property: JsonPropertyName("subject_name")]
    string? SubjectName,
    [property: JsonPropertyName("topic_id")]
    Guid? TopicId,
    [property: JsonPropertyName("topic_name")]
    string? TopicName,
    [property: JsonPropertyName("assessment_name")]
    string AssessmentName,
    [property: JsonPropertyName("assessment_type")]
    string AssessmentType,
    [property: JsonPropertyName("assessment_status")]
    string AssessmentStatus,
    [property: JsonPropertyName("duration_minutes")]
    int DurationMinutes,
    [property: JsonPropertyName("total_marks")]
    int TotalMarks,
    [property: JsonPropertyName("difficulty_level")]
    int DifficultyLevel,
    [property: JsonPropertyName("start_datetime")]
    DateTime? StartDateTime,
    [property: JsonPropertyName("end_datetime")]
    DateTime? EndDateTime,
    [property: JsonPropertyName("instructions")]
    string? Instructions,
    [property: JsonPropertyName("assigned_batch_ids")]
    Guid[] AssignedBatchIds,
    [property: JsonPropertyName("allow_late_entry")]
    bool AllowLateEntry,
    [property: JsonPropertyName("show_results_immediately")]
    bool ShowResultsImmediately,
    [property: JsonPropertyName("allow_question_review")]
    bool AllowQuestionReview,
    [property: JsonPropertyName("negative_marking")]
    bool NegativeMarking,
    [property: JsonPropertyName("marks_per_question")]
    decimal MarksPerQuestion,
    [property: JsonPropertyName("is_total_marks_auto_calculated")]
    bool IsTotalMarksAutoCalculated,
    [property: JsonPropertyName("created_by")]
    string CreatedBy,
    [property: JsonPropertyName("created_at")]
    DateTime CreatedAt,
    [property: JsonPropertyName("modified_at")]
    DateTime? ModifiedAt,
    [property: JsonPropertyName("question_ids")]
    List<Guid> QuestionIds);

public class AssessmentQuestionListRequest
{
    [JsonPropertyName("page_number")]
    public int PageNumber { get; set; } = 1;

    [JsonPropertyName("page_size")]
    public int PageSize { get; set; } = 10;
}

public record AssessmentQuestionListItemRecord(
    [property: JsonPropertyName("question_id")]
    Guid QuestionId,
    [property: JsonPropertyName("display_order")]
    int DisplayOrder,
    [property: JsonPropertyName("question_type")]
    string QuestionType,
    [property: JsonPropertyName("question_text")]
    string QuestionText,
    [property: JsonPropertyName("options")]
    List<string>? Options,
    [property: JsonPropertyName("marks")]
    decimal Marks,
    [property: JsonPropertyName("negative_marks")]
    decimal NegativeMarks,
    [property: JsonPropertyName("difficulty_level")]
    int DifficultyLevel);

public record PagedAssessmentQuestionListRecord(
    [property: JsonPropertyName("items")]
    List<AssessmentQuestionListItemRecord> Items,
    [property: JsonPropertyName("total_count")]
    int TotalCount,
    [property: JsonPropertyName("page_number")]
    int PageNumber,
    [property: JsonPropertyName("page_size")]
    int PageSize);

public class StudentAssessmentListRequest
{
    [Required]
    [JsonPropertyName("student_user_id")]
    public Guid StudentUserId { get; set; }
}

public record StudentAssessmentListItemRecord(
    [property: JsonPropertyName("assessment_id")]
    Guid AssessmentId,
    [property: JsonPropertyName("assessment_name")]
    string AssessmentName,
    [property: JsonPropertyName("assessment_status")]
    string AssessmentStatus,
    [property: JsonPropertyName("duration_minutes")]
    int DurationMinutes,
    [property: JsonPropertyName("total_marks")]
    int TotalMarks,
    [property: JsonPropertyName("difficulty_level")]
    int DifficultyLevel,
    [property: JsonPropertyName("start_datetime")]
    DateTime? StartDateTime,
    [property: JsonPropertyName("end_datetime")]
    DateTime? EndDateTime);

public record StudentAssessmentDetailRecord(
    [property: JsonPropertyName("assessment_name")]
    string AssessmentName,
    [property: JsonPropertyName("duration_minutes")]
    int DurationMinutes,
    [property: JsonPropertyName("total_marks")]
    int TotalMarks,
    [property: JsonPropertyName("total_questions")]
    int TotalQuestions,
    [property: JsonPropertyName("start_time")]
    DateTime? StartTime,
    [property: JsonPropertyName("end_time")]
    DateTime? EndTime,
    [property: JsonPropertyName("instructions")]
    string? Instructions);

public record StudentAssessmentStartRecord(
    [property: JsonPropertyName("attempt_id")]
    Guid AttemptId,
    [property: JsonPropertyName("assessment_id")]
    Guid AssessmentId,
    [property: JsonPropertyName("attempt_status")]
    string AttemptStatus,
    [property: JsonPropertyName("started_at")]
    DateTime? StartedAt);

public class SaveStudentAttemptAnswerRequest
{
    [JsonPropertyName("selected_answer")]
    public string? SelectedAnswer { get; set; }
}

public record StudentAttemptAnswerRecord(
    [property: JsonPropertyName("question_id")]
    Guid QuestionId,
    [property: JsonPropertyName("selected_answer")]
    string? SelectedAnswer,
    [property: JsonPropertyName("answered_at")]
    DateTime? AnsweredAt);

public record StudentAttemptRecoveryQuestionRecord(
    [property: JsonPropertyName("question_id")]
    Guid QuestionId,
    [property: JsonPropertyName("display_order")]
    int DisplayOrder,
    [property: JsonPropertyName("question_type")]
    string QuestionType,
    [property: JsonPropertyName("question_text")]
    string QuestionText,
    [property: JsonPropertyName("options")]
    List<string>? Options,
    [property: JsonPropertyName("marks")]
    decimal Marks,
    [property: JsonPropertyName("negative_marks")]
    decimal NegativeMarks,
    [property: JsonPropertyName("difficulty_level")]
    int DifficultyLevel,
    [property: JsonPropertyName("selected_answer")]
    string? SelectedAnswer,
    [property: JsonPropertyName("answered_at")]
    DateTime? AnsweredAt);

public record StudentAttemptRecoveryRecord(
    [property: JsonPropertyName("attempt_id")]
    Guid AttemptId,
    [property: JsonPropertyName("assessment_id")]
    Guid AssessmentId,
    [property: JsonPropertyName("assessment_name")]
    string AssessmentName,
    [property: JsonPropertyName("attempt_status")]
    string AttemptStatus,
    [property: JsonPropertyName("started_at")]
    DateTime? StartedAt,
    [property: JsonPropertyName("submitted_at")]
    DateTime? SubmittedAt,
    [property: JsonPropertyName("expires_at")]
    DateTime? ExpiresAt,
    [property: JsonPropertyName("remaining_seconds")]
    int RemainingSeconds,
    [property: JsonPropertyName("duration_minutes")]
    int DurationMinutes,
    [property: JsonPropertyName("total_marks")]
    int TotalMarks,
    [property: JsonPropertyName("total_questions")]
    int TotalQuestions,
    [property: JsonPropertyName("attempted_questions")]
    int AttemptedQuestions,
    [property: JsonPropertyName("unanswered_questions")]
    int UnansweredQuestions,
    [property: JsonPropertyName("instructions")]
    string? Instructions,
    [property: JsonPropertyName("questions")]
    List<StudentAttemptRecoveryQuestionRecord> Questions);
