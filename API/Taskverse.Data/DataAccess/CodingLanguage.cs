using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Taskverse.Data.DataAccess;

[Table("coding_languages")]
public class CodingLanguage
{
    [Key]
    [Column("coding_language_id")]
    public Guid CodingLanguageId { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(50)]
    [Column("language_code")]
    public string LanguageCode { get; set; } = default!;

    [Required]
    [MaxLength(100)]
    [Column("display_name")]
    public string DisplayName { get; set; } = default!;

    [Required]
    [MaxLength(50)]
    [Column("monaco_language_code")]
    public string MonacoLanguageCode { get; set; } = default!;

    [MaxLength(20)]
    [Column("file_extension")]
    public string? FileExtension { get; set; }

    [MaxLength(100)]
    [Column("runtime_name")]
    public string? RuntimeName { get; set; }

    [MaxLength(50)]
    [Column("runtime_version")]
    public string? RuntimeVersion { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("modified_at")]
    public DateTime? ModifiedAt { get; set; }

    public ICollection<CodingSetting> CodingSettings { get; set; } = [];

    public ICollection<StarterCode> StarterCodes { get; set; } = [];

    public ICollection<StudentCode> StudentCodes { get; set; } = [];

    public ICollection<CodeExecutionRequest> CodeExecutionRequests { get; set; } = [];
}
