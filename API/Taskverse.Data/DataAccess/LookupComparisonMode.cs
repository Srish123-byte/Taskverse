using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Taskverse.Data.DataAccess;

[Table("lookup_comparison_mode")]
public class LookupComparisonMode
{
    [Key]
    [Column("comparison_mode_id")]
    public short ComparisonModeId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("comparison_mode")]
    public string ComparisonMode { get; set; } = default!;
}
