using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Taskverse.API.CodingEngine.Service.Clients;
using Taskverse.API.CodingEngine.Service.Clients.Judge0;
using Taskverse.API.CodingEngine.Service.Models;
using Taskverse.Data.DataAccess;
using Taskverse.Data.Enums;

namespace Taskverse.API.CodingEngine.Service.Services;

public class PollService : IPollService
{
    private readonly TaskverseContext _context;
    private readonly IJudge0Client _judge0Client;
    private readonly IReportsServiceClient _reportsServiceClient;
    private readonly ILogger<PollService> _logger;

    public PollService(
        TaskverseContext context,
        IJudge0Client judge0Client,
        IReportsServiceClient reportsServiceClient,
        ILogger<PollService> logger)
    {
        _context = context;
        _judge0Client = judge0Client;
        _reportsServiceClient = reportsServiceClient;
        _logger = logger;
    }

    public async Task CollectResultAsync(CodeExecutionRequest request, CancellationToken cancellationToken)
    {
        _context.Attach(request);

        if (string.IsNullOrWhiteSpace(request.Judge0BatchToken) || request.Judge0NodeId is null)
        {
            _logger.LogError("Request '{RequestId}' has no Judge0 batch token or node.", request.CodeExecutionRequestId);
            request.CodeExecutionStatusId = (short)CodeExecutionStatus.Failed;
            request.CompletedAt = DateTime.UtcNow;
            request.ModifiedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            return;
        }

        var node = await _context.Judge0Nodes
            .AsNoTracking()
            .FirstOrDefaultAsync(jn => jn.Id == request.Judge0NodeId.Value, cancellationToken);

        if (node is null)
        {
            _logger.LogError("Request '{RequestId}' references Judge0 node '{NodeId}' which no longer exists.",
                request.CodeExecutionRequestId, request.Judge0NodeId);
            request.CodeExecutionStatusId = (short)CodeExecutionStatus.Failed;
            request.CompletedAt = DateTime.UtcNow;
            request.ModifiedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            return;
        }

        Judge0SubmissionResponse judge0Result;
        try
        {
            judge0Result = await _judge0Client.GetSubmissionAsync(node.BaseUrl, request.Judge0BatchToken, cancellationToken);

            if (judge0Result.Status is null ||
                judge0Result.Status.Id == Judge0StatusCodes.InQueue ||
                judge0Result.Status.Id == Judge0StatusCodes.Processing)
            {
                return;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Judge0 poll failed for request '{RequestId}' on node '{NodeId}', will retry.",
                request.CodeExecutionRequestId, node.Id);
            return;
        }

        var now = DateTime.UtcNow;

        var assessmentCodingQuestions = await _context.AssessmentCodingQuestions
            .Where(acq => acq.AssessmentId == request.AssessmentId)
            .Select(acq => acq.CodingQuestionId)
            .ToListAsync(cancellationToken);

        var codingQuestionIds = assessmentCodingQuestions.Distinct().ToList();

        var testCases = await _context.TestCases
            .Where(tc => codingQuestionIds.Contains(tc.CodingQuestionId) && tc.IsActive)
            .ToListAsync(cancellationToken);

        var totalTestCases = testCases.Count;
        var passedTestCases = 0;
        var testCaseResults = new List<TestCaseExecutionResult>();

        foreach (var testCase in testCases)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = EvaluateSingleTestCase(judge0Result, testCase);
            if (result.Passed) passedTestCases++;

            testCaseResults.Add(result);

            var submission = new CodeExecutionSubmission
            {
                SubmissionId = Guid.NewGuid(),
                CodeExecutionRequestId = request.CodeExecutionRequestId,
                TestCaseId = testCase.TestCaseId,
                CodingLanguageId = request.CodingLanguageId,
                Judge0Token = request.Judge0BatchToken,
                Judge0StatusId = (short?)judge0Result.Status?.Id,
                Judge0StatusDescription = judge0Result.Status?.Description,
                Judge0SubmittedAt = request.StartedAt,
                Judge0CompletedAt = now,
                Stdout = judge0Result.Stdout,
                Stderr = judge0Result.Stderr,
                CompileOutput = judge0Result.CompileOutput,
                ExitCode = judge0Result.ExitCode,
                Passed = result.Passed,
                ActualOutput = result.ActualOutput,
                ExecutionTimeMs = result.ExecutionTimeMs,
                CreatedAt = now
            };

            _context.CodeExecutionSubmissions.Add(submission);
        }

        var executionResult = new CodeExecutionResult
        {
            CodeExecutionResultId = Guid.NewGuid(),
            CodeExecutionRequestId = request.CodeExecutionRequestId,
            CodeExecutionResultStatusId = (short)(passedTestCases == totalTestCases
                ? CodeExecutionResultStatus.Success
                : CodeExecutionResultStatus.Failed),
            StandardOutput = string.Join("\n---\n", testCaseResults.Select(r => r.ActualOutput ?? "")),
            StandardError = judge0Result.Stderr,
            CompilerOutput = judge0Result.CompileOutput,
            ExitCode = judge0Result.ExitCode,
            ExecutionTimeMs = testCaseResults.Sum(r => r.ExecutionTimeMs),
            TotalTestCases = totalTestCases,
            PassedTestCases = passedTestCases,
            CodingScore = totalTestCases > 0
                ? Math.Round((decimal)passedTestCases / totalTestCases * 100, 2)
                : 0,
            CreatedAt = now
        };

        _context.CodeExecutionResults.Add(executionResult);

        request.CodeExecutionStatusId = (short)CodeExecutionStatus.Completed;
        request.CompletedAt = now;
        request.ModifiedAt = now;
        await _context.SaveChangesAsync(cancellationToken);

        await ReleaseNodeSlotAsync(node.Id, cancellationToken);

        try
        {
            await _reportsServiceClient.EvaluateCodingAsync(
                request.AssessmentId,
                Guid.Empty,
                executionResult.CodingScore ?? 0,
                passedTestCases,
                totalTestCases,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to report coding evaluation for request '{RequestId}'.", request.CodeExecutionRequestId);
        }
    }

    private async Task ReleaseNodeSlotAsync(Guid nodeId, CancellationToken cancellationToken)
    {
        await _context.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE judge0_nodes
            SET active_slots = active_slots + 1,
                modified_at = {DateTime.UtcNow}
            WHERE id = {nodeId}", cancellationToken);
    }

    private static TestCaseExecutionResult EvaluateSingleTestCase(Judge0SubmissionResponse judge0Result, TestCase testCase)
    {
        var stdout = judge0Result.Stdout ?? string.Empty;
        var executionTimeMs = 0;
        if (judge0Result.Time is not null && float.TryParse(judge0Result.Time, out var timeSec))
        {
            executionTimeMs = (int)(timeSec * 1000);
        }

        var exitCode = judge0Result.Status?.Id switch
        {
            Judge0StatusCodes.Accepted => 0,
            Judge0StatusCodes.WrongAnswer => 1,
            Judge0StatusCodes.TimeLimitExceeded => -1,
            Judge0StatusCodes.CompilationError or Judge0StatusCodes.CompilationErrorOld => -2,
            Judge0StatusCodes.InternalError => -3,
            _ => judge0Result.ExitCode ?? 1
        };

        var passed = exitCode == 0 && CompareOutput(stdout, testCase.ExpectedOutput, testCase.ComparisonMode, testCase.NumericTolerance);

        var status = exitCode switch
        {
            0 => "Completed",
            -1 => "Timeout",
            -2 => "CompilationError",
            _ => "RuntimeError"
        };

        return new TestCaseExecutionResult
        {
            TestCaseId = testCase.TestCaseId,
            IsSample = testCase.IsSample,
            Passed = passed,
            ActualOutput = stdout,
            ExpectedOutput = testCase.ExpectedOutput,
            ExecutionTimeMs = executionTimeMs,
            Status = status
        };
    }

    private static bool CompareOutput(string? actual, string? expected, int comparisonMode, decimal? numericTolerance)
    {
        if (actual is null && expected is null) return true;
        if (actual is null || expected is null) return false;

        return (ComparisonMode)comparisonMode switch
        {
            ComparisonMode.exact => string.Equals(actual, expected, StringComparison.Ordinal),
            ComparisonMode.trimmed => string.Equals(actual.Trim(), expected.Trim(), StringComparison.Ordinal),
            ComparisonMode.case_insensitive => string.Equals(actual.Trim(), expected.Trim(), StringComparison.OrdinalIgnoreCase),
            ComparisonMode.json => CompareAsJson(actual, expected),
            ComparisonMode.numeric_tolerance => CompareWithNumericTolerance(actual, expected, numericTolerance ?? 0),
            ComparisonMode.unordered_json => CompareAsUnorderedJson(actual, expected),
            _ => string.Equals(actual.Trim(), expected.Trim(), StringComparison.Ordinal)
        };
    }

    private static bool CompareAsJson(string actual, string expected)
    {
        try
        {
            using var actualDoc = JsonDocument.Parse(actual);
            using var expectedDoc = JsonDocument.Parse(expected);
            return JsonSerializer.Serialize(actualDoc.RootElement) == JsonSerializer.Serialize(expectedDoc.RootElement);
        }
        catch (JsonException) { return false; }
    }

    private static bool CompareAsUnorderedJson(string actual, string expected)
    {
        try
        {
            var actualList = JsonSerializer.Deserialize<List<object>>(actual);
            var expectedList = JsonSerializer.Deserialize<List<object>>(expected);
            if (actualList is null || expectedList is null) return false;
            if (actualList.Count != expectedList.Count) return false;
            return new HashSet<object>(actualList).SetEquals(new HashSet<object>(expectedList));
        }
        catch (JsonException) { return false; }
    }

    private static bool CompareWithNumericTolerance(string actual, string expected, decimal tolerance)
    {
        if (decimal.TryParse(actual.Trim(), out var actualVal) && decimal.TryParse(expected.Trim(), out var expectedVal))
            return Math.Abs(actualVal - expectedVal) <= tolerance;
        return string.Equals(actual.Trim(), expected.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}

internal class TestCaseExecutionResult
{
    public Guid TestCaseId { get; set; }
    public bool IsSample { get; set; }
    public bool Passed { get; set; }
    public string? ActualOutput { get; set; }
    public string? ExpectedOutput { get; set; }
    public int ExecutionTimeMs { get; set; }
    public string Status { get; set; } = "Pending";
}
