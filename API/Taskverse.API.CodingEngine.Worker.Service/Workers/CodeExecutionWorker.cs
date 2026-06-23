using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Taskverse.API.CodingEngine.Worker.Service.Services;
using Taskverse.Data.DataAccess;

namespace Taskverse.API.CodingEngine.Worker.Service.Workers;

public class CodeExecutionWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CodeExecutionWorker> _logger;

    public CodeExecutionWorker(
        IServiceProvider serviceProvider,
        ILogger<CodeExecutionWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("CodeExecutionWorker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessQueuedExecutionsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing queued code executions.");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }

        _logger.LogInformation("CodeExecutionWorker stopped.");
    }

    private async Task ProcessQueuedExecutionsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TaskverseContext>();
        var executionService = scope.ServiceProvider.GetRequiredService<ICodeExecutionService>();

        var queuedRequests = await context.CodeExecutionRequests
            .Where(cer => cer.CodeExecutionStatusId == 1) // Queued
            .OrderBy(cer => cer.RequestedAt)
            .Take(5)
            .ToListAsync(cancellationToken);

        foreach (var request in queuedRequests)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var assessmentCodingQuestions = await context.AssessmentCodingQuestions
                    .Where(acq => acq.AssessmentId == request.AssessmentId)
                    .Select(acq => acq.CodingQuestionId)
                    .ToListAsync(cancellationToken);

                var codingQuestionIds = assessmentCodingQuestions.Distinct().ToList();

                var testCases = await context.TestCases
                    .Where(tc => codingQuestionIds.Contains(tc.CodingQuestionId) && tc.IsActive)
                    .ToListAsync(cancellationToken);

                _logger.LogInformation(
                    "Processing execution request '{RequestId}' with {TestCaseCount} test cases.",
                    request.CodeExecutionRequestId, testCases.Count);

                await executionService.ExecuteCodeAsync(request, testCases, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to process execution request '{RequestId}'.",
                    request.CodeExecutionRequestId);

                request.CodeExecutionStatusId = 4; // Failed
                request.ModifiedAt = DateTime.UtcNow;
                await context.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
