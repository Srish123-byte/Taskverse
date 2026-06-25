namespace Taskverse.API.CodingEngine.Service.Models;

public class WorkerSettings
{
    public string WorkerId { get; set; } = string.Empty;
    public string WorkerType { get; set; } = "dispatch";
    public int PollingIntervalSeconds { get; set; } = 5;
    public int MaxConcurrentExecutions { get; set; } = 3;
    public int BatchSize { get; set; } = 5;
    public int RateLimitPerMinute { get; set; } = 60;
    public int LeaseDurationSeconds { get; set; } = 60;
}

public class CodingEngineWorkerOptions
{
    public List<WorkerSettings> Workers { get; set; } = new();
}
