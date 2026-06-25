using System.Text.Json.Serialization;

namespace Taskverse.API.CodingEngine.Service.Models;

public record RunCodeRequest(
    [property: JsonPropertyName("code")]
    string Code,
    [property: JsonPropertyName("coding_language_id")]
    Guid CodingLanguageId,
    [property: JsonPropertyName("mode")]
    string Mode = "Run");

public record RunCodeResponse(
    [property: JsonPropertyName("execution_request_id")]
    Guid ExecutionRequestId,
    [property: JsonPropertyName("status")]
    string Status,
    [property: JsonPropertyName("test_case_results")]
    List<TestCaseResult>? TestCaseResults,
    [property: JsonPropertyName("total_test_cases")]
    int TotalTestCases,
    [property: JsonPropertyName("passed_test_cases")]
    int PassedTestCases,
    [property: JsonPropertyName("score")]
    decimal? Score,
    [property: JsonPropertyName("reason")]
    string? Reason = null);

public record TestCaseResult(
    [property: JsonPropertyName("test_case_id")]
    Guid TestCaseId,
    [property: JsonPropertyName("is_sample")]
    bool IsSample,
    [property: JsonPropertyName("passed")]
    bool Passed,
    [property: JsonPropertyName("actual_output")]
    string? ActualOutput,
    [property: JsonPropertyName("expected_output")]
    string? ExpectedOutput,
    [property: JsonPropertyName("execution_time_ms")]
    int? ExecutionTimeMs,
    [property: JsonPropertyName("status")]
    string Status);
