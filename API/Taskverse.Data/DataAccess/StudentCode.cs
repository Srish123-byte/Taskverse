using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Taskverse.Data.DataAccess;

[Table("student_code")]
public class StudentCode
{
    [Key]
    [Column("student_code_id")]
    public Guid StudentCodeId { get; set; } = Guid.NewGuid();

    [Required]
    [Column("student_id")]
    public Guid StudentId { get; set; }

    [Required]
    [Column("assessment_id")]
    public Guid AssessmentId { get; set; }

    [Required]
    [Column("coding_language_id")]
    public Guid CodingLanguageId { get; set; }

    [Required]
    [Column("code")]
    public string Code { get; set; } = default!;

    [Column("coding_question_id")]
    public Guid? CodingQuestionId { get; set; }

    [Column("last_saved_at")]
    public DateTime LastSavedAt { get; set; } = DateTime.UtcNow;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("modified_at")]
    public DateTime? ModifiedAt { get; set; }

    public Student Student { get; set; } = default!;

    public Assessment Assessment { get; set; } = default!;

    public CodingLanguage CodingLanguage { get; set; } = default!;

    public CodingQuestion? CodingQuestion { get; set; }
}
