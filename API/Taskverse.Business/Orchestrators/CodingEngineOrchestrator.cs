using log4net;
using Taskverse.Api.MicroServices.Interfaces;
using Taskverse.Api.MicroServices.Models;
using Taskverse.Business.Interface;
using Taskverse.Business.Utilities;

namespace Taskverse.Business.Orchestrators;

public class CodingEngineOrchestrator : ICodingEngineOrchestrator
{
    private readonly IMicroServiceOrchestrator _microServiceOrchestrator;
    private static readonly ILog _log = LogManager.GetLogger(typeof(CodingEngineOrchestrator));

    public CodingEngineOrchestrator(IMicroServiceOrchestrator microServiceOrchestrator)
    {
        _microServiceOrchestrator = microServiceOrchestrator;
    }

    public async Task<ChallengeDto> GetChallenge(string challengeId)
    {
        _log.Debug($"CodingEngineOrchestrator.GetChallenge: challengeId={challengeId}");

        var result = await _microServiceOrchestrator.GetChallenge(challengeId);
        result.EnsureSuccess(nameof(GetChallenge));

        ChallengeModel model = result.DeserializeValue<ChallengeModel>()
            ?? throw new InvalidOperationException($"GetChallenge returned an empty response for challengeId={challengeId}.");

        return MapToDto(model);
    }

    public async Task<CodeExecutionResultDto> ExecuteCode(string challengeId, string userId, string language, string code)
    {
        _log.Debug($"CodingEngineOrchestrator.ExecuteCode: challengeId={challengeId}, userId={userId}, language={language}");

        var result = await _microServiceOrchestrator.ExecuteCode(
            new CodeExecutionRequestModel(challengeId, userId, language, code));

        result.EnsureSuccess(nameof(ExecuteCode));

        CodeExecutionResultModel model = result.DeserializeValue<CodeExecutionResultModel>()
            ?? throw new InvalidOperationException("ExecuteCode returned an empty response.");

        return MapToDto(model);
    }

    public async Task<CodeExecutionResultDto> SubmitCode(string challengeId, string userId, string language, string code)
    {
        _log.Debug($"CodingEngineOrchestrator.SubmitCode: challengeId={challengeId}, userId={userId}, language={language}");

        var result = await _microServiceOrchestrator.SubmitCode(
            new CodeExecutionRequestModel(challengeId, userId, language, code));

        result.EnsureSuccess(nameof(SubmitCode));

        CodeExecutionResultModel model = result.DeserializeValue<CodeExecutionResultModel>()
            ?? throw new InvalidOperationException("SubmitCode returned an empty response.");

        return MapToDto(model);
    }

    public async Task<CodeExecutionResultDto> GetSubmission(string submissionId)
    {
        _log.Debug($"CodingEngineOrchestrator.GetSubmission: submissionId={submissionId}");

        var result = await _microServiceOrchestrator.GetSubmission(submissionId);
        result.EnsureSuccess(nameof(GetSubmission));

        CodeExecutionResultModel model = result.DeserializeValue<CodeExecutionResultModel>()
            ?? throw new InvalidOperationException($"GetSubmission returned an empty response for submissionId={submissionId}.");

        return MapToDto(model);
    }

    public async Task<List<CodeExecutionResultDto>> GetSubmissionsByUser(string userId)
    {
        _log.Debug($"CodingEngineOrchestrator.GetSubmissionsByUser: userId={userId}");

        var result = await _microServiceOrchestrator.GetSubmissionsByUser(userId);
        result.EnsureSuccess(nameof(GetSubmissionsByUser));

        List<CodeExecutionResultModel> models = result.DeserializeValue<List<CodeExecutionResultModel>>()
            ?? throw new InvalidOperationException($"GetSubmissionsByUser returned an empty response for userId={userId}.");

        return models.Select(MapToDto).ToList();
    }

    public async Task<List<ChallengeDto>> GetChallengesByAssessment(string assessmentId)
    {
        _log.Debug($"CodingEngineOrchestrator.GetChallengesByAssessment: assessmentId={assessmentId}");

        var result = await _microServiceOrchestrator.GetChallengesByAssessment(assessmentId);
        result.EnsureSuccess(nameof(GetChallengesByAssessment));

        List<ChallengeModel> models = result.DeserializeValue<List<ChallengeModel>>()
            ?? throw new InvalidOperationException($"GetChallengesByAssessment returned an empty response for assessmentId={assessmentId}.");

        return models.Select(MapToDto).ToList();
    }

    private static ChallengeDto MapToDto(ChallengeModel model)
        => new()
        {
            ChallengeId = model.ChallengeId,
            Title = model.Title,
            Description = model.Description,
            Difficulty = model.Difficulty,
            Languages = model.Languages,
            TimeLimit = model.TimeLimit,
            MemoryLimit = model.MemoryLimit,
            IsActive = model.IsActive
        };

    private static CodeExecutionResultDto MapToDto(CodeExecutionResultModel model)
        => new()
        {
            SubmissionId = model.SubmissionId,
            Status = model.Status,
            Output = model.Output,
            ErrorOutput = model.ErrorOutput,
            ExecutionTimeMs = model.ExecutionTimeMs,
            MemoryUsedKb = model.MemoryUsedKb,
            TestCasesPassed = model.TestCasesPassed,
            TotalTestCases = model.TotalTestCases,
            Score = model.Score
        };
}
