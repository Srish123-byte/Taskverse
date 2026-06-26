using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Taskverse.Data.Enums;

namespace Taskverse.Data.DataAccess;

[Table("proctoring_sessions")]
public class ProctoringSession
{
    [Key]
    [Column("proctoring_session_id")]
    public Guid ProctoringSessionId { get; set; } = Guid.NewGuid();

    [Required]
    [Column("attempt_id")]
    public Guid AttemptId { get; set; }

    [Column("assessment_id")]
    public Guid? AssessmentId { get; set; }

    [Required]
    [Column("student_id")]
    public Guid StudentId { get; set; }

    [Column("proctoring_status")]
    public int ProctoringStatus { get; set; }

    [Column("started_at")]
    public DateTime? StartedAt { get; set; }

    [Column("ended_at")]
    public DateTime? EndedAt { get; set; }

    [Column("last_heartbeat_at")]
    public DateTime? LastHeartbeatAt { get; set; }

    [Column("last_known_question_id")]
    public Guid? LastKnownQuestionId { get; set; }

    [Column("last_known_visibility_state")]
    public ProctoringVisibilityStatus? LastKnownVisibilityState { get; set; }

    [Column("last_known_is_fullscreen")]
    public bool? LastKnownIsFullscreen { get; set; }

    [Column("last_known_network_status")]
    public ProctoringNetworkStatus? LastKnownNetworkStatus { get; set; }

    [MaxLength(100)]
    [Column("browser_name")]
    public string? BrowserName { get; set; }

    [MaxLength(100)]
    [Column("browser_version")]
    public string? BrowserVersion { get; set; }

    [MaxLength(100)]
    [Column("operating_system")]
    public string? OperatingSystem { get; set; }

    [MaxLength(50)]
    [Column("device_type")]
    public string? DeviceType { get; set; }

    [MaxLength(100)]
    [Column("user_agent")]
    public string? UserAgent { get; set; }

    [MaxLength(100)]
    [Column("ip_address")]
    public string? IpAddress { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("modified_at")]
    public DateTime? ModifiedAt { get; set; }

    public Attempt Attempt { get; set; } = default!;

    public Assessment? Assessment { get; set; }

    public Student Student { get; set; } = default!;
}
