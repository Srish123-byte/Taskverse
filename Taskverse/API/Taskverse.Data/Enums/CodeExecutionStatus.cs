namespace Taskverse.Data.Enums;

/// <summary>
/// Mirrors the PostgreSQL lookup_code_execution_status table
/// </summary>
public enum CodeExecutionStatus
{
    Queued = 1,
    Running = 2,
    Completed = 3,
    Failed = 4,
    Cancelled = 5,
    Timeout = 6
}
