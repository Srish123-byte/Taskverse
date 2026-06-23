using Microsoft.AspNetCore.Mvc;
using Taskverse.Api.MicroServices.Enums;
using Taskverse.Api.MicroServices.Models;

namespace Taskverse.Api.MicroServices.Orchestrators;

public partial class MicroServiceOrchestrator
{
    public async Task<ObjectResult> GetChallenge(string challengeId)
    {
        var url = $"{GetMicroServiceUrl(MicroService.CodingEngine)}challenges/{challengeId}";
        return await Get<ChallengeModel>(url);
    }

    public async Task<ObjectResult> ExecuteCode(CodeExecutionRequestModel model)
    {
        var url = $"{GetMicroServiceUrl(MicroService.CodingEngine)}code/execute";
        return await Post<CodeExecutionResultModel>(url, model);
    }

    public async Task<ObjectResult> GetSubmission(string submissionId)
    {
        var url = $"{GetMicroServiceUrl(MicroService.CodingEngine)}submissions/{submissionId}";
        return await Get<CodeSubmissionModel>(url);
    }

    public async Task<ObjectResult> GetSubmissionsByUser(string userId)
    {
        var url = $"{GetMicroServiceUrl(MicroService.CodingEngine)}submissions/user/{userId}";
        return await Get<List<CodeSubmissionModel>>(url);
    }

    public async Task<ObjectResult> GetChallengesByAssessment(string assessmentId)
    {
        var url = $"{GetMicroServiceUrl(MicroService.CodingEngine)}challenges/assessment/{assessmentId}";
        return await Get<List<ChallengeModel>>(url);
    }
}
