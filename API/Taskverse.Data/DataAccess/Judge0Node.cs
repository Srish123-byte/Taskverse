using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Taskverse.Data.DataAccess;

[Table("judge0_nodes")]
public class Judge0Node
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(500)]
    [Column("base_url")]
    public string BaseUrl { get; set; } = default!;

    [Column("enabled")]
    public bool Enabled { get; set; } = true;

    [Required]
    [MaxLength(30)]
    [Column("health_status")]
    public string HealthStatus { get; set; } = "Unknown";

    [Column("active_slots")]
    public int ActiveSlots { get; set; }

    [Column("reserved_final_slots")]
    public int ReservedFinalSlots { get; set; }

    [Column("last_health_check_at")]
    public DateTime? LastHealthCheckAt { get; set; }

    [Column("cooldown_until")]
    public DateTime? CooldownUntil { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("modified_at")]
    public DateTime? ModifiedAt { get; set; }

    public ICollection<CodeExecutionRequest> CodeExecutionRequests { get; set; } = new List<CodeExecutionRequest>();
}
