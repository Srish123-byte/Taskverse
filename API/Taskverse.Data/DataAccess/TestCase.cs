using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Taskverse.Data.DataAccess;

[Table("test_cases")]
public class TestCase
{
    [Key]
    [Column("test_case_id")]
    public Guid TestCaseId { get; set; } = Guid.NewGuid();

    [Required]
    [Column("coding_question_id")]
    public Guid CodingQuestionId { get; set; }

    [Required]
    [MaxLength(30)]
    [Column("input_format")]
    public string InputFormat { get; set; } = "stdin";

    [Column("input_data")]
    public string? InputData { get; set; }

    [Column("expected_output")]
    public string? ExpectedOutput { get; set; }

    [Column("comparison_mode")]
    public int ComparisonMode { get; set; } = 2;

    [Column("numeric_tolerance", TypeName = "numeric(18,4)")]
    public decimal? NumericTolerance { get; set; }

    [Column("is_hidden")]
    public bool IsHidden { get; set; }

    [Column("is_sample")]
    public bool IsSample { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("time_limit_ms")]
    public int? TimeLimitMs { get; set; }

    [Column("memory_limit_kb")]
    public int? MemoryLimitKb { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("modified_at")]
    public DateTime? ModifiedAt { get; set; }

    public CodingQuestion CodingQuestion { get; set; } = default!;
}
