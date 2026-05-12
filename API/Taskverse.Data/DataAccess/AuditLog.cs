using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Taskverse.Data.DataAccess;

[Table("audit_logs")]
public class AuditLog
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("action")]
    public string Action { get; set; } = default!;

    [MaxLength(100)]
    [Column("entity_type")]
    public string? EntityType { get; set; }

    [Column("entity_id")]
    public Guid? EntityId { get; set; }

    [Column("details")]
    public string? Details { get; set; }

    [Column("occurred_at")]
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    [MaxLength(45)]
    [Column("ip_address")]
    public string? IpAddress { get; set; }
}
