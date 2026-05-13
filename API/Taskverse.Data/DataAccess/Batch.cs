using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Taskverse.Data.DataAccess;

[Table("batches")]
public class Batch
{
    [Key]
    [Column("batch_id")]
    public Guid BatchId { get; set; } = Guid.NewGuid();

    [Column("class_id")]
    public Guid ClassId { get; set; }

    [Column("college_id")]
    public Guid CollegeId { get; set; }

    [Required]
    [Column("name")]
    public string Name { get; set; } = default!;

    [Column("capacity")]
    public int? Capacity { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("modified_at")]
    public DateTime? ModifiedAt { get; set; }

    // Navigation to students
    public ICollection<Student> Students { get; set; }

    // Navigation to trainer assignments
    public ICollection<TrainerBatch> TrainerBatches { get; set; }
}
