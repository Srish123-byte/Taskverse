using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Taskverse.Data.DataAccess;

[Table("subjects")]
public class Subject
{
    [Key]
    [Column("subject_id")]
    public Guid SubjectId { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(150)]
    [Column("subject_name")]
    public string SubjectName { get; set; } = default!;

    [Column("description")]
    public string? Description { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("modified_at")]
    public DateTime? ModifiedAt { get; set; }

    public ICollection<Topic> Topics { get; set; } = [];

    public ICollection<SubjectBatch> SubjectBatches { get; set; } = [];
}
