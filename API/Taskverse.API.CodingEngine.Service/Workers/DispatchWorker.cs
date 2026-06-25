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

    public DispatchWorker(
        IServiceProvider serviceProvider,
        ILogger<DispatchWorker> logger,
        IOptionsSnapshot<CodingEngineWorkerOptions> workerOptions,
        IConfiguration configuration)
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
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "DispatchWorker '{WorkerId}' started. Polling every {Interval}s, max {Concurrent} concurrent, batch {Batch}.",
            _settings.WorkerId, _settings.PollingIntervalSeconds, _settings.MaxConcurrentExecutions, _settings.BatchSize);

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
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TaskverseContext>();
        var dispatchService = scope.ServiceProvider.GetRequiredService<IDispatchService>();

        var queued = await context.CodeExecutionRequests
            .Where(cer => cer.CodeExecutionStatusId == (short)CodeExecutionStatus.Queued)
            .OrderBy(cer => cer.RequestedAt)
            .Take(_settings.BatchSize)
            .ToListAsync(cancellationToken);

        if (queued.Count == 0) return;

        var tasks = queued.Select(async request =>
        {
            await _concurrencySemaphore.WaitAsync(cancellationToken);
            try
            {
                request.CodeExecutionStatusId = (short)CodeExecutionStatus.Running;
                request.WorkerId = _settings.WorkerId;
                request.ModifiedAt = DateTime.UtcNow;
                await context.SaveChangesAsync(cancellationToken);

                await dispatchService.DispatchAsync(request, _settings.WorkerId, cancellationToken);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DispatchWorker '{WorkerId}' failed request '{RequestId}'.",
                    _settings.WorkerId, request.CodeExecutionRequestId);
            }
            finally { _concurrencySemaphore.Release(); }
        });

        await Task.WhenAll(tasks);
    }
}
