using log4net;
using Microsoft.AspNetCore.Http;
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

    public async Task<ProctorSessionDto> StartSession(StartProctorSessionDto dto, Guid studentUserId)
    {
        _log.Debug(
            $"ProctorOrchestrator.StartSession: attemptId={dto.AttemptId}, assessmentId={dto.AssessmentId}, studentUserId={studentUserId}");

        var result = await _microServiceOrchestrator.StartProctorSession(
            dto.AttemptId,
            studentUserId,
            new StartProctorSessionModel(
                dto.AttemptId,
                dto.AssessmentId,
                dto.StudentId,
                dto.StartedAt,
                dto.BrowserName,
                dto.BrowserVersion,
                dto.OperatingSystem,
                dto.DeviceType,
                dto.UserAgent,
                dto.IpAddress));
        result.EnsureSuccess(nameof(StartSession));

        ProctorSessionModel model = result.DeserializeValue<ProctorSessionModel>()
            ?? throw new InvalidOperationException("StartSession returned an empty response.");

        return MapToDto(model);
    }

    public async Task<SessionHeartbeatResponseDto> HeartbeatSession(Guid sessionId, SessionHeartbeatDto dto, Guid studentUserId)
    {
        _log.Debug(
            $"ProctorOrchestrator.HeartbeatSession: sessionId={sessionId}, studentUserId={studentUserId}, visibilityState={dto.VisibilityState}, networkStatus={dto.NetworkStatus}");

        var result = await _microServiceOrchestrator.HeartbeatProctorSession(
            sessionId,
            studentUserId,
            new SessionHeartbeatModel(
                dto.AttemptId,
                dto.ClientTimestamp,
                dto.VisibilityState,
                dto.IsFullscreen,
                dto.NetworkStatus,
                dto.QuestionId));
        result.EnsureSuccess(nameof(HeartbeatSession));

        var model = result.DeserializeValue<SessionHeartbeatResponseModel>()
            ?? throw new InvalidOperationException($"HeartbeatSession returned an empty response for sessionId={sessionId}.");

        return new SessionHeartbeatResponseDto
        {
            SessionId = model.SessionId,
            LastHeartbeatAt = model.LastHeartbeatAt
        };
    }

    public async Task<ProctorEventBatchResultDto> RecordEvents(Guid sessionId, ProctorEventBatchDto dto, Guid studentUserId)
    {
        _log.Debug(
            $"ProctorOrchestrator.RecordEvents: sessionId={sessionId}, studentUserId={studentUserId}, eventCount={dto.Events.Count}");

        var result = await _microServiceOrchestrator.RecordProctorEvents(sessionId, studentUserId, dto.ToModel());
        result.EnsureSuccess(nameof(RecordEvents));

        var model = result.DeserializeValue<ProctorEventBatchResultModel>()
            ?? throw new InvalidOperationException($"RecordEvents returned an empty response for sessionId={sessionId}.");

        return new ProctorEventBatchResultDto
        {
            ProcessedCount = model.ProcessedCount,
            Failures = model.Failures.Select(item => new ProctorEventBatchFailureDto
            {
                Index = item.Index,
                Message = item.Message
            }).ToList()
        };
    }

    public async Task<ProctorSessionDto> EndSession(Guid sessionId, EndProctorSessionDto dto, Guid studentUserId)
    {
        _log.Debug(
            $"ProctorOrchestrator.EndSession: sessionId={sessionId}, studentUserId={studentUserId}, eventType={dto.EventType}");

        var result = await _microServiceOrchestrator.EndProctorSession(
            sessionId,
            studentUserId,
            new EndProctorSessionModel(
                dto.AttemptId,
                dto.EventType,
                dto.ClientTimestamp,
                dto.Severity,
                dto.MetadataJson));
        result.EnsureSuccess(nameof(EndSession));

        var model = result.DeserializeValue<ProctorSessionModel>()
            ?? throw new InvalidOperationException($"EndSession returned an empty response for sessionId={sessionId}.");

        return MapToDto(model);
    }

    public async Task<ProctorSessionStateDto> GetSession(Guid sessionId, Guid studentUserId)
    {
        _log.Debug($"ProctorOrchestrator.GetSession: sessionId={sessionId}, studentUserId={studentUserId}");

        var result = await _microServiceOrchestrator.GetProctorSession(sessionId, studentUserId);
        result.EnsureSuccess(nameof(GetSession));

        ProctorSessionStateModel model = result.DeserializeValue<ProctorSessionStateModel>()
            ?? throw new InvalidOperationException($"GetSession returned an empty response for sessionId={sessionId}.");

        return new ProctorSessionStateDto
        {
            SessionId = model.SessionId,
            AttemptId = model.AttemptId,
            AssessmentId = model.AssessmentId,
            StudentId = model.StudentId,
            Status = model.Status,
            StartedAt = model.StartedAt,
            EndedAt = model.EndedAt,
            BrowserName = model.BrowserName,
            BrowserVersion = model.BrowserVersion,
            OperatingSystem = model.OperatingSystem,
            DeviceType = model.DeviceType,
            UserAgent = model.UserAgent,
            IpAddress = model.IpAddress,
            Summary = new ProctorSessionSummaryDto
            {
                TabSwitchCount = model.Summary.TabSwitchCount,
                FullScreenExitCount = model.Summary.FullScreenExitCount,
                CopyAttemptCount = model.Summary.CopyAttemptCount,
                PasteAttemptCount = model.Summary.PasteAttemptCount,
                CutAttemptCount = model.Summary.CutAttemptCount,
                ContextMenuAttemptCount = model.Summary.ContextMenuAttemptCount,
                BlockedShortcutCount = model.Summary.BlockedShortcutCount,
                PossibleDevtoolsCount = model.Summary.PossibleDevtoolsCount,
                NetworkDisconnectCount = model.Summary.NetworkDisconnectCount,
                RiskScore = model.Summary.RiskScore,
                RiskLevel = model.Summary.RiskLevel,
                LastEventAt = model.Summary.LastEventAt
            }
        };
    }

    public async Task<ProctorSessionStateDto> GetAttemptSession(Guid attemptId, Guid collegeId, string requesterRole, string requesterName)
    {
        _log.Debug(
            $"ProctorOrchestrator.GetAttemptSession: attemptId={attemptId}, collegeId={collegeId}, requesterRole={requesterRole}, requesterName={requesterName}");

        var result = await _microServiceOrchestrator.GetAttemptProctorSession(attemptId, collegeId, requesterRole, requesterName);
        if (result.IsSuccess())
        {
            var model = result.DeserializeValue<ProctorSessionStateModel>()
                ?? throw new InvalidOperationException($"GetAttemptSession returned an empty response for attemptId={attemptId}.");

            return new ProctorSessionStateDto
            {
                SessionId = model.SessionId,
                AttemptId = model.AttemptId,
                AssessmentId = model.AssessmentId,
                StudentId = model.StudentId,
                Status = model.Status,
                StartedAt = model.StartedAt,
                EndedAt = model.EndedAt,
                BrowserName = model.BrowserName,
                BrowserVersion = model.BrowserVersion,
                OperatingSystem = model.OperatingSystem,
                DeviceType = model.DeviceType,
                UserAgent = model.UserAgent,
                IpAddress = model.IpAddress,
                Summary = new ProctorSessionSummaryDto
                {
                    TabSwitchCount = model.Summary.TabSwitchCount,
                    FullScreenExitCount = model.Summary.FullScreenExitCount,
                    CopyAttemptCount = model.Summary.CopyAttemptCount,
                    PasteAttemptCount = model.Summary.PasteAttemptCount,
                    CutAttemptCount = model.Summary.CutAttemptCount,
                    ContextMenuAttemptCount = model.Summary.ContextMenuAttemptCount,
                    BlockedShortcutCount = model.Summary.BlockedShortcutCount,
                    PossibleDevtoolsCount = model.Summary.PossibleDevtoolsCount,
                    NetworkDisconnectCount = model.Summary.NetworkDisconnectCount,
                    RiskScore = model.Summary.RiskScore,
                    RiskLevel = model.Summary.RiskLevel,
                    LastEventAt = model.Summary.LastEventAt
                }
            };
        }

        var message = ExtractMessage(result.Value) ?? $"GetAttemptSession failed with status {result.StatusCode}.";
        throw result.StatusCode switch
        {
            StatusCodes.Status400BadRequest => new ArgumentException(message),
            StatusCodes.Status403Forbidden => new UnauthorizedAccessException(message),
            StatusCodes.Status404NotFound => new KeyNotFoundException(message),
            StatusCodes.Status409Conflict => new InvalidOperationException(message),
            StatusCodes.Status503ServiceUnavailable => new HttpRequestException(message),
            _ => new InvalidOperationException(message)
        };
    }

    public async Task RecordEvent(string sessionId, string eventType, string? payload)
    {
        _log.Debug($"ProctorOrchestrator.RecordEvent: sessionId={sessionId}, eventType={eventType}");

        var result = await _microServiceOrchestrator.RecordProctorEvent(
            new ProctorEventModel(sessionId, eventType, payload, DateTime.UtcNow));

        result.EnsureSuccess(nameof(RecordEvent));
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
            AttemptId = model.AttemptId,
            AssessmentId = model.AssessmentId,
            StudentId = model.StudentId,
            Status = model.Status,
            StartedAt = model.StartedAt,
            EndedAt = model.EndedAt
        };

    private static string? ExtractMessage(object? value)
    {
        if (value is null)
        {
            return null;
        }

        var messageProperty = value.GetType().GetProperty("message");
        if (messageProperty?.GetValue(value) is string message && !string.IsNullOrWhiteSpace(message))
        {
            return message;
        }

        var detailProperty = value.GetType().GetProperty("detail");
        if (detailProperty?.GetValue(value) is string detail && !string.IsNullOrWhiteSpace(detail))
        {
            return detail;
        }

        return null;
    }
}

internal static class ProctorOrchestratorMappings
{
    public static ProctorEventBatchModel ToModel(this ProctorEventBatchDto dto)
    {
        return new ProctorEventBatchModel(
            dto.Events.Select(item => new ProctorEventBatchItemModel(
                item.AttemptId,
                item.EventType,
                item.Severity,
                item.ClientTimestamp,
                item.QuestionId,
                item.MetadataJson)).ToList());
    }
}
