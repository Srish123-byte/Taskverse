namespace Taskverse.API.CodingEngine.Worker.Service.Clients;

public interface IReportsServiceClient
{
    Task<CodingEvaluationResultClientModel?> EvaluateCodingAsync(
        Guid attemptId,
        Guid codingQuestionId,
        decimal score,
        int passedTestCases,
        int totalTestCases,
        CancellationToken cancellationToken = default);
}

public sealed record CodingEvaluationResultClientModel(
    [property: System.Text.Json.Serialization.JsonPropertyName("result_id")]
    Guid ResultId,
    [property: System.Text.Json.Serialization.JsonPropertyName("status")]
    string Status);
