using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Taskverse.Data.DataAccess;

[Table("assessments")]
public class Assessment
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(256)]
    [Column("title")]
    public string Title { get; set; } = default!;

    [Column("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Valid values: Exam, Coding, Mixed
    /// </summary>
    [Required]
    [MaxLength(50)]
    [Column("type")]
    public string Type { get; set; } = default!;

    [Column("exam_id")]
    public Guid? ExamId { get; set; }

    /// <summary>
    /// UUIDs of coding challenges linked to this assessment.
    /// Stored as a native PostgreSQL UUID array.
    /// </summary>
    [Column("challenge_ids", TypeName = "uuid[]")]
    public Guid[] ChallengeIds { get; set; } = [];

    /// <summary>
    /// UUIDs of users (students) this assessment is assigned to.
    /// Stored as a native PostgreSQL UUID array.
    /// </summary>
    [Column("assigned_to", TypeName = "uuid[]")]
    public Guid[] AssignedTo { get; set; } = [];

    [Column("due_date")]
    public DateTime? DueDate { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Required]
    [Column("created_by")]
    public Guid CreatedBy { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("modified_at")]
    public DateTime? UpdatedAt { get; set; }
}
