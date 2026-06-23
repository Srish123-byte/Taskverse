using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Taskverse.Data.DataAccess;

[Table("code_execution_results")]
public class CodeExecutionResult
{
    [Key]
    [Column("code_execution_result_id")]
    public Guid CodeExecutionResultId { get; set; } = Guid.NewGuid();

    [Required]
    [Column("code_execution_request_id")]
    public Guid CodeExecutionRequestId { get; set; }

    [Column("code_execution_result_status")]
    public short CodeExecutionResultStatusId { get; set; } = 1;

    [Column("standard_output")]
    public string? StandardOutput { get; set; }

    [Column("standard_error")]
    public string? StandardError { get; set; }

    [Column("compiler_output")]
    public string? CompilerOutput { get; set; }

    [Column("exit_code")]
    public int? ExitCode { get; set; }

    [Column("execution_time_ms")]
    public int? ExecutionTimeMs { get; set; }

    [Column("memory_used_kb")]
    public int? MemoryUsedKb { get; set; }

    [Column("total_test_cases")]
    public int? TotalTestCases { get; set; }

    [Column("passed_test_cases")]
    public int? PassedTestCases { get; set; }

    [Column("coding_score", TypeName = "numeric(5,2)")]
    public decimal? CodingScore { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("modified_at")]
    public DateTime? ModifiedAt { get; set; }

    public CodeExecutionRequest CodeExecutionRequest { get; set; } = default!;

    public LookupCodeExecutionResultStatus CodeExecutionResultStatus { get; set; } = default!;
}
