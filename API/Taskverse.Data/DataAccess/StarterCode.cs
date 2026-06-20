using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Taskverse.Data.DataAccess;

[Table("starter_code")]
public class StarterCode
{
    [Key]
    [Column("starter_code_id")]
    public Guid StarterCodeId { get; set; } = Guid.NewGuid();

    [Required]
    [Column("coding_language_id")]
    public Guid CodingLanguageId { get; set; }

    [Required]
    [Column("starter_code")]
    public string StarterCodeContent { get; set; } = default!;

    [Column("solution_template")]
    public string? SolutionTemplate { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("modified_at")]
    public DateTime? ModifiedAt { get; set; }

    public CodingLanguage CodingLanguage { get; set; } = default!;
}
