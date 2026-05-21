using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Taskverse.Business.Enums;

namespace Taskverse.Data.DataAccess;

[Table("attempts")]
public class Attempt
{
    [Key]
    [Column("attempt_id")]
    public Guid AttemptId { get; set; } = Guid.NewGuid();

    [Required]
    [Column("assessment_id")]
    public Guid AssessmentId { get; set; }

    [Required]
    [Column("student_id")]
    public Guid StudentId { get; set; }

    [Column("started_at")]
    public DateTime? StartedAt { get; set; }

    [Column("submitted_at")]
    public DateTime? SubmittedAt { get; set; }

    [Column("attempt_status")]
    public AttemptStatus AttemptStatus { get; set; }

    [Column("total_questions")]
    public int TotalQuestions { get; set; }

    [Column("attempted_questions")]
    public int AttemptedQuestions { get; set; }

    [Column("correct_answers")]
    public int CorrectAnswers { get; set; }

    [Column("wrong_answers")]
    public int WrongAnswers { get; set; }

    [Column("unanswered_questions")]
    public int UnansweredQuestions { get; set; }

    [Column("total_score", TypeName = "numeric(6,2)")]
    public decimal TotalScore { get; set; }

    [Column("percentage", TypeName = "numeric(5,2)")]
    public decimal Percentage { get; set; }

    [Column("time_taken_seconds")]
    public int TimeTakenSeconds { get; set; }

    [Column("is_passed")]
    public bool IsPassed { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
