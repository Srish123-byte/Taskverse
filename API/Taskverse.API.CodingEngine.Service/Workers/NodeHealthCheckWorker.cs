using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Taskverse.API.CodingEngine.Service.Clients.Judge0;
using Taskverse.API.CodingEngine.Service.Models;
using Taskverse.Data.DataAccess;

namespace Taskverse.API.CodingEngine.Service.Workers;

public class NodeHealthCheckWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NodeHealthCheckWorker> _logger;
    private readonly NodeHealthCheckSettings _settings;

    public NodeHealthCheckWorker(
        IServiceProvider serviceProvider,
        ILogger<NodeHealthCheckWorker> logger,
        IOptions<NodeHealthCheckSettings> settings)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _settings = settings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "NodeHealthCheckWorker started. Checking every {Interval}s, timeout {Timeout}s, cooldown {Cooldown}s.",
            _settings.IntervalSeconds, _settings.TimeoutSeconds, _settings.UnhealthyCooldownSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckNodesAsync(stoppingToken);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "NodeHealthCheckWorker error.");
            }

            await Task.Delay(TimeSpan.FromSeconds(_settings.IntervalSeconds), stoppingToken);
        }

        _logger.LogInformation("NodeHealthCheckWorker stopped.");
    }

    private async Task CheckNodesAsync(CancellationToken cancellationToken)
    {
        List<Judge0Node> nodes;

        using (var queryScope = _serviceProvider.CreateScope())
        {
            var queryContext = queryScope.ServiceProvider.GetRequiredService<TaskverseContext>();
            var now = DateTime.UtcNow;

            nodes = await queryContext.Judge0Nodes
                .Where(jn => jn.Enabled && (jn.CooldownUntil == null || jn.CooldownUntil <= now))
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        if (nodes.Count == 0)
        {
            return;
        }

        var tasks = nodes.Select(node => CheckNodeAsync(node.Id, node.BaseUrl, cancellationToken));
        await Task.WhenAll(tasks);
    }

    private async Task CheckNodeAsync(Guid nodeId, string baseUrl, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TaskverseContext>();
        var judge0Client = scope.ServiceProvider.GetRequiredService<IJudge0Client>();

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(_settings.TimeoutSeconds));

        bool isHealthy;
        try
        {
            isHealthy = await judge0Client.PingAsync(baseUrl, timeoutCts.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            isHealthy = false;
        }

        var now = DateTime.UtcNow;

        if (isHealthy)
        {
            await context.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE judge0_nodes
                SET health_status = 'Healthy',
                    cooldown_until = NULL,
                    last_health_check_at = {now},
                    modified_at = {now}
                WHERE id = {nodeId}", cancellationToken);
        }
        else
        {
            var cooldownUntil = now.AddSeconds(_settings.UnhealthyCooldownSeconds);
            _logger.LogWarning(
                "Judge0 node '{NodeId}' ({BaseUrl}) failed health check. Cooling down until {CooldownUntil}.",
                nodeId, baseUrl, cooldownUntil);

            await context.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE judge0_nodes
                SET health_status = 'Unhealthy',
                    cooldown_until = {cooldownUntil},
                    last_health_check_at = {now},
                    modified_at = {now}
                WHERE id = {nodeId}", cancellationToken);
        }
    }
}
