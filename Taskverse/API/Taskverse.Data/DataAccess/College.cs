using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Taskverse.Data.DataAccess;

[Table("colleges")]
public class College
{
    [Key]
    [Column("college_id")]
    public Guid CollegeId { get; set; } = Guid.NewGuid();

    [Column("college_name")]
    public string? CollegeName { get; set; }

    [Column("admin_name")]
    public string? AdminName { get; set; }

    [Column("city")]
    public string? City { get; set; }

    [Column("state")]
    public string? State { get; set; }

    [Column("status")]
    public string? Status { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("modified_at")]
    public DateTime? ModifiedAt { get; set; }

    public ICollection<CodingQuestion> CodingQuestions { get; set; } = [];
}
