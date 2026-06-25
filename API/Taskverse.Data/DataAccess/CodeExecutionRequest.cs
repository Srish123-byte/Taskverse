using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Taskverse.Data.DataAccess;

[Table("code_execution_requests")]
public class CodeExecutionRequest
{
    [Key]
    [Column("code_execution_request_id")]
    public Guid CodeExecutionRequestId { get; set; } = Guid.NewGuid();

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

    [Column("input_payload")]
    public string? InputPayload { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("execution_mode")]
    public string ExecutionMode { get; set; } = "Run";

    [Column("code_execution_status")]
    public short CodeExecutionStatusId { get; set; } = 1;

    [Column("requested_at")]
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    [Column("started_at")]
    public DateTime? StartedAt { get; set; }

    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }

    [MaxLength(100)]
    [Column("worker_id")]
    public string? WorkerId { get; set; }

    [MaxLength(500)]
    [Column("judge0_batch_token")]
    public string? Judge0BatchToken { get; set; }

    [Column("lease_expires_at")]
    public DateTime? LeaseExpiresAt { get; set; }

    [MaxLength(100)]
    [Column("claimed_by_instance")]
    public string? ClaimedByInstance { get; set; }

    [Column("lease_heartbeat_at")]
    public DateTime? LeaseHeartbeatAt { get; set; }

    [Column("judge0_node_id")]
    public Guid? Judge0NodeId { get; set; }

    [Column("correlation_id")]
    public Guid CorrelationId { get; set; } = Guid.NewGuid();

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("modified_at")]
    public DateTime? ModifiedAt { get; set; }

    public Student Student { get; set; } = default!;

    public Assessment Assessment { get; set; } = default!;

    public CodingLanguage CodingLanguage { get; set; } = default!;

    public LookupCodeExecutionStatus CodeExecutionStatus { get; set; } = default!;

    public CodeExecutionResult? CodeExecutionResult { get; set; }

    public Judge0Node? Judge0Node { get; set; }
}
