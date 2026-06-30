using Taskverse.API.CodingEngine.Service.Models;

namespace Taskverse.API.CodingEngine.Service.Orchestrators;

public interface ICodingEngineOrchestrator
{
    Task<EditorStateResponse> GetEditorStateAsync(Guid assessmentId, Guid codingQuestionId, Guid studentUserId);
    Task<SaveCodeResponse> SaveCodeAsync(Guid assessmentId, Guid codingQuestionId, Guid studentUserId, SaveCodeRequest request);
    Task<RunCodeResponse> RunCodeAsync(Guid assessmentId, Guid codingQuestionId, Guid studentUserId, RunCodeRequest request, CancellationToken cancellationToken = default);
    Task<RunCodeResponse> SubmitCodeAsync(Guid assessmentId, Guid codingQuestionId, Guid studentUserId, RunCodeRequest request, CancellationToken cancellationToken = default);
    Task<RunCodeResponse> GetExecutionStatusAsync(Guid assessmentId, Guid executionRequestId, Guid studentUserId);
}
