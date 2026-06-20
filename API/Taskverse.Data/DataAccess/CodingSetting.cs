using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Taskverse.Data.DataAccess;

[Table("coding_settings")]
public class CodingSetting
{
    [Key]
    [Column("coding_setting_id")]
    public Guid CodingSettingId { get; set; } = Guid.NewGuid();

    [Column("default_language_id")]
    public Guid? DefaultLanguageId { get; set; }

    [Column("time_limit_ms")]
    public int TimeLimitMs { get; set; } = 3000;

    [Column("memory_limit_kb")]
    public int MemoryLimitKb { get; set; } = 262144;

    [Column("max_code_size_kb")]
    public int MaxCodeSizeKb { get; set; } = 512;

    [Column("is_code_execution_enabled")]
    public bool IsCodeExecutionEnabled { get; set; }

    [Column("is_submission_enabled")]
    public bool IsSubmissionEnabled { get; set; } = true;

    [Column("allow_language_change")]
    public bool AllowLanguageChange { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("modified_at")]
    public DateTime? ModifiedAt { get; set; }

    public CodingLanguage? DefaultLanguage { get; set; }
}
