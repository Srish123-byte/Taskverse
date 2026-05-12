namespace Taskverse.Business.Enums;

/// <summary>
/// Mirrors the PostgreSQL user_status enum.
/// Must be registered with NpgsqlDataSourceBuilder.MapEnum in Startup.cs.
/// </summary>
public enum UserStatus
{
    APPROVED = 1,
    PENDING_APPROVAL = 2,
    REJECTED = 3
}
