namespace Taskverse.API.Assessments.Service.Models;

public class AssessmentStatusTransitionSettings
{
    /// <summary>
    /// Interval in seconds between each polling cycle.
    /// Default: 60 seconds.
    /// </summary>
    public int PollingIntervalSeconds { get; set; }

    /// <summary>
    /// PostgreSQL session-level advisory lock key used to coordinate
    /// across multiple service instances behind a load balancer.
    /// All instances must share the same key value.
    /// </summary>
    public long AdvisoryLockKey { get; set; }
}
