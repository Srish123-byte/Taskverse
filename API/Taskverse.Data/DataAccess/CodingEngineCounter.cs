using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Taskverse.Data.DataAccess;

[Table("coding_engine_counters")]
public class CodingEngineCounter
{
    [Key]
    [MaxLength(30)]
    [Column("counter_key")]
    public string CounterKey { get; set; } = default!;

    [Column("active_count")]
    public int ActiveCount { get; set; }

    [Column("max_active")]
    public int MaxActive { get; set; }

    [Column("modified_at")]
    public DateTime? ModifiedAt { get; set; }
}
