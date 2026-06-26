using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Taskverse.Data.DataAccess;

[Table("subject_batches")]
public class SubjectBatch
{
    [Key]
    [Column("subject_batch_id")]
    public Guid SubjectBatchId { get; set; } = Guid.NewGuid();

    [Required]
    [Column("subject_id")]
    public Guid SubjectId { get; set; }

    [Required]
    [Column("batch_id")]
    public Guid BatchId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Subject Subject { get; set; } = default!;

    public Batch Batch { get; set; } = default!;
}
