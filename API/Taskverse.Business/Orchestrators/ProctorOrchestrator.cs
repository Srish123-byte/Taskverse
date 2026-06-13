using System.Text.Json;
using log4net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Taskverse.Api.MicroServices.Interfaces;
using Taskverse.Api.MicroServices.Models;
using Taskverse.Api.MicroServices.Utilities;
using Taskverse.Business.Interface;
using Taskverse.Business.Utilities;

namespace Taskverse.Business.Orchestrators;

public class ProctorOrchestrator : IProctorOrchestrator
{
    private readonly IMicroServiceOrchestrator _microServiceOrchestrator;
    private static readonly ILog _log = LogManager.GetLogger(typeof(ProctorOrchestrator));
    private const string ProctorServiceAddressError = "Proctor microservice address is missing or invalid.";
    private const string AssessmentServiceAddressError = "Assessment microservice address is missing or invalid.";

    public ProctorOrchestrator(IMicroServiceOrchestrator microServiceOrchestrator)
    {
        _microServiceOrchestrator = microServiceOrchestrator;
    }

    public async Task<ProctorSessionDto> StartSession(StartProctorSessionDto dto, Guid studentUserId)
    {
        _log.Debug(
            $"ProctorOrchestrator.StartSession: attemptId={dto.AttemptId}, assessmentId={dto.AssessmentId}, studentUserId={studentUserId}");

        var result = await ExecuteMicroServiceOperationAsync(
            () => _microServiceOrchestrator.StartProctorSession(
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
                    dto.IpAddress)),
            ProctorServiceAddressError);
        result.EnsureSuccess(nameof(StartSession));

        ProctorSessionModel model = result.DeserializeValue<ProctorSessionModel>()
            ?? throw new InvalidOperationException("StartSession returned an empty response.");

        return MapToDto(model);
    }

    public async Task<SessionHeartbeatResponseDto> HeartbeatSession(Guid sessionId, SessionHeartbeatDto dto, Guid studentUserId)
    {
        _log.Debug(
            $"ProctorOrchestrator.HeartbeatSession: sessionId={sessionId}, studentUserId={studentUserId}, visibilityState={dto.VisibilityState}, networkStatus={dto.NetworkStatus}");

        var result = await ExecuteMicroServiceOperationAsync(
            () => _microServiceOrchestrator.HeartbeatProctorSession(
                sessionId,
                studentUserId,
                new SessionHeartbeatModel(
                    dto.AttemptId,
                    dto.ClientTimestamp,
                    dto.VisibilityState,
                    dto.IsFullscreen,
                    dto.NetworkStatus,
                    dto.QuestionId)),
            ProctorServiceAddressError);
        result.EnsureSuccess(nameof(HeartbeatSession));

        var model = result.DeserializeValue<SessionHeartbeatResponseModel>()
            ?? throw new InvalidOperationException($"HeartbeatSession returned an empty response for sessionId={sessionId}.");

        var sessionState = await ApplyServerSideThresholdEnforcementAsync(
            sessionId,
            dto.AttemptId,
            studentUserId,
            MapSessionStateToDto(model.SessionState));

        return new SessionHeartbeatResponseDto
        {
            SessionId = model.SessionId,
            LastHeartbeatAt = model.LastHeartbeatAt,
            SessionState = sessionState
        };
    }

    public async Task<ProctorEventBatchResultDto> RecordEvents(Guid sessionId, ProctorEventBatchDto dto, Guid studentUserId)
    {
        _log.Debug(
            $"ProctorOrchestrator.RecordEvents: sessionId={sessionId}, studentUserId={studentUserId}, eventCount={dto.Events.Count}");

        var result = await ExecuteMicroServiceOperationAsync(
            () => _microServiceOrchestrator.RecordProctorEvents(sessionId, studentUserId, dto.ToModel()),
            ProctorServiceAddressError);
        result.EnsureSuccess(nameof(RecordEvents));

        var model = result.DeserializeValue<ProctorEventBatchResultModel>()
            ?? throw new InvalidOperationException($"RecordEvents returned an empty response for sessionId={sessionId}.");

        var sessionState = await ApplyServerSideThresholdEnforcementAsync(
            sessionId,
            model.SessionState.AttemptId,
            studentUserId,
            MapSessionStateToDto(model.SessionState));

        return new ProctorEventBatchResultDto
        {
            ProcessedCount = model.ProcessedCount,
            Failures = model.Failures.Select(item => new ProctorEventBatchFailureDto
            {
                Index = item.Index,
                Message = item.Message
            }).ToList(),
            SessionState = sessionState
        };
    }

    public async Task<ProctorSessionDto> EndSession(Guid sessionId, EndProctorSessionDto dto, Guid studentUserId)
    {
        _log.Debug(
            $"ProctorOrchestrator.EndSession: sessionId={sessionId}, studentUserId={studentUserId}, eventType={dto.EventType}");

        var result = await ExecuteMicroServiceOperationAsync(
            () => _microServiceOrchestrator.EndProctorSession(
                sessionId,
                studentUserId,
                new EndProctorSessionModel(
                    dto.AttemptId,
                    dto.EventType,
                    dto.ClientTimestamp,
                    dto.Severity,
                    dto.MetadataJson)),
            ProctorServiceAddressError);
        result.EnsureSuccess(nameof(EndSession));

        var model = result.DeserializeValue<ProctorSessionModel>()
            ?? throw new InvalidOperationException($"EndSession returned an empty response for sessionId={sessionId}.");

        return MapToDto(model);
    }

    public async Task<ProctorSessionStateDto> GetSessionByAttempt(Guid attemptId, Guid studentUserId)
    {
        _log.Debug($"ProctorOrchestrator.GetSessionByAttempt: attemptId={attemptId}, studentUserId={studentUserId}");

        var result = await ExecuteMicroServiceOperationAsync(
            () => _microServiceOrchestrator.GetProctorSessionByAttempt(attemptId, studentUserId),
            ProctorServiceAddressError);
        if (!result.IsSuccess())
        {
            var message = ExtractMessage(result.Value) ?? $"GetSessionByAttempt failed with status {result.StatusCode}.";
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

        var model = result.DeserializeValue<ProctorSessionStateModel>()
            ?? throw new InvalidOperationException($"GetSessionByAttempt returned an empty response for attemptId={attemptId}.");

        return MapSessionStateToDto(model);
    }

    public async Task<ProctorSessionStateDto> GetSession(Guid sessionId, Guid studentUserId)
    {
        _log.Debug($"ProctorOrchestrator.GetSession: sessionId={sessionId}, studentUserId={studentUserId}");

        var result = await ExecuteMicroServiceOperationAsync(
            () => _microServiceOrchestrator.GetProctorSession(sessionId, studentUserId),
            ProctorServiceAddressError);
        result.EnsureSuccess(nameof(GetSession));

        ProctorSessionStateModel model = result.DeserializeValue<ProctorSessionStateModel>()
            ?? throw new InvalidOperationException($"GetSession returned an empty response for sessionId={sessionId}.");

        return MapSessionStateToDto(model);
    }

    public async Task<ProctorSessionStateDto> GetAttemptSession(Guid attemptId, Guid collegeId, string requesterRole, string requesterName)
    {
        _log.Debug(
            $"ProctorOrchestrator.GetAttemptSession: attemptId={attemptId}, collegeId={collegeId}, requesterRole={requesterRole}, requesterName={requesterName}");

        var result = await ExecuteMicroServiceOperationAsync(
            () => _microServiceOrchestrator.GetAttemptProctorSession(attemptId, collegeId, requesterRole, requesterName),
            AssessmentServiceAddressError);
        if (result.IsSuccess())
        {
            var model = result.DeserializeValue<ProctorSessionStateModel>()
                ?? throw new InvalidOperationException($"GetAttemptSession returned an empty response for attemptId={attemptId}.");

            return MapSessionStateToDto(model);
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

        var result = await ExecuteMicroServiceOperationAsync(
            () => _microServiceOrchestrator.RecordProctorEvent(
                new ProctorEventModel(sessionId, eventType, payload, DateTime.UtcNow)),
            ProctorServiceAddressError);

        result.EnsureSuccess(nameof(RecordEvent));
    }

    public async Task<ProctorSummaryDto> GetSummary(string sessionId)
    {
        _log.Debug($"ProctorOrchestrator.GetSummary: sessionId={sessionId}");

        var result = await ExecuteMicroServiceOperationAsync(
            () => _microServiceOrchestrator.GetProctorSummary(sessionId),
            ProctorServiceAddressError);
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

    private async Task<ProctorSessionStateDto> ApplyServerSideThresholdEnforcementAsync(
        Guid sessionId,
        Guid attemptId,
        Guid studentUserId,
        ProctorSessionStateDto sessionState)
    {
        if (!string.Equals(sessionState.Enforcement.Action, "AUTO_SUBMIT", StringComparison.OrdinalIgnoreCase))
        {
            return sessionState;
        }

        await SubmitAttemptForEnforcementAsync(attemptId, studentUserId);
        await EndSessionForEnforcementAsync(sessionId, attemptId, studentUserId, sessionState.Enforcement);

        var refreshedState = await GetSession(sessionId, studentUserId);
        refreshedState.Enforcement.Action = "AUTO_SUBMIT";
        refreshedState.Enforcement.TriggeredByEventType = sessionState.Enforcement.TriggeredByEventType;
        refreshedState.Enforcement.Message = sessionState.Enforcement.Message;

        return refreshedState;
    }

    private async Task SubmitAttemptForEnforcementAsync(Guid attemptId, Guid studentUserId)
    {
        var result = await ExecuteMicroServiceOperationAsync(
            () => _microServiceOrchestrator.SubmitStudentAttempt(attemptId, studentUserId),
            AssessmentServiceAddressError);

        if (result.IsSuccess())
        {
            return;
        }

        if (result.StatusCode == StatusCodes.Status409Conflict)
        {
            return;
        }

        var message = ExtractMessage(result.Value) ?? $"SubmitStudentAttempt failed with status {result.StatusCode}.";
        throw result.StatusCode switch
        {
            StatusCodes.Status400BadRequest => new ArgumentException(message),
            StatusCodes.Status404NotFound => new KeyNotFoundException(message),
            StatusCodes.Status503ServiceUnavailable => new HttpRequestException(message),
            _ => new InvalidOperationException(message)
        };
    }

    private async Task EndSessionForEnforcementAsync(
        Guid sessionId,
        Guid attemptId,
        Guid studentUserId,
        ProctorSessionEnforcementDto enforcement)
    {
        var metadataJson = JsonSerializer.Serialize(new
        {
            source = "server_threshold_enforcement",
            enforcementAction = enforcement.Action,
            triggeredByEventType = enforcement.TriggeredByEventType,
            message = enforcement.Message
        });

        var result = await ExecuteMicroServiceOperationAsync(
            () => _microServiceOrchestrator.EndProctorSession(
                sessionId,
                studentUserId,
                new EndProctorSessionModel(
                    attemptId,
                    nameof(Taskverse.Data.Enums.EventType.ASSESSMENT_AUTO_SUBMITTED),
                    DateTime.UtcNow,
                    "High",
                    metadataJson)),
            ProctorServiceAddressError);

        if (result.IsSuccess() || result.StatusCode == StatusCodes.Status409Conflict)
        {
            return;
        }

        var message = ExtractMessage(result.Value) ?? $"EndProctorSession failed with status {result.StatusCode}.";
        throw result.StatusCode switch
        {
            StatusCodes.Status400BadRequest => new ArgumentException(message),
            StatusCodes.Status403Forbidden => new UnauthorizedAccessException(message),
            StatusCodes.Status404NotFound => new KeyNotFoundException(message),
            StatusCodes.Status503ServiceUnavailable => new HttpRequestException(message),
            _ => new InvalidOperationException(message)
        };
    }

    private static ProctorSessionStateDto MapSessionStateToDto(ProctorSessionStateModel model)
        => new()
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
            },
            Rules = model.Rules.Select(item => new ProctorSessionRuleDto
            {
                EventType = item.EventType,
                DisplayName = item.DisplayName,
                WarningMessage = item.WarningMessage,
                CurrentCount = item.CurrentCount,
                MaxAllowedCount = item.MaxAllowedCount,
                RemainingCount = item.RemainingCount,
                IsEnabled = item.IsEnabled,
                LockAttemptOnLimitExceeded = item.LockAttemptOnLimitExceeded,
                AutoSubmitOnLimitExceeded = item.AutoSubmitOnLimitExceeded,
                IsThresholdExceeded = item.IsThresholdExceeded
            }).ToList(),
            Enforcement = new ProctorSessionEnforcementDto
            {
                Action = model.Enforcement.Action,
                TriggeredByEventType = model.Enforcement.TriggeredByEventType,
                Message = model.Enforcement.Message
            }
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

    private static async Task<ObjectResult> ExecuteMicroServiceOperationAsync(
        Func<Task<ObjectResult>> operation,
        string addressErrorMessage)
    {
        try
        {
            return await operation();
        }
        catch (InvalidOperationException ex) when (string.Equals(ex.Message, MicroServiceBusinessCondition.AddressNotFound, StringComparison.Ordinal))
        {
            throw new HttpRequestException(addressErrorMessage, ex);
        }
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
