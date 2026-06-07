using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Taskverse.Data.Enums;

namespace Taskverse.Data.DataAccess;

[Table("proctoring_events")]
public class ProctoringEvent
{
    [Key]
    [Column("proctoring_event_id")]
    public Guid ProctoringEventId { get; set; } = Guid.NewGuid();

    [Required]
    [Column("proctoring_session_id")]
    public Guid ProctoringSessionId { get; set; }

    [Required]
    [Column("attempt_id")]
    public Guid AttemptId { get; set; }

    [Column("assessment_id")]
    public Guid? AssessmentId { get; set; }

    [Required]
    [Column("student_id")]
    public Guid StudentId { get; set; }

    [Column("event_type")]
    public EventType EventType { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("severity")]
    public string Severity { get; set; } = default!;

    [Column("client_timestamp")]
    public DateTime? ClientTimestamp { get; set; }

    [Column("server_received_at")]
    public DateTime ServerReceivedAt { get; set; } = DateTime.UtcNow;

    [Column("question_id")]
    public Guid? QuestionId { get; set; }

    [Column("metadata_json", TypeName = "jsonb")]
    public string? MetadataJson { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("modified_at")]
    public DateTime? ModifiedAt { get; set; }

    public ProctoringSession ProctoringSession { get; set; } = default!;

    public Attempt Attempt { get; set; } = default!;

    public Assessment? Assessment { get; set; }

    public Student Student { get; set; } = default!;

    public Question? Question { get; set; }
}
