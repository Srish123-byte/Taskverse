using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Taskverse.API.CodingEngine.Worker.Service.Clients;
using Taskverse.API.CodingEngine.Worker.Service.Models;
using Taskverse.Data.DataAccess;
using Taskverse.Data.Enums;

namespace Taskverse.API.CodingEngine.Worker.Service.Services;

public class CodeExecutionService : ICodeExecutionService
{
    private static readonly Dictionary<string, int> SupportedLanguages = new(StringComparer.OrdinalIgnoreCase)
    {
        ["c"] = 50,
        ["cpp"] = 54,
        ["java"] = 62,
        ["python"] = 71,
    };

    private readonly TaskverseContext _context;
    private readonly IReportsServiceClient _reportsServiceClient;
    private readonly IJudge0Client _judge0Client;
    private readonly ILogger<CodeExecutionService> _logger;

    public CodeExecutionService(
        TaskverseContext context,
        IReportsServiceClient reportsServiceClient,
        IJudge0Client judge0Client,
        ILogger<CodeExecutionService> logger)
    {
        _context = context;
        _reportsServiceClient = reportsServiceClient;
        _judge0Client = judge0Client;
        _logger = logger;
    }

    public async Task ExecuteCodeAsync(CodeExecutionRequest request, List<TestCase> testCases, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var totalTestCases = testCases.Count;
        var passedTestCases = 0;
        var testCaseResults = new List<TestCaseExecutionResult>();

        request.StartedAt = now;
        request.CodeExecutionStatusId = (short)CodeExecutionStatus.Running;
        request.ModifiedAt = now;
        await _context.SaveChangesAsync(cancellationToken);

        var codingLanguage = await _context.CodingLanguages
            .FirstOrDefaultAsync(cl => cl.CodingLanguageId == request.CodingLanguageId, cancellationToken);

        if (codingLanguage is null)
        {
            _logger.LogError("Coding language '{LanguageId}' not found for request '{RequestId}'.",
                request.CodingLanguageId, request.CodeExecutionRequestId);

            request.CodeExecutionStatusId = (short)CodeExecutionStatus.Failed;
            request.CompletedAt = DateTime.UtcNow;
            request.ModifiedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            return;
        }

        if (!SupportedLanguages.TryGetValue(codingLanguage.LanguageCode, out var judge0LanguageId))
        {
            _logger.LogError("Language '{LanguageCode}' is not supported for Judge0 execution.", codingLanguage.LanguageCode);

            request.CodeExecutionStatusId = (short)CodeExecutionStatus.Failed;
            request.CompletedAt = DateTime.UtcNow;
            request.ModifiedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            return;
        }

        foreach (var testCase in testCases)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var (exitCode, stdout, stderr, executionTimeMs) = await ExecuteViaJudge0Async(
                judge0LanguageId, request.Code, testCase, codingLanguage, request, cancellationToken);

            var passed = CompareOutput(stdout, testCase.ExpectedOutput, testCase.ComparisonMode, testCase.NumericTolerance);
            if (passed)
            {
                passedTestCases++;
            }

            var status = exitCode == 0 ? "Completed" : "RuntimeError";
            if (exitCode == -1) status = "Timeout";
            if (exitCode == -2) status = "CompilationError";

            testCaseResults.Add(new TestCaseExecutionResult
            {
                TestCaseId = testCase.TestCaseId,
                IsSample = testCase.IsSample,
                Passed = passed,
                ActualOutput = stdout,
                ExpectedOutput = testCase.ExpectedOutput,
                ExecutionTimeMs = executionTimeMs,
                Status = status
            });
        }

        var executionResult = new CodeExecutionResult
        {
            CodeExecutionResultId = Guid.NewGuid(),
            CodeExecutionRequestId = request.CodeExecutionRequestId,
            CodeExecutionResultStatusId = (short)(passedTestCases == totalTestCases
                ? CodeExecutionResultStatus.Success
                : CodeExecutionResultStatus.Failed),
            StandardOutput = string.Join("\n---\n", testCaseResults.Select(r => r.ActualOutput ?? "")),
            StandardError = null,
            ExitCode = testCaseResults.Any(r => r.Status is "RuntimeError" or "CompilationError") ? 1 : 0,
            ExecutionTimeMs = testCaseResults.Sum(r => r.ExecutionTimeMs),
            TotalTestCases = totalTestCases,
            PassedTestCases = passedTestCases,
            CodingScore = totalTestCases > 0
                ? Math.Round((decimal)passedTestCases / totalTestCases * 100, 2)
                : 0,
            CreatedAt = DateTime.UtcNow
        };

        _context.CodeExecutionResults.Add(executionResult);

        request.CodeExecutionStatusId = (short)CodeExecutionStatus.Completed;
        request.CompletedAt = DateTime.UtcNow;
        request.ModifiedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

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
            _logger.LogError(ex, "Failed to report coding evaluation for execution request '{RequestId}'.",
                request.CodeExecutionRequestId);
        }
    }

    private async Task<(int ExitCode, string Stdout, string Stderr, int ExecutionTimeMs)> ExecuteViaJudge0Async(
        int judge0LanguageId,
        string code,
        TestCase testCase,
        CodingLanguage codingLanguage,
        CodeExecutionRequest request,
        CancellationToken cancellationToken)
    {
        var judge0Request = new Judge0CreateSubmissionRequest(
            SourceCode: code,
            LanguageId: judge0LanguageId,
            Stdin: testCase.InputData,
            ExpectedOutput: null,
            CpuTimeLimit: (testCase.TimeLimitMs ?? 3000) / 1000f,
            MemoryLimit: testCase.MemoryLimitKb);

        var startTime = DateTime.UtcNow;

        try
        {
            var judge0Result = await _judge0Client.CreateAndWaitAsync(judge0Request, 200, cancellationToken);

            var executionTimeMs = 0;
            decimal? timeSeconds = null;
            if (judge0Result.Time is not null && float.TryParse(judge0Result.Time, out var timeSec))
            {
                executionTimeMs = (int)(timeSec * 1000);
                timeSeconds = (decimal)timeSec;
            }

            var submission = new CodeExecutionSubmission
            {
                SubmissionId = Guid.NewGuid(),
                CodeExecutionRequestId = request.CodeExecutionRequestId,
                TestCaseId = testCase.TestCaseId,
                CodingLanguageId = codingLanguage.CodingLanguageId,
                Judge0Token = judge0Result.Token,
                Judge0StatusId = (short?)judge0Result.Status?.Id,
                Judge0StatusDescription = judge0Result.Status?.Description,
                Judge0SubmittedAt = startTime,
                Judge0CompletedAt = DateTime.UtcNow,
                Stdout = judge0Result.Stdout,
                Stderr = judge0Result.Stderr,
                CompileOutput = judge0Result.CompileOutput,
                ExitCode = judge0Result.ExitCode,
                TimeSeconds = timeSeconds,
                MemoryKilobytes = (int?)judge0Result.Memory,
                CreatedAt = DateTime.UtcNow
            };

            _context.CodeExecutionSubmissions.Add(submission);

            var exitCode = judge0Result.Status?.Id switch
            {
                Judge0StatusCodes.Accepted => 0,
                Judge0StatusCodes.WrongAnswer => 1,
                Judge0StatusCodes.TimeLimitExceeded => -1,
                Judge0StatusCodes.CompilationError or Judge0StatusCodes.CompilationErrorOld => -2,
                Judge0StatusCodes.InternalError => -3,
                _ => judge0Result.ExitCode ?? 1
            };

            return (exitCode, judge0Result.Stdout ?? string.Empty, judge0Result.Stderr ?? string.Empty, executionTimeMs);
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("Judge0 submission timed out for test case '{TestCaseId}'.", testCase.TestCaseId);
            return (-1, string.Empty, "Timeout", 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Judge0 execution failed for test case '{TestCaseId}'.", testCase.TestCaseId);
            return (-3, string.Empty, $"Execution error: {ex.Message}", 0);
        }
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
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool CompareAsUnorderedJson(string actual, string expected)
    {
        try
        {
            var actualList = JsonSerializer.Deserialize<List<object>>(actual);
            var expectedList = JsonSerializer.Deserialize<List<object>>(expected);

            if (actualList is null || expectedList is null) return false;
            if (actualList.Count != expectedList.Count) return false;

            var actualSet = new HashSet<object>(actualList);
            var expectedSet = new HashSet<object>(expectedList);

            return actualSet.SetEquals(expectedSet);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool CompareWithNumericTolerance(string actual, string expected, decimal tolerance)
    {
        if (decimal.TryParse(actual.Trim(), out var actualVal) &&
            decimal.TryParse(expected.Trim(), out var expectedVal))
        {
            return Math.Abs(actualVal - expectedVal) <= tolerance;
        }

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
