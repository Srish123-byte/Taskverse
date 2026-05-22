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
