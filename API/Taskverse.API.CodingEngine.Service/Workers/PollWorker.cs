using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Taskverse.API.CodingEngine.Service.Models;
using Taskverse.API.CodingEngine.Service.Services;
using Taskverse.Data.DataAccess;
using Taskverse.Data.Enums;

namespace Taskverse.API.CodingEngine.Service.Workers;

public class PollWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PollWorker> _logger;
    private readonly WorkerSettings _settings;
    private readonly SemaphoreSlim _concurrencySemaphore;
    private readonly IRateLimiter _rateLimiter;

    public PollWorker(
        IServiceProvider serviceProvider,
        ILogger<PollWorker> logger,
        IOptionsSnapshot<CodingEngineWorkerOptions> workerOptions,
        IConfiguration configuration,
        RateLimiterFactory rateLimiterFactory)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        var workerId = configuration["WorkerId"] ?? "poll-default";
        var workers = workerOptions.Value.Workers;
        _settings = workers.FirstOrDefault(w => w.WorkerId == workerId)
            ?? workers.FirstOrDefault()
            ?? new WorkerSettings
            {
                WorkerId = workerId,
                PollingIntervalSeconds = 3,
                MaxConcurrentExecutions = 10,
                BatchSize = 20,
                RateLimitPerMinute = 200
            };

        _concurrencySemaphore = new SemaphoreSlim(_settings.MaxConcurrentExecutions, _settings.MaxConcurrentExecutions);
        _rateLimiter = rateLimiterFactory.GetOrCreate(workerId, _settings.RateLimitPerMinute);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "PollWorker '{WorkerId}' started. Polling every {Interval}s, max {Concurrent} concurrent, batch {Batch}.",
            _settings.WorkerId, _settings.PollingIntervalSeconds, _settings.MaxConcurrentExecutions, _settings.BatchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PollRunningRequestsAsync(stoppingToken);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PollWorker '{WorkerId}' error.", _settings.WorkerId);
            }

            await Task.Delay(TimeSpan.FromSeconds(_settings.PollingIntervalSeconds), stoppingToken);
        }

        _logger.LogInformation("PollWorker '{WorkerId}' stopped.", _settings.WorkerId);
    }

    private async Task PollRunningRequestsAsync(CancellationToken cancellationToken)
    {
        List<CodeExecutionRequest> running;

        using (var queryScope = _serviceProvider.CreateScope())
        {
            var queryContext = queryScope.ServiceProvider.GetRequiredService<TaskverseContext>();
            running = await queryContext.CodeExecutionRequests
                .Where(cer =>
                    cer.CodeExecutionStatusId == (short)CodeExecutionStatus.Running &&
                    cer.Judge0BatchToken != null)
                .OrderBy(cer => cer.StartedAt)
                .Take(_settings.BatchSize)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        if (running.Count == 0)
        {
            return;
        }

        var tasks = running.Select(request => CollectResultForRequestAsync(request, cancellationToken));
        await Task.WhenAll(tasks);
    }

    private async Task CollectResultForRequestAsync(CodeExecutionRequest request, CancellationToken cancellationToken)
    {
        await _concurrencySemaphore.WaitAsync(cancellationToken);
        try
        {
            await _rateLimiter.WaitAsync(cancellationToken);

            using var scope = _serviceProvider.CreateScope();
            var pollService = scope.ServiceProvider.GetRequiredService<IPollService>();
            await pollService.CollectResultAsync(request, cancellationToken);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PollWorker '{WorkerId}' failed request '{RequestId}'.",
                _settings.WorkerId, request.CodeExecutionRequestId);
        }
        finally { _concurrencySemaphore.Release(); }
    }
}
