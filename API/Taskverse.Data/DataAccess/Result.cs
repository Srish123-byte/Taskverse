using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Taskverse.Data.Enums;

namespace Taskverse.Data.DataAccess;

[Table("results")]
public class Result
{
    [Key]
    [Column("result_id")]
    public Guid ResultId { get; set; } = Guid.NewGuid();

    [Required]
    [Column("assessment_id")]
    public Guid AssessmentId { get; set; }

    [Required]
    [Column("attempt_id")]
    public Guid AttemptId { get; set; }

    [Required]
    [Column("student_id")]
    public Guid StudentId { get; set; }

    [Column("total_marks", TypeName = "numeric(6,2)")]
    public decimal TotalMarks { get; set; }

    [Column("obtained_marks", TypeName = "numeric(6,2)")]
    public decimal ObtainedMarks { get; set; }

    [Column("percentage", TypeName = "numeric(5,2)")]
    public decimal Percentage { get; set; }

    [Column("rank")]
    public int Rank { get; set; }

    [Column("result_status")]
    public ResultStatus ResultStatus { get; set; }

    [Column("generated_at")]
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
