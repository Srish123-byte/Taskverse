using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Taskverse.Data.DataAccess;

[Table("assessment_questions")]
public class AssessmentQuestion
{
    [Key]
    [Column("assessment_questions_id")]
    public Guid AssessmentQuestionId { get; set; } = Guid.NewGuid();

    [Required]
    [Column("assessment_id")]
    public Guid AssessmentId { get; set; }

    [Required]
    [Column("question_id")]
    public Guid QuestionId { get; set; }

    [Column("display_order")]
    public int DisplayOrder { get; set; }

    [Column("marks", TypeName = "numeric(5,2)")]
    public decimal Marks { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("modified_at")]
    public DateTime? ModifiedAt { get; set; }

    public Assessment? Assessment { get; set; }
}
