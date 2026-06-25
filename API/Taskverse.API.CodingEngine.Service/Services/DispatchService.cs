using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Taskverse.API.CodingEngine.Service.Clients.Judge0;
using Taskverse.API.CodingEngine.Service.Models;
using Taskverse.Data.DataAccess;
using Taskverse.Data.Enums;

namespace Taskverse.API.CodingEngine.Service.Services;

public class DispatchService : IDispatchService
{
    private readonly TaskverseContext _context;
    private readonly IJudge0Client _judge0Client;
    private readonly IExecutionLifecycleService _executionLifecycle;
    private readonly ILogger<DispatchService> _logger;

    public DispatchService(
        TaskverseContext context,
        IJudge0Client judge0Client,
        IExecutionLifecycleService executionLifecycle,
        ILogger<DispatchService> logger)
    {
        _context = context;
        _judge0Client = judge0Client;
        _executionLifecycle = executionLifecycle;
        _logger = logger;
    }

    public async Task DispatchAsync(CodeExecutionRequest request, string workerId, CancellationToken cancellationToken)
    {
        _context.Attach(request);

        var now = DateTime.UtcNow;

        var codingLanguage = await _context.CodingLanguages
            .FirstOrDefaultAsync(cl => cl.CodingLanguageId == request.CodingLanguageId, cancellationToken);

        if (codingLanguage is null || codingLanguage.Judge0LanguageId is null or 0)
        {
            _logger.LogError("Language not found or has no Judge0 mapping for request '{RequestId}'.", request.CodeExecutionRequestId);
            await _executionLifecycle.MarkTerminalAsync(request, CodeExecutionStatus.Failed, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return;
        }

        var node = await ClaimHealthyNodeAsync(cancellationToken);
        if (node is null)
        {
            _logger.LogWarning(
                "No healthy Judge0 node with available capacity for request '{RequestId}'. Re-queuing.",
                request.CodeExecutionRequestId);
            request.CodeExecutionStatusId = (short)CodeExecutionStatus.Queued;
            request.ModifiedAt = now;
            await _context.SaveChangesAsync(cancellationToken);
            return;
        }

        request.Judge0NodeId = node.Id;

        var judge0Request = new Judge0CreateSubmissionRequest(
            SourceCode: request.Code,
            LanguageId: codingLanguage.Judge0LanguageId.Value,
            Stdin: request.InputPayload,
            ExpectedOutput: null,
            CpuTimeLimit: 3.0f,
            MemoryLimit: null);

        string judge0Token;
        try
        {
            judge0Token = await _judge0Client.CreateSubmissionAsync(node.BaseUrl, judge0Request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Judge0 submission for request '{RequestId}' on node '{NodeId}'.",
                request.CodeExecutionRequestId, node.Id);
            await _executionLifecycle.MarkTerminalAsync(request, CodeExecutionStatus.Failed, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return;
        }

        request.CodeExecutionStatusId = (short)CodeExecutionStatus.Running;
        request.Judge0BatchToken = judge0Token;
        request.StartedAt = now;
        request.WorkerId = workerId;
        request.ModifiedAt = now;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Dispatched request '{RequestId}' to Judge0 node '{NodeId}' with token '{Token}'.",
            request.CodeExecutionRequestId, node.Id, judge0Token);
    }

    private async Task<Judge0Node?> ClaimHealthyNodeAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var claimed = await _context.Judge0Nodes
            .FromSqlInterpolated($@"
                UPDATE judge0_nodes
                SET active_slots = active_slots - 1,
                    modified_at = {now}
                WHERE id = (
                    SELECT id
                    FROM judge0_nodes
                    WHERE enabled = true
                      AND health_status = 'Healthy'
                      AND (cooldown_until IS NULL OR cooldown_until <= {now})
                      AND active_slots > reserved_final_slots
                    ORDER BY active_slots DESC
                    LIMIT 1
                    FOR UPDATE SKIP LOCKED
                )
                RETURNING *")
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return claimed.FirstOrDefault();
    }
}
