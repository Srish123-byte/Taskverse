using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Taskverse.Data.DataAccess;

[Table("classes")]
public class Class
{
    [Key]
    [Column("class_id")]
    public Guid ClassId { get; set; } = Guid.NewGuid();

    [Column("college_id")]
    public Guid CollegeId { get; set; }

    [Required]
    [Column("name")]
    public string Name { get; set; } = default!;

    [Column("description")]
    public string? Description { get; set; }

    [Column("academic_year")]
    public string? AcademicYear { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("modified_at")]
    public DateTime? ModifiedAt { get; set; }

    // Navigation to batches
    public ICollection<Batch> Batches { get; set; }

    // Navigation to students (one class → many students)
    public ICollection<Student> Students { get; set; }

    // Navigation to trainer assignments
    public ICollection<TrainerClass> TrainerClasses { get; set; }
}
