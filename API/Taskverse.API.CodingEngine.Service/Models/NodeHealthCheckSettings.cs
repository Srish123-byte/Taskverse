namespace Taskverse.API.CodingEngine.Service.Models;

public class NodeHealthCheckSettings
{
    public int IntervalSeconds { get; set; } = 30;
    public int TimeoutSeconds { get; set; } = 5;
    public int UnhealthyCooldownSeconds { get; set; } = 60;
}
