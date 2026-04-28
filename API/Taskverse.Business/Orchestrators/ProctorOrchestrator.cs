using log4net;
using Taskverse.Api.MicroServices.Interfaces;
using Taskverse.Api.MicroServices.Models;
using Taskverse.Business.Interface;
using Taskverse.Business.Utilities;

namespace Taskverse.Business.Orchestrators;

public class ProctorOrchestrator : IProctorOrchestrator
{
    private readonly IMicroServiceOrchestrator _microServiceOrchestrator;
    private static readonly ILog _log = LogManager.GetLogger(typeof(ProctorOrchestrator));

    public ProctorOrchestrator(IMicroServiceOrchestrator microServiceOrchestrator)
    {
        _microServiceOrchestrator = microServiceOrchestrator;
    }

    public async Task<ProctorSessionDto> StartSession(string examId, string userId)
    {
        _log.Debug($"ProctorOrchestrator.StartSession: examId={examId}, userId={userId}");

        var result = await _microServiceOrchestrator.StartProctorSession(new StartProctorSessionModel(examId, userId));
        result.EnsureSuccess(nameof(StartSession));

        ProctorSessionModel model = result.DeserializeValue<ProctorSessionModel>()
            ?? throw new InvalidOperationException("StartSession returned an empty response.");

        return MapToDto(model);
    }

    public async Task<ProctorSessionDto> GetSession(string sessionId)
    {
        _log.Debug($"ProctorOrchestrator.GetSession: sessionId={sessionId}");

        var result = await _microServiceOrchestrator.GetProctorSession(sessionId);
        result.EnsureSuccess(nameof(GetSession));

        ProctorSessionModel model = result.DeserializeValue<ProctorSessionModel>()
            ?? throw new InvalidOperationException($"GetSession returned an empty response for sessionId={sessionId}.");

        return MapToDto(model);
    }

    public async Task RecordEvent(string sessionId, string eventType, string? payload)
    {
        _log.Debug($"ProctorOrchestrator.RecordEvent: sessionId={sessionId}, eventType={eventType}");

        var result = await _microServiceOrchestrator.RecordProctorEvent(
            new ProctorEventModel(sessionId, eventType, payload, DateTime.UtcNow));

        result.EnsureSuccess(nameof(RecordEvent));
    }

    public async Task EndSession(string sessionId)
    {
        _log.Debug($"ProctorOrchestrator.EndSession: sessionId={sessionId}");

        var result = await _microServiceOrchestrator.EndProctorSession(sessionId);
        result.EnsureSuccess(nameof(EndSession));
    }

    public async Task<ProctorSummaryDto> GetSummary(string sessionId)
    {
        _log.Debug($"ProctorOrchestrator.GetSummary: sessionId={sessionId}");

        var result = await _microServiceOrchestrator.GetProctorSummary(sessionId);
        result.EnsureSuccess(nameof(GetSummary));

        ProctorSummaryModel model = result.DeserializeValue<ProctorSummaryModel>()
            ?? throw new InvalidOperationException($"GetSummary returned an empty response for sessionId={sessionId}.");

        return new ProctorSummaryDto
        {
            SessionId = model.SessionId,
            TotalFlags = model.TotalFlags,
            HighSeverityFlags = model.HighSeverityFlags,
            IsApproved = model.IsApproved,
            ReviewedBy = model.ReviewedBy
        };
    }

    private static ProctorSessionDto MapToDto(ProctorSessionModel model)
        => new()
        {
            SessionId = model.SessionId,
            ExamId = model.ExamId,
            UserId = model.UserId,
            Status = model.Status,
            StartedAt = model.StartedAt,
            EndedAt = model.EndedAt,
            TotalFlags = model.Flags.Count
        };
}
