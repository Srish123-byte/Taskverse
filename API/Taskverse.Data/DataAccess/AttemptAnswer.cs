using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Taskverse.Data.DataAccess;

[Table("attempt_answers")]
public class AttemptAnswer
{
    [Key]
    [Column("attempt_answer_id")]
    public Guid AttemptAnswerId { get; set; } = Guid.NewGuid();

    [Required]
    [Column("attempt_id")]
    public Guid AttemptId { get; set; }

    [Required]
    [Column("question_id")]
    public Guid QuestionId { get; set; }

    [Column("selected_answer")]
    public string? SelectedAnswer { get; set; }

    [Column("is_correct")]
    public bool IsCorrect { get; set; }

    [Column("marks_awarded", TypeName = "numeric(5,2)")]
    public decimal MarksAwarded { get; set; }

    [Column("answered_at")]
    public DateTime? AnsweredAt { get; set; }
}
