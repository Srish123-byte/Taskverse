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

    [JsonPropertyName("assessment_name")]
    public string AssessmentName { get; set; } = string.Empty;

    [JsonPropertyName("subject_ids")]
    public Guid[] SubjectIds { get; set; } = [];

    [JsonPropertyName("topic_ids")]
    public Guid[] TopicIds { get; set; } = [];

    [MaxLength(2000)]
    [JsonPropertyName("instructions")]
    public string? Instructions { get; set; }

    [JsonPropertyName("allow_late_entry")]
    public bool AllowLateEntry { get; set; }

    [JsonPropertyName("allow_question_review")]
    public bool AllowQuestionReview { get; set; }

    [JsonPropertyName("negative_marking")]
    public bool NegativeMarking { get; set; }

    [Range(0, 100)]
    [JsonPropertyName("passing_percentage")]
    public int PassingPercentage { get; set; } = 50;

    [JsonPropertyName("assigned_batch_ids")]
    public Guid[] AssignedBatchIds { get; set; } = [];

    [JsonPropertyName("question_ids")]
    public List<Guid> QuestionIds { get; set; } = [];

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

public class PublishAssessmentRequest
{
    [JsonPropertyName("assessment_id")]
    public Guid? AssessmentId { get; set; }

    [JsonPropertyName("college_id")]
    public Guid CollegeId { get; set; }

    [MaxLength(200)]
    [JsonPropertyName("created_by")]
    public string CreatedBy { get; set; } = string.Empty;

    [MaxLength(120)]
    [JsonPropertyName("assessment_name")]
    public string AssessmentName { get; set; } = string.Empty;

    [JsonPropertyName("subject_ids")]
    public Guid[] SubjectIds { get; set; } = [];

    [JsonPropertyName("topic_ids")]
    public Guid[] TopicIds { get; set; } = [];

    [MaxLength(2000)]
    [JsonPropertyName("instructions")]
    public string? Instructions { get; set; }

    [JsonPropertyName("allow_late_entry")]
    public bool AllowLateEntry { get; set; }

    [JsonPropertyName("allow_question_review")]
    public bool AllowQuestionReview { get; set; }

    [JsonPropertyName("negative_marking")]
    public bool NegativeMarking { get; set; }

    [Range(0, 100)]
    [JsonPropertyName("passing_percentage")]
    public int PassingPercentage { get; set; } = 50;

    [JsonPropertyName("assigned_batch_ids")]
    public Guid[] AssignedBatchIds { get; set; } = [];

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

public class UpdateAssessmentRequest
{
    [Required]
    [JsonPropertyName("assessment_id")]
    public Guid AssessmentId { get; set; }

    [Required]
    [JsonPropertyName("college_id")]
    public Guid CollegeId { get; set; }

    [Required]
    [MaxLength(200)]
    [JsonPropertyName("updated_by")]
    public string UpdatedBy { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    [JsonPropertyName("requester_role")]
    public string RequesterRole { get; set; } = string.Empty;

    [JsonPropertyName("assessment_name")]
    public string AssessmentName { get; set; } = string.Empty;

    [JsonPropertyName("subject_ids")]
    public Guid[] SubjectIds { get; set; } = [];

    [JsonPropertyName("topic_ids")]
    public Guid[] TopicIds { get; set; } = [];

    [MaxLength(2000)]
    [JsonPropertyName("instructions")]
    public string? Instructions { get; set; }

    [JsonPropertyName("allow_late_entry")]
    public bool AllowLateEntry { get; set; }

    [JsonPropertyName("allow_question_review")]
    public bool AllowQuestionReview { get; set; }

    [JsonPropertyName("negative_marking")]
    public bool NegativeMarking { get; set; }

    [Range(0, 100)]
    [JsonPropertyName("passing_percentage")]
    public int PassingPercentage { get; set; } = 50;

    [JsonPropertyName("assigned_batch_ids")]
    public Guid[] AssignedBatchIds { get; set; } = [];

    [JsonPropertyName("question_ids")]
    public List<Guid> QuestionIds { get; set; } = [];

    [JsonPropertyName("duration_minutes")]
    public int DurationMinutes { get; set; }

    [Range(0, int.MaxValue)]
    [JsonPropertyName("total_marks")]
    public int TotalMarks { get; set; }

    [JsonPropertyName("start_datetime")]
    public DateTime? StartDateTime { get; set; }

    [JsonPropertyName("end_datetime")]
    public DateTime? EndDateTime { get; set; }

    [JsonPropertyName("is_draft_save")]
    public bool IsDraftSave { get; set; }
}

public class DeleteAssessmentRequest
{
    [Required]
    [JsonPropertyName("assessment_id")]
    public Guid AssessmentId { get; set; }

    [JsonPropertyName("is_deleted")]
    public bool? IsDeleted { get; set; }

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
    [property: JsonPropertyName("subject_ids")]
    Guid[] SubjectIds,
    [property: JsonPropertyName("subject_names")]
    string[] SubjectNames,
    [property: JsonPropertyName("subject_name")]
    string? SubjectName,
    [property: JsonPropertyName("subject_display_label")]
    string? SubjectDisplayLabel,
    [property: JsonPropertyName("topic_ids")]
    Guid[] TopicIds,
    [property: JsonPropertyName("topic_names")]
    string[] TopicNames,
    [property: JsonPropertyName("topic_name")]
    string? TopicName,
    [property: JsonPropertyName("topic_display_label")]
    string? TopicDisplayLabel,
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
    [property: JsonPropertyName("passing_percentage")]
    int PassingPercentage,
    [property: JsonPropertyName("allow_question_review")]
    bool AllowQuestionReview,
    [property: JsonPropertyName("negative_marking")]
    bool NegativeMarking,
    [property: JsonPropertyName("is_total_marks_auto_calculated")]
    bool? IsTotalMarksAutoCalculated,
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

public class AssessmentSearchRequest
{
    [Required]
    [JsonPropertyName("college_id")]
    public Guid CollegeId { get; set; }

    [Required]
    [MaxLength(50)]
    [JsonPropertyName("requester_role")]
    public string RequesterRole { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    [JsonPropertyName("requester_name")]
    public string RequesterName { get; set; } = string.Empty;

    [MaxLength(200)]
    [JsonPropertyName("search_term")]
    public string? SearchTerm { get; set; }

    [MaxLength(50)]
    [JsonPropertyName("assessment_status")]
    public string? AssessmentStatus { get; set; }

    [JsonPropertyName("difficulty_level")]
    public int? DifficultyLevel { get; set; }

    [JsonPropertyName("page_number")]
    public int PageNumber { get; set; } = 1;

    [JsonPropertyName("page_size")]
    public int PageSize { get; set; } = 10;
}

public record AssessmentSearchItemRecord(
    [property: JsonPropertyName("assessment_id")]
    Guid AssessmentId,
    [property: JsonPropertyName("assessment_name")]
    string AssessmentName,
    [property: JsonPropertyName("subject_ids")]
    Guid[] SubjectIds,
    [property: JsonPropertyName("subject_names")]
    string[] SubjectNames,
    [property: JsonPropertyName("subject_name")]
    string? SubjectName,
    [property: JsonPropertyName("subject_display_label")]
    string? SubjectDisplayLabel,
    [property: JsonPropertyName("topic_ids")]
    Guid[] TopicIds,
    [property: JsonPropertyName("topic_names")]
    string[] TopicNames,
    [property: JsonPropertyName("topic_name")]
    string? TopicName,
    [property: JsonPropertyName("topic_display_label")]
    string? TopicDisplayLabel,
    [property: JsonPropertyName("assessment_status")]
    string AssessmentStatus,
    [property: JsonPropertyName("assessment_date")]
    DateTime? AssessmentDate,
    [property: JsonPropertyName("start_datetime")]
    DateTime? StartDateTime,
    [property: JsonPropertyName("total_marks")]
    int TotalMarks,
    [property: JsonPropertyName("difficulty_level")]
    int DifficultyLevel,
    [property: JsonPropertyName("assigned_batch_ids")]
    Guid[] AssignedBatchIds);

public record PagedAssessmentSearchRecord(
    [property: JsonPropertyName("items")]
    List<AssessmentSearchItemRecord> Items,
    [property: JsonPropertyName("total_count")]
    int TotalCount,
    [property: JsonPropertyName("active_count")]
    int ActiveCount,
    [property: JsonPropertyName("completed_count")]
    int CompletedCount,
    [property: JsonPropertyName("page_number")]
    int PageNumber,
    [property: JsonPropertyName("page_size")]
    int PageSize);

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

public class AssessmentAccessibleBatchesRequest
{
    [Required]
    [JsonPropertyName("college_id")]
    public Guid CollegeId { get; set; }

    [MaxLength(50)]
    [JsonPropertyName("requester_role")]
    public string RequesterRole { get; set; } = string.Empty;

    [JsonPropertyName("requester_user_id")]
    public Guid? RequesterUserId { get; set; }
}

public record AssessmentAssignmentBatchRecord(
    [property: JsonPropertyName("batch_id")]
    Guid BatchId,
    [property: JsonPropertyName("class_id")]
    Guid ClassId,
    [property: JsonPropertyName("college_id")]
    Guid CollegeId,
    [property: JsonPropertyName("name")]
    string Name);

public record AssessmentAssignmentClassRecord(
    [property: JsonPropertyName("class_id")]
    Guid ClassId,
    [property: JsonPropertyName("college_id")]
    Guid CollegeId,
    [property: JsonPropertyName("name")]
    string Name,
    [property: JsonPropertyName("academic_year")]
    string? AcademicYear,
    [property: JsonPropertyName("batches")]
    List<AssessmentAssignmentBatchRecord> Batches);

public record AssessmentAssignmentCatalogRecord(
    [property: JsonPropertyName("classes")]
    List<AssessmentAssignmentClassRecord> Classes);

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
    [property: JsonPropertyName("subject_name")]
    string? SubjectName,
    [property: JsonPropertyName("topic_name")]
    string? TopicName,
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

    [JsonPropertyName("selected_answers")]
    public List<string>? SelectedAnswers { get; set; }
}

public record StudentAttemptAnswerRecord(
    [property: JsonPropertyName("question_id")]
    Guid QuestionId,
    [property: JsonPropertyName("selected_answer")]
    string? SelectedAnswer,
    [property: JsonPropertyName("selected_answers")]
    List<string>? SelectedAnswers,
    [property: JsonPropertyName("answered_at")]
    DateTime? AnsweredAt);

public record StudentAttemptSubmitRecord(
    [property: JsonPropertyName("attempt_id")]
    Guid AttemptId,
    [property: JsonPropertyName("attempt_status")]
    string AttemptStatus,
    [property: JsonPropertyName("submitted_at")]
    DateTime? SubmittedAt);

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
    [property: JsonPropertyName("allows_multiple_answers")]
    bool AllowsMultipleAnswers,
    [property: JsonPropertyName("selected_answer")]
    string? SelectedAnswer,
    [property: JsonPropertyName("selected_answers")]
    List<string>? SelectedAnswers,
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

public record StudentStreakRecord(
    [property: JsonPropertyName("current_streak")]
    int CurrentStreak,
    [property: JsonPropertyName("longest_streak")]
    int LongestStreak,
    [property: JsonPropertyName("total_assessments_taken")]
    int TotalAssessmentsTaken);
