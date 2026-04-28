using Microsoft.AspNetCore.Mvc;
using Taskverse.Api.MicroServices.Enums;
using Taskverse.Api.MicroServices.Models;

namespace Taskverse.Api.MicroServices.Orchestrators;

public partial class MicroServiceOrchestrator
{
    public async Task<ObjectResult> StartProctorSession(StartProctorSessionModel model)
    {
        var url = $"{GetMicroServiceUrl(MicroService.Proctor)}proctor/sessions";
        return await Post<ProctorSessionModel>(url, model);
    }

    public async Task<ObjectResult> GetProctorSession(string sessionId)
    {
        var url = $"{GetMicroServiceUrl(MicroService.Proctor)}proctor/sessions/{sessionId}";
        return await Get<ProctorSessionModel>(url);
    }

    public async Task<ObjectResult> RecordProctorEvent(ProctorEventModel model)
    {
        var url = $"{GetMicroServiceUrl(MicroService.Proctor)}proctor/events";
        return await Post<object>(url, model);
    }

    public async Task<ObjectResult> EndProctorSession(string sessionId)
    {
        var url = $"{GetMicroServiceUrl(MicroService.Proctor)}proctor/sessions/{sessionId}/end";
        return await Put<ProctorSessionModel>(url, new { });
    }

    public async Task<ObjectResult> GetProctorSummary(string sessionId)
    {
        var url = $"{GetMicroServiceUrl(MicroService.Proctor)}proctor/sessions/{sessionId}/summary";
        return await Get<ProctorSummaryModel>(url);
    }
}
