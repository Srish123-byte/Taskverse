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
    private readonly ILogger<DispatchService> _logger;

    public DispatchService(
        TaskverseContext context,
        IJudge0Client judge0Client,
        ILogger<DispatchService> logger)
    {
        _context = context;
        _judge0Client = judge0Client;
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
            request.CodeExecutionStatusId = (short)CodeExecutionStatus.Failed;
            request.CompletedAt = now;
            request.ModifiedAt = now;
            await _context.SaveChangesAsync(cancellationToken);
            return;
        }

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
            judge0Token = await _judge0Client.CreateSubmissionAsync(judge0Request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Judge0 submission for request '{RequestId}'.", request.CodeExecutionRequestId);
            request.CodeExecutionStatusId = (short)CodeExecutionStatus.Failed;
            request.CompletedAt = now;
            request.ModifiedAt = now;
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
            "Dispatched request '{RequestId}' to Judge0 with token '{Token}'.",
            request.CodeExecutionRequestId, judge0Token);
    }
}
