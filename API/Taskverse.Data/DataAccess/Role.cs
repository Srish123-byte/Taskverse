using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Taskverse.Data.DataAccess;

[Table("roles")]
public class Role
{
    [Key]
    [Column("role_id")]
    public short RoleId { get; set; }

    [Required]
    [Column("name")]
    public string Name { get; set; } = default!;

    [Column("description")]
    public string? Description { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;
}
