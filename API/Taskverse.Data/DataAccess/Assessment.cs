using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Taskverse.Business.Enums;

namespace Taskverse.Data.DataAccess;

[Table("assessments")]
public class Assessment
{
    [Key]
    [Column("assessment_id")]
    public Guid AssessmentId { get; set; } = Guid.NewGuid();

    [Required]
    [Column("college_id")]
    public Guid CollegeId { get; set; }

    [Column("subject_id")]
    public Guid? SubjectId { get; set; }

    [Column("topic_id")]
    public Guid? TopicId { get; set; }

    [Required]
    [MaxLength(120)]
    [Column("assessment_name")]
    public string AssessmentName { get; set; } = default!;

    [Column("assessment_type")]
    public AssessmentType AssessmentType { get; set; }

    [Column("assessment_status")]
    public AssessmentStatus AssessmentStatus { get; set; }

    [Column("duration_minutes")]
    public int DurationMinutes { get; set; }

    [Column("total_marks")]
    public int TotalMarks { get; set; }

    [Column("difficulty_level")]
    public int DifficultyLevel { get; set; }

    [Column("start_datetime")]
    public DateTime? StartDateTime { get; set; }

    [Column("end_datetime")]
    public DateTime? EndDateTime { get; set; }

    [MaxLength(2000)]
    [Column("instructions")]
    public string? Instructions { get; set; }

    [Column("assigned_batch_ids", TypeName = "uuid[]")]
    public Guid[] AssignedBatchIds { get; set; } = [];

    [Column("allow_late_entry")]
    public bool AllowLateEntry { get; set; }

    [Column("show_results_immediately")]
    public bool ShowResultsImmediately { get; set; }

    [Column("allow_question_review")]
    public bool AllowQuestionReview { get; set; }

    [Column("negative_marking")]
    public bool NegativeMarking { get; set; }

    [Column("marks_per_question", TypeName = "numeric(6,2)")]
    public decimal MarksPerQuestion { get; set; }

    [Column("is_total_marks_auto_calculated")]
    public bool IsTotalMarksAutoCalculated { get; set; }

    [Required]
    [Column("created_by")]
    public Guid CreatedBy { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("modified_at")]
    public DateTime? ModifiedAt { get; set; }

    public ICollection<AssessmentQuestion> AssessmentQuestions { get; set; } = [];
}
