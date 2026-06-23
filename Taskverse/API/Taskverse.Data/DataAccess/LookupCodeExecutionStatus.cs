using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Taskverse.Data.DataAccess;

[Table("lookup_code_execution_status")]
public class LookupCodeExecutionStatus
{
    [Key]
    [Column("code_execution_status_id")]
    public short CodeExecutionStatusId { get; set; }

    [Required]
    [MaxLength(30)]
    [Column("code_execution_status")]
    public string CodeExecutionStatus { get; set; } = default!;

    public ICollection<CodeExecutionRequest> CodeExecutionRequests { get; set; } = [];
}
