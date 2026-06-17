namespace Taskverse.Data.Enums;

/// <summary>
/// Mirrors the PostgreSQL lookup_user_status table
/// </summary>
public enum UserStatus
{
    APPROVED = 1,
    PENDING_APPROVAL = 2,
    REJECTED = 3
}
