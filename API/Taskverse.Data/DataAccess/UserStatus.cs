namespace Taskverse.Data.DataAccess;

/// <summary>
/// Mirrors the PostgreSQL user_status enum.
/// Must be registered with NpgsqlDataSourceBuilder.MapEnum in Startup.cs.
/// </summary>
public enum UserStatus
{
    PENDING_APPROVAL,
    ACTIVE,
    SUSPENDED,
    REJECTED
}
