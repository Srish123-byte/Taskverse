namespace Taskverse.Data.Enums;

/// <summary>
/// Mirrors the PostgreSQL lookup_proctoring_network_status table
/// </summary>
public enum ProctoringNetworkStatus
{
    Online = 1,
    Offline = 2,
    Unstable = 3,
    Unknown = 4
}
