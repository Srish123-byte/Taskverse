namespace Taskverse.Business.Enums;

/// <summary>
/// Mirrors the PostgreSQL lookup_attempt_status.
/// </summary>
public enum AttemptStatus
{
    In_Progress = 1,
    Submitted = 2,
    Auto_Submitted = 3
}
