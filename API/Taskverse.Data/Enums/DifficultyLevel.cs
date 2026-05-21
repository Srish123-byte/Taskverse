namespace Taskverse.Business.Enums;

/// <summary>
/// Mirrors the PostgreSQL user_status enum.
/// Must be registered with NpgsqlDataSourceBuilder.MapEnum in Startup.cs.
/// </summary>
public enum DifficultyLevel
{
    Easy = 1,
    Medium = 2,
    Hard = 3
}
