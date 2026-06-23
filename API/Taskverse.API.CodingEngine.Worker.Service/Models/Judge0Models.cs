using System.Text.Json.Serialization;

namespace Taskverse.API.CodingEngine.Worker.Service.Models;

public record Judge0CreateSubmissionRequest(
    [property: JsonPropertyName("source_code")]
    string SourceCode,
    [property: JsonPropertyName("language_id")]
    int LanguageId,
    [property: JsonPropertyName("stdin")]
    string? Stdin,
    [property: JsonPropertyName("expected_output")]
    string? ExpectedOutput,
    [property: JsonPropertyName("cpu_time_limit")]
    float? CpuTimeLimit,
    [property: JsonPropertyName("memory_limit")]
    float? MemoryLimit);

public record Judge0SubmissionResponse(
    [property: JsonPropertyName("token")]
    string? Token,
    [property: JsonPropertyName("stdout")]
    string? Stdout,
    [property: JsonPropertyName("stderr")]
    string? Stderr,
    [property: JsonPropertyName("compile_output")]
    string? CompileOutput,
    [property: JsonPropertyName("message")]
    string? Message,
    [property: JsonPropertyName("status")]
    Judge0Status? Status,
    [property: JsonPropertyName("time")]
    string? Time,
    [property: JsonPropertyName("memory")]
    float? Memory,
    [property: JsonPropertyName("exit_code")]
    int? ExitCode,
    [property: JsonPropertyName("exit_signal")]
    int? ExitSignal,
    [property: JsonPropertyName("created_at")]
    DateTime? CreatedAt,
    [property: JsonPropertyName("finished_at")]
    DateTime? FinishedAt);

public record Judge0Status(
    [property: JsonPropertyName("id")]
    int Id,
    [property: JsonPropertyName("description")]
    string Description);

public static class Judge0StatusCodes
{
    public const int InQueue = 1;
    public const int Processing = 2;
    public const int Accepted = 3;
    public const int WrongAnswer = 4;
    public const int TimeLimitExceeded = 5;
    public const int CompilationError = 6;
    public const int RuntimeErrorSigSegv = 7;
    public const int RuntimeErrorSigXfsz = 8;
    public const int RuntimeErrorSigFpe = 9;
    public const int RuntimeErrorSigPipe = 10;
    public const int CompilationErrorOld = 11;
    public const int RuntimeError = 12;
    public const int InternalError = 13;
    public const int ExecFormatError = 14;
}
