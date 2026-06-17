using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Taskverse.Data.Enums;

namespace Taskverse.Data.DataAccess;

[Table("proctoring_violation_summaries")]
public class ProctoringViolationSummary
{
    [Key]
    [Column("proctoring_violation_summary_id")]
    public Guid ProctoringViolationSummaryId { get; set; } = Guid.NewGuid();

    [Required]
    [Column("attempt_id")]
    public Guid AttemptId { get; set; }

    [Required]
    [Column("proctoring_session_id")]
    public Guid ProctoringSessionId { get; set; }

    [Column("tab_switch_count")]
    public int TabSwitchCount { get; set; }

    [Column("full_screen_exit_count")]
    public int FullScreenExitCount { get; set; }

    [Column("copy_attempt_count")]
    public int CopyAttemptCount { get; set; }

    [Column("paste_attempt_count")]
    public int PasteAttemptCount { get; set; }

    [Column("cut_attempt_count")]
    public int CutAttemptCount { get; set; }

    [Column("context_menu_attempt_count")]
    public int ContextMenuAttemptCount { get; set; }

    [Column("blocked_shortcut_count")]
    public int BlockedShortcutCount { get; set; }

    [Column("possible_devtools_count")]
    public int PossibleDevtoolsCount { get; set; }

    [Column("network_disconnect_count")]
    public int NetworkDisconnectCount { get; set; }

    [Column("risk_score")]
    public int RiskScore { get; set; }

    [Column("risk_level")]
    public RiskLevel RiskLevel { get; set; } = RiskLevel.Low;

    [Column("last_event_at")]
    public DateTime? LastEventAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("modified_at")]
    public DateTime? ModifiedAt { get; set; }

    public Attempt Attempt { get; set; } = default!;

    public ProctoringSession ProctoringSession { get; set; } = default!;
}
