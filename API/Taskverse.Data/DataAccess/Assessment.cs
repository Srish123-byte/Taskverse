using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Taskverse.Data.Enums;

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

    [Column("passing_percentage")]
    public int PassingPercentage { get; set; } = 50;

    [Column("allow_question_review")]
    public bool AllowQuestionReview { get; set; }

    [Column("negative_marking")]
    public bool NegativeMarking { get; set; }

    [Column("is_total_marks_auto_calculated")]
    public bool? IsTotalMarksAutoCalculated { get; set; }

    [Required]
    [MaxLength(200)]
    [Column("created_by")]
    public string CreatedBy { get; set; } = default!;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("modified_at")]
    public DateTime? ModifiedAt { get; set; }

    [Column("is_deleted")]
    public bool? IsDeleted { get; set; }

    [Column("soft_deleted_at")]
    public DateTime? SoftDeletedAt { get; set; }

    [MaxLength(200)]
    [Column("soft_deleted_by")]
    public string? SoftDeletedBy { get; set; }

    [NotMapped]
    public string? SubjectName { get; set; }

    [NotMapped]
    public string? TopicName { get; set; }

    public Subject? Subject { get; set; }

    public Topic? Topic { get; set; }

    public ICollection<AssessmentQuestion> AssessmentQuestions { get; set; } = [];
}
