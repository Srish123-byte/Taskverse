namespace Taskverse.Data.Enums;

/// <summary>
/// Mirrors the PostgreSQL lookup_code_execution_result_status table
/// </summary>
public enum CodeExecutionResultStatus
{
    Success = 1,
    Failed = 2,
    Compilation_Error = 3,
    Runtime_Error = 4,
    Timeout = 5,
    Cancelled = 6
}