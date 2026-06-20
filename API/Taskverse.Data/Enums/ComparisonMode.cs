namespace Taskverse.Data.Enums;

/// <summary>
/// Mirrors the PostgreSQL lookup_comparison_mode table.
/// </summary>
public enum ComparisonMode
{
    exact = 1,
    trimmed = 2,
    case_insensitive = 3,
    json = 4,
    numeric_tolerance = 5,
    unordered_json = 6
}
