using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Taskverse.API.CodingEngine.Worker.Service.Clients;
using Taskverse.Data.DataAccess;
using Taskverse.Data.Enums;

namespace Taskverse.API.CodingEngine.Worker.Service.Services;

public class CodeExecutionService : ICodeExecutionService
{
    private readonly TaskverseContext _context;
    private readonly IReportsServiceClient _reportsServiceClient;
    private readonly ILogger<CodeExecutionService> _logger;

    public CodeExecutionService(
        TaskverseContext context,
        IReportsServiceClient reportsServiceClient,
        ILogger<CodeExecutionService> logger)
    {
        _context = context;
        _reportsServiceClient = reportsServiceClient;
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

        foreach (var testCase in testCases)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var sw = Stopwatch.StartNew();
            var timeLimit = testCase.TimeLimitMs ?? 3000;
            var (exitCode, stdout, stderr) = await ExecuteProcessAsync(
                codingLanguage, request.Code, testCase.InputData, timeLimit,
                cancellationToken);
            sw.Stop();

            var passed = CompareOutput(stdout, testCase.ExpectedOutput, testCase.ComparisonMode, testCase.NumericTolerance);
            if (passed)
            {
                passedTestCases++;
            }

            testCaseResults.Add(new TestCaseExecutionResult
            {
                TestCaseId = testCase.TestCaseId,
                IsSample = testCase.IsSample,
                Passed = passed,
                ActualOutput = stdout,
                ExpectedOutput = testCase.ExpectedOutput,
                ExecutionTimeMs = (int)sw.ElapsedMilliseconds,
                Status = exitCode == 0 ? "Completed" : "RuntimeError"
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
            ExitCode = testCaseResults.Any(r => r.Status == "RuntimeError") ? 1 : 0,
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

    private async Task<(int ExitCode, string Stdout, string Stderr)> ExecuteProcessAsync(
        CodingLanguage language,
        string code,
        string? inputData,
        int timeLimitMs,
        CancellationToken cancellationToken)
    {
        var (fileName, arguments, tempFile) = PrepareProcessArguments(language, code);

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardInput = !string.IsNullOrEmpty(inputData),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = psi };
            process.Start();

            if (!string.IsNullOrEmpty(inputData))
            {
                await process.StandardInput.WriteAsync(inputData);
                process.StandardInput.Close();
            }

            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();

            var completed = process.WaitForExit(timeLimitMs > 0 ? timeLimitMs : 3000);

            if (!completed)
            {
                process.Kill(entireProcessTree: true);
                return (ExitCode: -1, Stdout: string.Empty, Stderr: "Timeout");
            }

            var stdout = await stdoutTask;
            var stderr = await stderrTask;

            return (process.ExitCode, stdout ?? string.Empty, stderr ?? string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute process for language '{LanguageCode}'.", language.LanguageCode);
            return (-1, string.Empty, $"Execution error: {ex.Message}");
        }
        finally
        {
            if (!string.IsNullOrEmpty(tempFile))
            {
                try { File.Delete(tempFile); } catch { /* best effort cleanup */ }
            }
        }
    }

    private static (string FileName, string Arguments, string? TempFile) PrepareProcessArguments(CodingLanguage language, string code)
    {
        var tempDir = Path.GetTempPath();
        var tempFile = Path.Combine(tempDir, $"code_{Guid.NewGuid()}{language.FileExtension ?? ".txt"}");

        File.WriteAllText(tempFile, code);

        return language.LanguageCode.ToLowerInvariant() switch
        {
            "python" => ("python", $"\"{tempFile}\"", tempFile),
            "javascript" => ("node", $"\"{tempFile}\"", tempFile),
            "typescript" => ("npx", $"ts-node \"{tempFile}\"", tempFile),
            "java" => ("java", $"-cp \"{tempDir}\" \"{Path.GetFileNameWithoutExtension(tempFile)}\"", tempFile),
            "csharp" => ("dotnet", $"script \"{tempFile}\"", tempFile),
            "cpp" => (FindExecutable("g++"), $"-o \"{tempFile}.exe\" \"{tempFile}\" && \"{tempFile}.exe\"", tempFile),
            "c" => (FindExecutable("gcc"), $"-o \"{tempFile}.exe\" \"{tempFile}\" && \"{tempFile}.exe\"", tempFile),
            _ => ("python", $"\"{tempFile}\"", tempFile)
        };
    }

    private static string FindExecutable(string name)
    {
        try
        {
            var psi = new ProcessStartInfo("where", name)
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var process = new Process { StartInfo = psi };
            process.Start();
            var path = process.StandardOutput.ReadLine();
            process.WaitForExit();
            return path ?? name;
        }
        catch
        {
            return name;
        }
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
