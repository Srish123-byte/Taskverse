using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Taskverse.API.CodingEngine.Service.Models;
using Taskverse.API.CodingEngine.Service.Services;
using Taskverse.Data.DataAccess;
using Taskverse.Data.Enums;

namespace Taskverse.API.CodingEngine.Service.Workers;

public class DispatchWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DispatchWorker> _logger;
    private readonly WorkerSettings _settings;
    private readonly SemaphoreSlim _concurrencySemaphore;
    private readonly IRateLimiter _rateLimiter;
    private readonly string _instanceId;

    public DispatchWorker(
        IServiceProvider serviceProvider,
        ILogger<DispatchWorker> logger,
        IOptionsSnapshot<CodingEngineWorkerOptions> workerOptions,
        IConfiguration configuration,
        RateLimiterFactory rateLimiterFactory)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        var workerId = configuration["WorkerId"] ?? "dispatch-default";
        var workers = workerOptions.Value.Workers;
        _settings = workers.FirstOrDefault(w => w.WorkerId == workerId)
            ?? workers.FirstOrDefault()
            ?? new WorkerSettings
            {
                WorkerId = workerId,
                PollingIntervalSeconds = 3,
                MaxConcurrentExecutions = 5,
                BatchSize = 10,
                RateLimitPerMinute = 100
            };

        _concurrencySemaphore = new SemaphoreSlim(_settings.MaxConcurrentExecutions, _settings.MaxConcurrentExecutions);
        _rateLimiter = rateLimiterFactory.GetOrCreate(workerId, _settings.RateLimitPerMinute);
        _instanceId = $"{Environment.MachineName}:{Environment.ProcessId}:{Guid.NewGuid():N}";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "DispatchWorker '{WorkerId}' (instance '{InstanceId}') started. Polling every {Interval}s, max {Concurrent} concurrent, batch {Batch}.",
            _settings.WorkerId, _instanceId, _settings.PollingIntervalSeconds, _settings.MaxConcurrentExecutions, _settings.BatchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DispatchQueuedRequestsAsync(stoppingToken);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DispatchWorker '{WorkerId}' error.", _settings.WorkerId);
            }

            await Task.Delay(TimeSpan.FromSeconds(_settings.PollingIntervalSeconds), stoppingToken);
        }

        _logger.LogInformation("DispatchWorker '{WorkerId}' stopped.", _settings.WorkerId);
    }

    private async Task DispatchQueuedRequestsAsync(CancellationToken cancellationToken)
    {
        List<CodeExecutionRequest> claimed;

        using (var claimScope = _serviceProvider.CreateScope())
        {
            var claimContext = claimScope.ServiceProvider.GetRequiredService<TaskverseContext>();
            claimed = await ClaimRequestsAsync(claimContext, cancellationToken);
        }

        if (claimed.Count == 0)
        {
            return;
        }

        var tasks = claimed.Select(request => DispatchClaimedRequestAsync(request, cancellationToken));
        await Task.WhenAll(tasks);
    }

    private async Task<List<CodeExecutionRequest>> ClaimRequestsAsync(TaskverseContext context, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var leaseExpiresAt = now.AddSeconds(_settings.LeaseDurationSeconds);

        // Atomic claim: FOR UPDATE SKIP LOCKED in the subquery means two dispatcher
        // instances can never claim the same row, even across separate processes.
        // Eligible rows are freshly Queued, or Running with an expired lease and no
        // Judge0 token yet (i.e. the worker that claimed them crashed before dispatching).
        return await context.CodeExecutionRequests
            .FromSqlInterpolated($@"
                UPDATE code_execution_requests
                SET code_execution_status = {(short)CodeExecutionStatus.Running},
                    worker_id = {_settings.WorkerId},
                    claimed_by_instance = {_instanceId},
                    lease_expires_at = {leaseExpiresAt},
                    lease_heartbeat_at = {now},
                    started_at = {now},
                    modified_at = {now}
                WHERE code_execution_request_id IN (
                    SELECT code_execution_request_id
                    FROM code_execution_requests
                    WHERE (code_execution_status = {(short)CodeExecutionStatus.Queued})
                       OR (code_execution_status = {(short)CodeExecutionStatus.Running}
                           AND judge0_batch_token IS NULL
                           AND lease_expires_at < {now})
                    ORDER BY requested_at
                    LIMIT {_settings.BatchSize}
                    FOR UPDATE SKIP LOCKED
                )
                RETURNING *")
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    private async Task DispatchClaimedRequestAsync(CodeExecutionRequest claimedRequest, CancellationToken cancellationToken)
    {
        await _concurrencySemaphore.WaitAsync(cancellationToken);
        try
        {
            await _rateLimiter.WaitAsync(cancellationToken);

            using var scope = _serviceProvider.CreateScope();
            var dispatchService = scope.ServiceProvider.GetRequiredService<IDispatchService>();
            await dispatchService.DispatchAsync(claimedRequest, _settings.WorkerId, cancellationToken);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DispatchWorker '{WorkerId}' failed request '{RequestId}'.",
                _settings.WorkerId, claimedRequest.CodeExecutionRequestId);
        }
        finally { _concurrencySemaphore.Release(); }
    }
}
