using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Taskverse.Data.DataAccess;

[Table("questions")]
public class Question
{
    [Key]
    [Column("question_id")]
    public Guid QuestionId { get; set; } = Guid.NewGuid();

    [Required]
    [Column("college_id")]
    public Guid CollegeId { get; set; }

    [Column("assessment_id")]
    public Guid? AssessmentId { get; set; }

    [MaxLength(100)]
    [Column("stream")]
    public string? Stream { get; set; }

    [MaxLength(100)]
    [Column("subject")]
    public string? Subject { get; set; }

    [MaxLength(200)]
    [Column("topic")]
    public string? Topic { get; set; }

    [MaxLength(200)]
    [Column("topic_tag")]
    public string? TopicTag { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("question_type")]
    public string QuestionType { get; set; } = default!;

    [Required]
    [Column("question_text")]
    public string QuestionText { get; set; } = default!;

    [Column("options", TypeName = "jsonb")]
    public string? Options { get; set; }

    [Column("answer")]
    public string? Answer { get; set; }

    [MaxLength(1000)]
    [Column("explanation")]
    public string? Explanation { get; set; }

    [Column("marks", TypeName = "numeric(5,2)")]
    public decimal Marks { get; set; }

    [Column("negative_marks", TypeName = "numeric(5,2)")]
    public decimal NegativeMarks { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; }

    [Required]
    [MaxLength(200)]
    [Column("created_by")]
    public string CreatedBy { get; set; } = default!;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("modified_at")]
    public DateTime? ModifiedAt { get; set; }

    [Column("difficulty_level")]
    public int DifficultyLevel { get; set; }

    [Column("version")]
    public int Version { get; set; }

    [NotMapped]
    public Guid? SubjectId { get; set; }

    [NotMapped]
    public Guid? TopicId { get; set; }
}
