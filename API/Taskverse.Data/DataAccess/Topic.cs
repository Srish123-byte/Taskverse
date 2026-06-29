using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Taskverse.Data.DataAccess;

[Table("topics")]
public class Topic
{
    [Key]
    [Column("topic_id")]
    public Guid TopicId { get; set; } = Guid.NewGuid();

    [Required]
    [Column("subject_id")]
    public Guid SubjectId { get; set; }

    [Required]
    [MaxLength(150)]
    [Column("topic_name")]
    public string TopicName { get; set; } = default!;

    [Column("description")]
    public string? Description { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("modified_at")]
    public DateTime? ModifiedAt { get; set; }

    public Subject Subject { get; set; } = default!;

    public ICollection<Assessment> Assessments { get; set; } = [];
}
