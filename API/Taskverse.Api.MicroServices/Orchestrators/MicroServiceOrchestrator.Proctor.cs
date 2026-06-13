using Microsoft.AspNetCore.Mvc;
using Taskverse.Api.MicroServices.Enums;
using Taskverse.Api.MicroServices.Models;

namespace Taskverse.Api.MicroServices.Orchestrators;

public partial class MicroServiceOrchestrator
{
    public async Task<ObjectResult> StartProctorSession(Guid attemptId, Guid studentUserId, StartProctorSessionModel model)
    {
        var url =
            $"{GetMicroServiceUrl(MicroService.Proctor)}api/v1/proctor/attempts/{attemptId}/session?studentUserId={studentUserId}";
        return await Post<ProctorSessionModel>(url, model);
    }

    public async Task<ObjectResult> HeartbeatProctorSession(Guid sessionId, Guid studentUserId, SessionHeartbeatModel model)
    {
        var url =
            $"{GetMicroServiceUrl(MicroService.Proctor)}api/v1/sessionhealth/sessions/{sessionId}/heartbeat?studentUserId={studentUserId}";
        return await Post<SessionHeartbeatResponseModel>(url, model);
    }

    public async Task<ObjectResult> RecordProctorEvents(Guid sessionId, Guid studentUserId, ProctorEventBatchModel model)
    {
        var url =
            $"{GetMicroServiceUrl(MicroService.Proctor)}api/v1/proctor/session/{sessionId}/event?studentUserId={studentUserId}";
        return await Post<ProctorEventBatchResultModel>(url, model);
    }

    public async Task<ObjectResult> GetProctorSession(Guid sessionId, Guid studentUserId)
    {
        var url =
            $"{GetMicroServiceUrl(MicroService.Proctor)}api/v1/proctor/sessions/{sessionId}?studentUserId={studentUserId}";
        return await Get<ProctorSessionStateModel>(url);
    }

    public async Task<ObjectResult> GetProctorSessionByAttempt(Guid attemptId, Guid studentUserId)
    {
        var url =
            $"{GetMicroServiceUrl(MicroService.Proctor)}api/v1/proctor/attempts/{attemptId}/session?studentUserId={studentUserId}";
        return await Get<ProctorSessionStateModel>(url);
    }

    public async Task<ObjectResult> GetAttemptProctorSession(Guid attemptId, Guid collegeId, string requesterRole, string requesterName)
    {
        var encodedRequesterRole = Uri.EscapeDataString(requesterRole ?? string.Empty);
        var encodedRequesterName = Uri.EscapeDataString(requesterName ?? string.Empty);
        var url =
            $"{GetMicroServiceUrl(MicroService.Assessment)}api/assessments/attempts/{attemptId}/proctor-session" +
            $"?collegeId={collegeId}&requesterRole={encodedRequesterRole}&requesterName={encodedRequesterName}";
        return await Get<ProctorSessionStateModel>(url);
    }

    public async Task<ObjectResult> RecordProctorEvent(ProctorEventModel model)
    {
        var url = $"{GetMicroServiceUrl(MicroService.Proctor)}proctor/events";
        return await Post<object>(url, model);
    }

    public async Task<ObjectResult> EndProctorSession(Guid sessionId, Guid studentUserId, EndProctorSessionModel model)
    {
        var url =
            $"{GetMicroServiceUrl(MicroService.Proctor)}api/v1/proctor/session/{sessionId}/end?studentUserId={studentUserId}";
        return await Post<ProctorSessionModel>(url, model);
    }

    public async Task<ObjectResult> GetProctorSummary(string sessionId)
    {
        var url = $"{GetMicroServiceUrl(MicroService.Proctor)}proctor/sessions/{sessionId}/summary";
        return await Get<ProctorSummaryModel>(url);
    }
}
