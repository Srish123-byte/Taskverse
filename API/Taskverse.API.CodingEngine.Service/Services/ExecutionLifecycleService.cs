using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Taskverse.Data.DataAccess;
using Taskverse.Data.Enums;

namespace Taskverse.API.CodingEngine.Service.Services;

public class ExecutionLifecycleService : IExecutionLifecycleService
{
    private readonly TaskverseContext _context;
    private readonly ILogger<ExecutionLifecycleService> _logger;

    public ExecutionLifecycleService(TaskverseContext context, ILogger<ExecutionLifecycleService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> RegisterAndCheckCapacityAsync(string executionMode, CancellationToken cancellationToken)
    {
        var counterKey = NormalizeCounterKey(executionMode);
        var now = DateTime.UtcNow;

        var counters = await _context.CodingEngineCounters
            .FromSqlInterpolated($@"
                UPDATE coding_engine_counters
                SET active_count = active_count + 1,
                    modified_at = {now}
                WHERE counter_key = {counterKey}
                RETURNING *")
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var counter = counters.FirstOrDefault();
        if (counter is null)
        {
            _logger.LogWarning("No coding_engine_counters row for '{CounterKey}'. Treating as over capacity.", counterKey);
            return false;
        }

        if (counter.ActiveCount > counter.MaxActive)
        {
            _logger.LogInformation(
                "Skipping inline wait for '{CounterKey}': {ActiveCount}/{MaxActive} active.",
                counterKey, counter.ActiveCount, counter.MaxActive);
            return false;
        }

        var isSubmitMode = counterKey == "submit";
        var hasAvailableNode = isSubmitMode
            ? await _context.Judge0Nodes.AnyAsync(jn =>
                jn.Enabled &&
                jn.HealthStatus == "Healthy" &&
                (jn.CooldownUntil == null || jn.CooldownUntil <= now) &&
                jn.ActiveSlots > 0, cancellationToken)
            : await _context.Judge0Nodes.AnyAsync(jn =>
                jn.Enabled &&
                jn.HealthStatus == "Healthy" &&
                (jn.CooldownUntil == null || jn.CooldownUntil <= now) &&
                jn.ActiveSlots > jn.ReservedFinalSlots, cancellationToken);

        if (!hasAvailableNode)
        {
            _logger.LogInformation("Skipping inline wait for '{CounterKey}': no node with available capacity.", counterKey);
        }

        return hasAvailableNode;
    }

    public async Task MarkTerminalAsync(CodeExecutionRequest request, CodeExecutionStatus terminalStatus, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        request.CodeExecutionStatusId = (short)terminalStatus;
        request.CompletedAt = now;
        request.ModifiedAt = now;

        await DecrementModeCounterAsync(request.ExecutionMode, cancellationToken);

        if (request.Judge0NodeId is not null)
        {
            await ReleaseNodeSlotAsync(request.Judge0NodeId.Value, cancellationToken);
        }
    }

    private async Task DecrementModeCounterAsync(string executionMode, CancellationToken cancellationToken)
    {
        var counterKey = NormalizeCounterKey(executionMode);

        await _context.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE coding_engine_counters
            SET active_count = GREATEST(active_count - 1, 0),
                modified_at = {DateTime.UtcNow}
            WHERE counter_key = {counterKey}", cancellationToken);
    }

    private async Task ReleaseNodeSlotAsync(Guid nodeId, CancellationToken cancellationToken)
    {
        await _context.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE judge0_nodes
            SET active_slots = active_slots + 1,
                modified_at = {DateTime.UtcNow}
            WHERE id = {nodeId}", cancellationToken);
    }

    private static string NormalizeCounterKey(string executionMode)
        => executionMode.Equals("Submit", StringComparison.OrdinalIgnoreCase) ? "submit" : "run";
}
