using Microsoft.AspNetCore.Mvc;
using Taskverse.Api.MicroServices.Enums;
using Taskverse.Api.MicroServices.Models;

namespace Taskverse.Api.MicroServices.Orchestrators;

public partial class MicroServiceOrchestrator
{
    public async Task<ObjectResult> GetAssessment(string assessmentId)
    {
        var url = $"{GetMicroServiceUrl(MicroService.Assessment)}assessments/{assessmentId}";
        return await Get<AssessmentModel>(url);
    }

    public async Task<ObjectResult> CreateAssessment(CreateAssessmentModel model)
    {
        var url = $"{GetMicroServiceUrl(MicroService.Assessment)}assessments";
        return await Post<AssessmentModel>(url, model);
    }

    public async Task<ObjectResult> GetAssessmentsByUser(string userId)
    {
        var url = $"{GetMicroServiceUrl(MicroService.Assessment)}assessments/user/{userId}";
        return await Get<List<AssessmentModel>>(url);
    }

    public async Task<ObjectResult> GetAssessmentResult(string assessmentId, string userId)
    {
        var url = $"{GetMicroServiceUrl(MicroService.Assessment)}assessments/{assessmentId}/results/{userId}";
        return await Get<AssessmentResultModel>(url);
    }

    public async Task<ObjectResult> GetAssessmentSummary(string assessmentId)
    {
        var url = $"{GetMicroServiceUrl(MicroService.Assessment)}assessments/{assessmentId}/summary";
        return await Get<AssessmentSummaryModel>(url);
    }
}
