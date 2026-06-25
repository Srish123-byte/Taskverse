using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Taskverse.Data.DataAccess;

[Table("code_execution_submissions")]
public class CodeExecutionSubmission
{
    [Key]
    [Column("submission_id")]
    public Guid SubmissionId { get; set; } = Guid.NewGuid();

    [Required]
    [Column("code_execution_request_id")]
    public Guid CodeExecutionRequestId { get; set; }

    [Required]
    [Column("test_case_id")]
    public Guid TestCaseId { get; set; }

    [Column("coding_language_id")]
    public Guid? CodingLanguageId { get; set; }

    [MaxLength(100)]
    [Column("judge0_token")]
    public string? Judge0Token { get; set; }

    [Column("judge0_status_id")]
    public short? Judge0StatusId { get; set; }

    [MaxLength(50)]
    [Column("judge0_status_description")]
    public string? Judge0StatusDescription { get; set; }

    [Column("judge0_submitted_at")]
    public DateTime? Judge0SubmittedAt { get; set; }

    [Column("judge0_completed_at")]
    public DateTime? Judge0CompletedAt { get; set; }

    [Column("stdout")]
    public string? Stdout { get; set; }

    [Column("stderr")]
    public string? Stderr { get; set; }

    [Column("compile_output")]
    public string? CompileOutput { get; set; }

    [Column("exit_code")]
    public int? ExitCode { get; set; }

    [Column("time_seconds", TypeName = "numeric(10,4)")]
    public decimal? TimeSeconds { get; set; }

    [Column("memory_kilobytes")]
    public int? MemoryKilobytes { get; set; }

    [Column("passed")]
    public bool Passed { get; set; }

    [Column("actual_output")]
    public string? ActualOutput { get; set; }

    [Column("execution_time_ms")]
    public int? ExecutionTimeMs { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("modified_at")]
    public DateTime? ModifiedAt { get; set; }

    public CodeExecutionRequest CodeExecutionRequest { get; set; } = default!;

    public TestCase TestCase { get; set; } = default!;

    public CodingLanguage? CodingLanguage { get; set; }
}
