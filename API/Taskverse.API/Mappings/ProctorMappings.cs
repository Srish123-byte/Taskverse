using Taskverse.Api.Models;
using Taskverse.Business.Interface;
using Taskverse.Data.Utilities;

namespace Taskverse.Api.Mappings;

public static class ProctorMappings
{
    public static StartProctorSessionDto ToDto(
        this StartProctorSessionRequestModel model,
        Guid attemptId)
    {
        return new StartProctorSessionDto
        {
            AttemptId = model.AttemptId == Guid.Empty ? attemptId : model.AttemptId,
            AssessmentId = model.AssessmentId,
            StudentId = model.StudentId,
            StartedAt = UtcDateTime.Normalize(model.StartedAt),
            BrowserName = model.BrowserName?.Trim(),
            BrowserVersion = model.BrowserVersion?.Trim(),
            OperatingSystem = model.OperatingSystem?.Trim(),
            DeviceType = model.DeviceType?.Trim(),
            UserAgent = model.UserAgent?.Trim(),
            IpAddress = model.IpAddress?.Trim()
        };
    }

    public static ProctorSessionResponseModel ToResponseModel(this ProctorSessionDto dto)
    {
        return new ProctorSessionResponseModel
        {
            SessionId = dto.SessionId,
            AttemptId = dto.AttemptId,
            AssessmentId = dto.AssessmentId,
            StudentId = dto.StudentId,
            Status = dto.Status,
            StartedAt = UtcDateTime.Normalize(dto.StartedAt),
            EndedAt = UtcDateTime.Normalize(dto.EndedAt)
        };
    }

    public static SessionHeartbeatDto ToDto(this SessionHeartbeatRequestModel model)
    {
        return new SessionHeartbeatDto
        {
            AttemptId = model.AttemptId,
            ClientTimestamp = UtcDateTime.Normalize(model.ClientTimestamp),
            VisibilityState = model.VisibilityState?.Trim() ?? string.Empty,
            IsFullscreen = model.IsFullscreen,
            NetworkStatus = model.NetworkStatus?.Trim() ?? string.Empty,
            QuestionId = model.QuestionId
        };
    }

    public static SessionHeartbeatResponseModel ToResponseModel(this SessionHeartbeatResponseDto dto)
    {
        return new SessionHeartbeatResponseModel
        {
            SessionId = dto.SessionId,
            LastHeartbeatAt = UtcDateTime.Normalize(dto.LastHeartbeatAt),
            SessionState = dto.SessionState.ToResponseModel()
        };
    }

    public static EndProctorSessionDto ToDto(this EndProctorSessionRequestModel model)
    {
        return new EndProctorSessionDto
        {
            AttemptId = model.AttemptId,
            EventType = model.EventType?.Trim() ?? string.Empty,
            ClientTimestamp = UtcDateTime.Normalize(model.ClientTimestamp),
            Severity = model.Severity?.Trim() ?? string.Empty,
            MetadataJson = model.Metadata?.ValueKind is null or System.Text.Json.JsonValueKind.Undefined
                ? null
                : model.Metadata?.GetRawText()
        };
    }

    public static ProctorEventBatchDto ToDto(this ProctorEventBatchRequestModel model)
    {
        return new ProctorEventBatchDto
        {
            Events = model.Events.Select(item => new ProctorEventBatchItemDto
            {
                AttemptId = item.AttemptId,
                EventType = item.EventType?.Trim() ?? string.Empty,
                Severity = item.Severity?.Trim() ?? string.Empty,
                ClientTimestamp = UtcDateTime.Normalize(item.ClientTimestamp),
                QuestionId = item.QuestionId,
                MetadataJson = item.Metadata?.ValueKind is null or System.Text.Json.JsonValueKind.Undefined
                    ? null
                    : item.Metadata?.GetRawText()
            }).ToList()
        };
    }

    public static ProctorEventBatchResponseModel ToResponseModel(this ProctorEventBatchResultDto dto)
    {
        return new ProctorEventBatchResponseModel
        {
            ProcessedCount = dto.ProcessedCount,
            Failures = dto.Failures.Select(item => new ProctorEventBatchFailureResponseModel
            {
                Index = item.Index,
                Message = item.Message
            }).ToList(),
            SessionState = dto.SessionState.ToResponseModel()
        };
    }

    public static ProctorSessionStateResponseModel ToResponseModel(this ProctorSessionStateDto dto)
    {
        return new ProctorSessionStateResponseModel
        {
            SessionId = dto.SessionId,
            AttemptId = dto.AttemptId,
            AssessmentId = dto.AssessmentId,
            StudentId = dto.StudentId,
            Status = dto.Status,
            StartedAt = UtcDateTime.Normalize(dto.StartedAt),
            EndedAt = UtcDateTime.Normalize(dto.EndedAt),
            BrowserName = dto.BrowserName,
            BrowserVersion = dto.BrowserVersion,
            OperatingSystem = dto.OperatingSystem,
            DeviceType = dto.DeviceType,
            UserAgent = dto.UserAgent,
            IpAddress = dto.IpAddress,
            Summary = new ProctorSessionSummaryResponseModel
            {
                TabSwitchCount = dto.Summary.TabSwitchCount,
                FullScreenExitCount = dto.Summary.FullScreenExitCount,
                CopyAttemptCount = dto.Summary.CopyAttemptCount,
                PasteAttemptCount = dto.Summary.PasteAttemptCount,
                CutAttemptCount = dto.Summary.CutAttemptCount,
                ContextMenuAttemptCount = dto.Summary.ContextMenuAttemptCount,
                BlockedShortcutCount = dto.Summary.BlockedShortcutCount,
                PossibleDevtoolsCount = dto.Summary.PossibleDevtoolsCount,
                NetworkDisconnectCount = dto.Summary.NetworkDisconnectCount,
                RiskScore = dto.Summary.RiskScore,
                RiskLevel = dto.Summary.RiskLevel,
                LastEventAt = UtcDateTime.Normalize(dto.Summary.LastEventAt)
            },
            Rules = dto.Rules.Select(item => new ProctorSessionRuleResponseModel
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
            Enforcement = new ProctorSessionEnforcementResponseModel
            {
                Action = dto.Enforcement.Action,
                TriggeredByEventType = dto.Enforcement.TriggeredByEventType,
                Message = dto.Enforcement.Message
            }
        };
    }
}
