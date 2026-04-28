using log4net;
using Taskverse.Api.MicroServices.Interfaces;
using Taskverse.Api.MicroServices.Models;
using Taskverse.Business.DTOs;
using Taskverse.Business.Interface;
using Taskverse.Business.Mappings;
using Taskverse.Business.Utilities;

namespace Taskverse.Business.Orchestrators;

public class AssessmentOrchestrator : IAssessmentOrchestrator
{
    private readonly IMicroServiceOrchestrator _microServiceOrchestrator;
    private readonly IAssessmentManager _assessmentManager;
    private static readonly ILog _log = LogManager.GetLogger(typeof(AssessmentOrchestrator));

    public AssessmentOrchestrator(
        IMicroServiceOrchestrator microServiceOrchestrator,
        IAssessmentManager assessmentManager)
    {
        _microServiceOrchestrator = microServiceOrchestrator;
        _assessmentManager = assessmentManager;
    }

    public async Task<AssessmentDto> GetAssessment(string assessmentId)
    {
        _log.Debug($"AssessmentOrchestrator.GetAssessment: assessmentId={assessmentId}");

        var result = await _microServiceOrchestrator.GetAssessment(assessmentId);
        result.EnsureSuccess(nameof(GetAssessment));

        AssessmentModel model = result.DeserializeValue<AssessmentModel>()
            ?? throw new InvalidOperationException($"GetAssessment returned an empty response for assessmentId={assessmentId}.");

        return model.ToDto();
    }

    public async Task<AssessmentDto> CreateAssessment(CreateAssessmentDto dto)
    {
        _log.Debug($"AssessmentOrchestrator.CreateAssessment: title={dto.Title}");

        var result = await _microServiceOrchestrator.CreateAssessment(dto.ToMicroServiceModel());
        result.EnsureSuccess(nameof(CreateAssessment));

        AssessmentModel model = result.DeserializeValue<AssessmentModel>()
            ?? throw new InvalidOperationException("CreateAssessment returned an empty response.");

        return model.ToDto();
    }

    public async Task<List<AssessmentDto>> GetAssessmentsByUser(string userId)
    {
        _log.Debug($"AssessmentOrchestrator.GetAssessmentsByUser: userId={userId}");

        var result = await _microServiceOrchestrator.GetAssessmentsByUser(userId);
        result.EnsureSuccess(nameof(GetAssessmentsByUser));

        List<AssessmentModel> models = result.DeserializeValue<List<AssessmentModel>>()
            ?? throw new InvalidOperationException($"GetAssessmentsByUser returned an empty response for userId={userId}.");

        return models.Select(a => a.ToDto()).ToList();
    }

    public async Task<AssessmentResultDto> GetAssessmentResult(string assessmentId, string userId)
    {
        _log.Debug($"AssessmentOrchestrator.GetAssessmentResult: assessmentId={assessmentId}, userId={userId}");

        var result = await _microServiceOrchestrator.GetAssessmentResult(assessmentId, userId);
        result.EnsureSuccess(nameof(GetAssessmentResult));

        AssessmentResultModel model = result.DeserializeValue<AssessmentResultModel>()
            ?? throw new InvalidOperationException($"GetAssessmentResult returned an empty response for assessmentId={assessmentId}, userId={userId}.");

        return model.ToDto();
    }

    public async Task<AssessmentSummaryDto> GetAssessmentSummary(string assessmentId)
    {
        _log.Debug($"AssessmentOrchestrator.GetAssessmentSummary: assessmentId={assessmentId}");

        var result = await _microServiceOrchestrator.GetAssessmentSummary(assessmentId);
        result.EnsureSuccess(nameof(GetAssessmentSummary));

        AssessmentSummaryModel model = result.DeserializeValue<AssessmentSummaryModel>()
            ?? throw new InvalidOperationException($"GetAssessmentSummary returned an empty response for assessmentId={assessmentId}.");

        return model.ToDto();
    }
}
