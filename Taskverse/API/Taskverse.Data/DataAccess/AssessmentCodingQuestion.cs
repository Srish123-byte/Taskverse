using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Taskverse.Data.DataAccess;

[Table("assessment_coding_questions")]
public class AssessmentCodingQuestion
{
    [Key]
    [Column("assessment_coding_question_id")]
    public Guid AssessmentCodingQuestionId { get; set; } = Guid.NewGuid();

    [Required]
    [Column("assessment_id")]
    public Guid AssessmentId { get; set; }

    [Required]
    [Column("coding_question_id")]
    public Guid CodingQuestionId { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("modified_at")]
    public DateTime? ModifiedAt { get; set; } = DateTime.UtcNow;

    public Assessment Assessment { get; set; } = default!;

    public CodingQuestion CodingQuestion { get; set; } = default!;
}
