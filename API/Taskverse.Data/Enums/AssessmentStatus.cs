namespace Taskverse.Data.Enums;

/// <summary>
/// Mirrors the PostgreSQL lookup_assessment_status enum.
/// </summary>
public enum AssessmentStatus
{
    Draft = 1,
    Scheduled = 2,
    Live = 3,
    Completed = 4,
    Cancelled = 5,
    Soft_Deleted = 6
}
