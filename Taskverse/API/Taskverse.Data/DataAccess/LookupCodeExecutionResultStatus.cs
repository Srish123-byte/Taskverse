using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Taskverse.Data.DataAccess;

[Table("lookup_code_execution_result_status")]
public class LookupCodeExecutionResultStatus
{
    [Key]
    [Column("code_execution_result_status_id")]
    public short CodeExecutionResultStatusId { get; set; }

    [Required]
    [MaxLength(30)]
    [Column("code_execution_result_status")]
    public string CodeExecutionResultStatus { get; set; } = default!;

    public ICollection<CodeExecutionResult> CodeExecutionResults { get; set; } = [];
}
