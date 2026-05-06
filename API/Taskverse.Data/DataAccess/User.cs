using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Taskverse.Data.DataAccess;

[Table("users")]
public class User
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("full_name")]
    public string FullName { get; set; } = default!;

    [Required]
    [Column("email")]
    public string Email { get; set; } = default!;

    [Column("phone")]
    public string? Phone { get; set; }

    [Column("college_id")]
    public Guid? CollegeId { get; set; }

    [Required]
    [Column("role")]
    public string Role { get; set; } = default!;

    /// <summary>
    /// Maps to the PostgreSQL user_status enum.
    /// Valid values: PENDING_APPROVAL, ACTIVE, SUSPENDED, REJECTED
    /// </summary>
    [Required]
    [Column("status")]
    public UserStatus Status { get; set; } = UserStatus.PENDING_APPROVAL;

    [Column("batch_id")]
    public Guid? BatchId { get; set; }

    [Column("class_id")]
    public Guid? ClassId { get; set; }

    [Column("password_hash")]
    public string? PasswordHash { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("modified_at")]
    public DateTime? ModifiedAt { get; set; }
}
