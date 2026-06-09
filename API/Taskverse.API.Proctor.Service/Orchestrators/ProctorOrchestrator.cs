using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Taskverse.API.Proctor.Service.Managers;
using Taskverse.API.Proctor.Service.Models;
using Taskverse.Data.DataAccess;
using Taskverse.Data.Enums;

namespace Taskverse.API.Proctor.Service.Orchestrators;

public class ProctorOrchestrator : IProctorOrchestrator
{
    private readonly IProctorManager _proctorManager;
    private readonly ProctoringSettings _proctoringSettings;
    private readonly ILogger<ProctorOrchestrator> _logger;

    public ProctorOrchestrator(
        IProctorManager proctorManager,
        IOptions<ProctoringSettings> proctoringSettings,
        ILogger<ProctorOrchestrator> logger)
    {
        _proctorManager = proctorManager;
        _proctoringSettings = proctoringSettings.Value;
        _logger = logger;
    }

    public async Task<ProctorSessionRecord> StartSession(Guid attemptId, Guid studentUserId, StartProctorSessionRequest request)
    {
        ValidateStartSessionRequest(attemptId, studentUserId, request);

        return await ExecuteDbOperationAsync(async () =>
        {
            var student = await GetStudentByUserIdAsync(studentUserId);
            EnsureStudentContextMatchesRequest(student, studentUserId, request);

            var attempt = await GetAttemptForStudentAsync(attemptId, student.StudentId);
            EnsureAttemptCanStartProctoring(attempt);

            if (request.AssessmentId != Guid.Empty && request.AssessmentId != attempt.AssessmentId)
            {
                throw new InvalidOperationException(
                    $"Assessment '{request.AssessmentId}' does not match attempt '{attemptId}'.");
            }

            var existingSession = await _proctorManager.GetActiveSessionForAttemptAsync(attemptId, student.StudentId);
            if (existingSession is not null)
            {
                return existingSession.ToRecord();
            }

            var startedAt = request.StartedAt?.ToUniversalTime() ?? DateTime.UtcNow;
            var now = DateTime.UtcNow;
            var session = new ProctoringSession
            {
                ProctoringSessionId = Guid.NewGuid(),
                AttemptId = attempt.AttemptId,
                AssessmentId = attempt.AssessmentId,
                StudentId = student.StudentId,
                ProctoringStatus = (int)ProctoringStatus.Active,
                StartedAt = startedAt,
                BrowserName = Normalize(request.BrowserName),
                BrowserVersion = Normalize(request.BrowserVersion),
                OperatingSystem = Normalize(request.OperatingSystem),
                DeviceType = Normalize(request.DeviceType),
                UserAgent = Normalize(request.UserAgent),
                IpAddress = Normalize(request.IpAddress),
                CreatedAt = now,
                ModifiedAt = now
            };

            var startEvent = new ProctoringEvent
            {
                ProctoringEventId = Guid.NewGuid(),
                ProctoringSessionId = session.ProctoringSessionId,
                AttemptId = attempt.AttemptId,
                AssessmentId = attempt.AssessmentId,
                StudentId = student.StudentId,
                EventType = EventType.ASSESSMENT_STARTED,
                Severity = "Info",
                ClientTimestamp = startedAt,
                ServerReceivedAt = now,
                MetadataJson = BuildAssessmentStartedMetadata(request, startedAt),
                CreatedAt = now
            };

            _proctorManager.AddProctoringSession(session);
            _proctorManager.AddProctoringEvent(startEvent);
            await SaveChangesWithWrapAsync("Unable to start the proctoring session.");

            return session.ToRecord();
        }, "starting the proctoring session");
    }

    public async Task<SessionHeartbeatResponseRecord> HeartbeatSession(
        Guid sessionId,
        Guid studentUserId,
        SessionHeartbeatRequest request)
    {
        ValidateHeartbeatSessionRequest(sessionId, studentUserId, request);

        return await ExecuteDbOperationAsync(async () =>
        {
            var student = await GetStudentByUserIdAsync(studentUserId);
            var session = await GetSessionForStudentAsync(sessionId, student.StudentId);
            EnsureSessionIsActive(session);

            if (request.AttemptId != session.AttemptId)
            {
                throw new ArgumentException("Attempt id in the request body must match the session attempt.");
            }

            var attempt = await GetAttemptForStudentAsync(session.AttemptId, student.StudentId);
            var summary = await GetOrCreateViolationSummaryAsync(session, attempt);
            var heartbeatAt = DateTime.UtcNow;
            var clientTimestamp = request.ClientTimestamp?.ToUniversalTime();
            var visibilityState = ParseVisibilityState(request.VisibilityState);
            var networkStatus = ParseNetworkStatus(request.NetworkStatus);
            var questionId = await ValidateHeartbeatQuestionIdAsync(request.QuestionId);

            session.LastHeartbeatAt = heartbeatAt;
            session.LastKnownVisibilityState = visibilityState;
            session.LastKnownIsFullscreen = request.IsFullscreen;
            session.LastKnownNetworkStatus = networkStatus;
            session.LastKnownQuestionId = questionId;
            session.ModifiedAt = heartbeatAt;

            var heartbeatEvents = BuildHeartbeatEvents(
                session,
                attempt,
                student.StudentId,
                clientTimestamp,
                heartbeatAt,
                visibilityState,
                request.IsFullscreen,
                networkStatus,
                questionId);

            if (heartbeatEvents.Count > 0)
            {
                foreach (var heartbeatEvent in heartbeatEvents)
                {
                    ApplyViolationSummaryForEvent(summary, heartbeatEvent);
                }

                var latestEventAt = heartbeatEvents
                    .Select(item => item.ClientTimestamp ?? item.ServerReceivedAt)
                    .DefaultIfEmpty(summary.LastEventAt ?? heartbeatAt)
                    .Max();

                summary.LastEventAt = latestEventAt;
                summary.ModifiedAt = heartbeatAt;
                summary.RiskScore = CalculateRiskScore(summary);
                summary.RiskLevel = ResolveRiskLevel(summary.RiskScore);

                _proctorManager.AddProctoringEvents(heartbeatEvents);
            }
            else if (_proctorManager.IsViolationSummaryNew(summary))
            {
                summary.ModifiedAt = heartbeatAt;
            }

            await SaveChangesWithWrapAsync("Unable to register the proctoring session heartbeat.");

            return new SessionHeartbeatResponseRecord(session.ProctoringSessionId, heartbeatAt);
        }, "registering the proctoring session heartbeat");
    }

    public async Task<ProctorEventBatchResultRecord> RecordEvents(
        Guid sessionId,
        Guid studentUserId,
        ProctorEventBatchRequest request)
    {
        ValidateRecordEventsRequest(sessionId, studentUserId, request);

        return await ExecuteDbOperationAsync(async () =>
        {
            var student = await GetStudentByUserIdAsync(studentUserId);
            var session = await GetSessionForStudentAsync(sessionId, student.StudentId);
            EnsureSessionIsActive(session);

            var attempt = await GetAttemptForStudentAsync(session.AttemptId, student.StudentId);
            var summary = await GetOrCreateViolationSummaryAsync(session, attempt);

            var requestedQuestionIds = request.Events
                .Where(item => item.QuestionId.HasValue && item.QuestionId.Value != Guid.Empty)
                .Select(item => item.QuestionId!.Value)
                .Distinct()
                .ToList();

            var validQuestionIds = await _proctorManager.GetValidQuestionIdsAsync(requestedQuestionIds);

            var failures = new List<ProctorEventBatchFailureRecord>();
            var successfulEvents = new List<ProctoringEvent>();
            var latestEventAt = summary.LastEventAt;

            for (var index = 0; index < request.Events.Count; index++)
            {
                var item = request.Events[index];

                if (!TryBuildEventEntity(index, item, session, attempt, student.StudentId, validQuestionIds, out var eventEntity, out var failure))
                {
                    failures.Add(failure!);
                    continue;
                }

                var successfulEvent = eventEntity!;
                successfulEvents.Add(successfulEvent);
                ApplyViolationSummaryForEvent(summary, successfulEvent);

                var effectiveEventTime = successfulEvent.ClientTimestamp ?? successfulEvent.ServerReceivedAt;
                if (!latestEventAt.HasValue || effectiveEventTime > latestEventAt.Value)
                {
                    latestEventAt = effectiveEventTime;
                }
            }

            if (successfulEvents.Count > 0)
            {
                summary.LastEventAt = latestEventAt;
                summary.ModifiedAt = DateTime.UtcNow;
                summary.RiskScore = CalculateRiskScore(summary);
                summary.RiskLevel = ResolveRiskLevel(summary.RiskScore);

                _proctorManager.AddProctoringEvents(successfulEvents);
                await SaveChangesWithWrapAsync("Unable to record the proctoring events.");
            }
            else if (_proctorManager.IsViolationSummaryNew(summary))
            {
                await SaveChangesWithWrapAsync("Unable to initialize the proctoring violation summary.");
            }

            return new ProctorEventBatchResultRecord(successfulEvents.Count, failures);
        }, "recording proctoring events");
    }

    public async Task<ProctorSessionRecord> EndSession(
        Guid sessionId,
        Guid studentUserId,
        EndProctorSessionRequest request)
    {
        ValidateEndSessionRequest(sessionId, studentUserId, request);

        return await ExecuteDbOperationAsync(async () =>
        {
            var student = await GetStudentByUserIdAsync(studentUserId);
            var session = await GetSessionForStudentAsync(sessionId, student.StudentId);

            if (request.AttemptId != Guid.Empty && request.AttemptId != session.AttemptId)
            {
                throw new ArgumentException("Attempt id in the request body must match the session attempt.");
            }

            EnsureEndEventTypeIsSupported(request.EventType);

            if (session.ProctoringStatus == (int)ProctoringStatus.Submitted || session.EndedAt.HasValue)
            {
                return session.ToRecord();
            }

            var attempt = await GetAttemptForStudentAsync(session.AttemptId, student.StudentId);
            var endedAt = DateTime.UtcNow;

            session.ProctoringStatus = (int)ProctoringStatus.Submitted;
            session.EndedAt = endedAt;
            session.ModifiedAt = endedAt;

            var submitEvent = new ProctoringEvent
            {
                ProctoringEventId = Guid.NewGuid(),
                ProctoringSessionId = session.ProctoringSessionId,
                AttemptId = attempt.AttemptId,
                AssessmentId = session.AssessmentId,
                StudentId = student.StudentId,
                EventType = ParseEndEventType(request.EventType),
                Severity = string.IsNullOrWhiteSpace(request.Severity) ? "Info" : request.Severity.Trim(),
                ClientTimestamp = request.ClientTimestamp?.ToUniversalTime(),
                ServerReceivedAt = endedAt,
                MetadataJson = NormalizeJson(request.MetadataJson),
                CreatedAt = endedAt
            };

            _proctorManager.AddProctoringEvent(submitEvent);
            await SaveChangesWithWrapAsync("Unable to end the proctoring session.");

            return session.ToRecord();
        }, "ending the proctoring session");
    }

    public async Task<ProctorSessionStateRecord> GetSessionState(Guid sessionId, Guid studentUserId)
    {
        ValidateGetSessionStateRequest(sessionId, studentUserId);

        return await ExecuteDbOperationAsync(async () =>
        {
            var student = await GetStudentByUserIdAsync(studentUserId);
            var session = await GetSessionForStudentOwnedAccessAsync(sessionId, student.StudentId);
            var summary = await _proctorManager.GetViolationSummaryAsync(session.ProctoringSessionId)
                ?? throw new KeyNotFoundException(
                    $"Proctoring violation summary for session '{sessionId}' was not found.");

            return session.ToStateRecord(summary);
        }, "retrieving the proctoring session state");
    }

    public async Task<ProctorSessionStateRecord> GetSessionStateByAttempt(Guid attemptId)
    {
        ValidateGetAttemptSessionStateRequest(attemptId);

        return await ExecuteDbOperationAsync(async () =>
        {
            var sessions = await _proctorManager.GetSessionsByAttemptAsync(attemptId);
            if (sessions.Count == 0)
            {
                throw new KeyNotFoundException($"Proctoring session for attempt '{attemptId}' was not found.");
            }

            if (sessions.Count > 1)
            {
                throw new InvalidOperationException(
                    $"Multiple proctoring sessions were found for attempt '{attemptId}'. Exactly one session is expected.");
            }

            var session = sessions[0];
            var summary = await _proctorManager.GetViolationSummaryAsync(session.ProctoringSessionId)
                ?? throw new KeyNotFoundException(
                    $"Proctoring violation summary for attempt '{attemptId}' was not found.");

            return session.ToStateRecord(summary);
        }, "retrieving the proctoring session state by attempt");
    }

    private static void ValidateStartSessionRequest(Guid attemptId, Guid studentUserId, StartProctorSessionRequest request)
    {
        if (attemptId == Guid.Empty)
        {
            throw new ArgumentException("Attempt id is required.");
        }

        if (studentUserId == Guid.Empty)
        {
            throw new ArgumentException("Student user id is required.");
        }

        if (request is null)
        {
            throw new ArgumentException("Start proctoring request is required.");
        }

        if (request.AttemptId != Guid.Empty && request.AttemptId != attemptId)
        {
            throw new ArgumentException("Attempt id in the request body must match the route attempt id.");
        }
    }

    private static void ValidateRecordEventsRequest(Guid sessionId, Guid studentUserId, ProctorEventBatchRequest request)
    {
        if (sessionId == Guid.Empty)
        {
            throw new ArgumentException("Session id is required.");
        }

        if (studentUserId == Guid.Empty)
        {
            throw new ArgumentException("Student user id is required.");
        }

        if (request is null)
        {
            throw new ArgumentException("Proctor event batch request is required.");
        }

        if (request.Events is null || request.Events.Count == 0)
        {
            throw new ArgumentException("At least one proctoring event is required.");
        }
    }

    private static void ValidateHeartbeatSessionRequest(Guid sessionId, Guid studentUserId, SessionHeartbeatRequest request)
    {
        if (sessionId == Guid.Empty)
        {
            throw new ArgumentException("Session id is required.");
        }

        if (studentUserId == Guid.Empty)
        {
            throw new ArgumentException("Student user id is required.");
        }

        if (request is null)
        {
            throw new ArgumentException("Session heartbeat request is required.");
        }

        if (request.AttemptId == Guid.Empty)
        {
            throw new ArgumentException("Attempt id is required.");
        }

        if (string.IsNullOrWhiteSpace(request.VisibilityState))
        {
            throw new ArgumentException("Visibility state is required.");
        }

        if (string.IsNullOrWhiteSpace(request.NetworkStatus))
        {
            throw new ArgumentException("Network status is required.");
        }
    }

    private static void ValidateEndSessionRequest(Guid sessionId, Guid studentUserId, EndProctorSessionRequest request)
    {
        if (sessionId == Guid.Empty)
        {
            throw new ArgumentException("Session id is required.");
        }

        if (studentUserId == Guid.Empty)
        {
            throw new ArgumentException("Student user id is required.");
        }

        if (request is null)
        {
            throw new ArgumentException("End proctoring request is required.");
        }

        if (string.IsNullOrWhiteSpace(request.EventType))
        {
            throw new ArgumentException("Event type is required.");
        }
    }

    private static void ValidateGetSessionStateRequest(Guid sessionId, Guid studentUserId)
    {
        if (sessionId == Guid.Empty)
        {
            throw new ArgumentException("Session id is required.");
        }

        if (studentUserId == Guid.Empty)
        {
            throw new ArgumentException("Student user id is required.");
        }
    }

    private static void ValidateGetAttemptSessionStateRequest(Guid attemptId)
    {
        if (attemptId == Guid.Empty)
        {
            throw new ArgumentException("Attempt id is required.");
        }
    }

    private async Task<Student> GetStudentByUserIdAsync(Guid studentUserId)
    {
        var student = await _proctorManager.GetStudentByUserIdAsync(studentUserId);
        return student ?? throw new KeyNotFoundException($"Student profile was not found for user '{studentUserId}'.");
    }

    private async Task<Attempt> GetAttemptForStudentAsync(Guid attemptId, Guid studentId)
    {
        var attempt = await _proctorManager.GetAttemptForStudentAsync(attemptId, studentId);
        return attempt ?? throw new KeyNotFoundException($"Attempt '{attemptId}' was not found for the current student.");
    }

    private async Task<ProctoringSession> GetSessionForStudentAsync(Guid sessionId, Guid studentId)
    {
        var session = await _proctorManager.GetSessionForStudentAsync(sessionId, studentId);
        return session ?? throw new KeyNotFoundException($"Proctoring session '{sessionId}' was not found for the current student.");
    }

    private async Task<ProctoringSession> GetSessionForStudentOwnedAccessAsync(Guid sessionId, Guid studentId)
    {
        var session = await _proctorManager.GetSessionByIdAsync(sessionId)
            ?? throw new KeyNotFoundException($"Proctoring session '{sessionId}' was not found.");

        if (session.StudentId != studentId)
        {
            throw new UnauthorizedAccessException("Student can access only their own proctoring sessions.");
        }

        return session;
    }

    private async Task<ProctoringViolationSummary> GetOrCreateViolationSummaryAsync(ProctoringSession session, Attempt attempt)
    {
        var summary = await _proctorManager.GetViolationSummaryAsync(session.ProctoringSessionId);
        if (summary is not null)
        {
            return summary;
        }

        summary = new ProctoringViolationSummary
        {
            ProctoringViolationSummaryId = Guid.NewGuid(),
            AttemptId = attempt.AttemptId,
            ProctoringSessionId = session.ProctoringSessionId,
            RiskScore = 0,
            RiskLevel = RiskLevel.Low,
            CreatedAt = DateTime.UtcNow
        };

        _proctorManager.AddViolationSummary(summary);
        return summary;
    }

    private static void EnsureStudentContextMatchesRequest(
        Student student,
        Guid studentUserId,
        StartProctorSessionRequest request)
    {
        if (!request.StudentId.HasValue || request.StudentId.Value == Guid.Empty)
        {
            return;
        }

        if (request.StudentId.Value == studentUserId || request.StudentId.Value == student.StudentId)
        {
            return;
        }

        throw new UnauthorizedAccessException("Student id in the request body does not match the current student context.");
    }

    private static void EnsureAttemptCanStartProctoring(Attempt attempt)
    {
        if (attempt.AttemptStatus is not AttemptStatus.In_Progress)
        {
            throw new InvalidOperationException("Proctoring can only be started for an in-progress attempt.");
        }
    }

    private static void EnsureSessionIsActive(ProctoringSession session)
    {
        if (session.ProctoringStatus != (int)ProctoringStatus.Active || session.EndedAt.HasValue)
        {
            throw new InvalidOperationException("Proctoring events can only be recorded for an active session.");
        }
    }

    private static void EnsureEndEventTypeIsSupported(string eventType)
    {
        if (!string.Equals(eventType, nameof(EventType.ASSESSMENT_SUBMITTED), StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(eventType, nameof(EventType.ASSESSMENT_AUTO_SUBMITTED), StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Only ASSESSMENT_SUBMITTED and ASSESSMENT_AUTO_SUBMITTED are allowed for session end.");
        }
    }

    private static EventType ParseEndEventType(string eventType)
    {
        EnsureEndEventTypeIsSupported(eventType);
        return Enum.Parse<EventType>(eventType, true);
    }

    private static ProctoringVisibilityStatus ParseVisibilityState(string visibilityState)
    {
        if (!Enum.TryParse<ProctoringVisibilityStatus>(visibilityState.Trim(), true, out var parsedValue))
        {
            throw new ArgumentException(
                $"VisibilityState '{visibilityState}' is invalid. Allowed values are Visible, Hidden, Unknown.");
        }

        return parsedValue;
    }

    private static ProctoringNetworkStatus ParseNetworkStatus(string networkStatus)
    {
        if (!Enum.TryParse<ProctoringNetworkStatus>(networkStatus.Trim(), true, out var parsedValue))
        {
            throw new ArgumentException(
                $"NetworkStatus '{networkStatus}' is invalid. Allowed values are Online, Offline, Unstable, Unknown.");
        }

        return parsedValue;
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? BuildAssessmentStartedMetadata(StartProctorSessionRequest request, DateTime startedAt)
    {
        var metadata = new Dictionary<string, object?>
        {
            ["startedAt"] = startedAt,
            ["browserName"] = Normalize(request.BrowserName),
            ["browserVersion"] = Normalize(request.BrowserVersion),
            ["operatingSystem"] = Normalize(request.OperatingSystem),
            ["deviceType"] = Normalize(request.DeviceType),
            ["userAgent"] = Normalize(request.UserAgent),
            ["ipAddress"] = Normalize(request.IpAddress)
        };

        var filteredMetadata = metadata
            .Where(item =>
                item.Value is not null &&
                (item.Value is not string stringValue || !string.IsNullOrWhiteSpace(stringValue)))
            .ToDictionary(item => item.Key, item => item.Value);

        return filteredMetadata.Count == 0
            ? null
            : JsonSerializer.Serialize(filteredMetadata);
    }

    private static bool TryBuildEventEntity(
        int index,
        ProctorEventBatchItemRequest item,
        ProctoringSession session,
        Attempt attempt,
        Guid studentId,
        HashSet<Guid> validQuestionIds,
        out ProctoringEvent? eventEntity,
        out ProctorEventBatchFailureRecord? failure)
    {
        eventEntity = null;
        failure = null;

        if (item.AttemptId == Guid.Empty || item.AttemptId != session.AttemptId)
        {
            failure = new ProctorEventBatchFailureRecord(index, "AttemptId must match the session attempt.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(item.EventType) ||
            !Enum.TryParse<EventType>(item.EventType.Trim(), true, out var eventType))
        {
            failure = new ProctorEventBatchFailureRecord(index, $"EventType '{item.EventType}' is invalid.");
            return false;
        }

        if (item.QuestionId.HasValue && item.QuestionId.Value != Guid.Empty && !validQuestionIds.Contains(item.QuestionId.Value))
        {
            failure = new ProctorEventBatchFailureRecord(index, $"Question '{item.QuestionId}' was not found.");
            return false;
        }

        eventEntity = new ProctoringEvent
        {
            ProctoringEventId = Guid.NewGuid(),
            ProctoringSessionId = session.ProctoringSessionId,
            AttemptId = attempt.AttemptId,
            AssessmentId = session.AssessmentId,
            StudentId = studentId,
            EventType = eventType,
            Severity = string.IsNullOrWhiteSpace(item.Severity) ? "Info" : item.Severity.Trim(),
            ClientTimestamp = item.ClientTimestamp?.ToUniversalTime(),
            ServerReceivedAt = DateTime.UtcNow,
            QuestionId = item.QuestionId is { } questionId && questionId != Guid.Empty ? questionId : null,
            MetadataJson = NormalizeJson(item.MetadataJson),
            CreatedAt = DateTime.UtcNow
        };

        return true;
    }

    private static string? NormalizeJson(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(metadataJson);
            return document.RootElement.GetRawText();
        }
        catch (JsonException)
        {
            return metadataJson.Trim();
        }
    }

    private async Task<Guid?> ValidateHeartbeatQuestionIdAsync(Guid? questionId)
    {
        if (!questionId.HasValue || questionId.Value == Guid.Empty)
        {
            return null;
        }

        var validQuestionIds = await _proctorManager.GetValidQuestionIdsAsync([questionId.Value]);
        if (!validQuestionIds.Contains(questionId.Value))
        {
            throw new KeyNotFoundException($"Question '{questionId}' was not found.");
        }

        return questionId.Value;
    }

    private static List<ProctoringEvent> BuildHeartbeatEvents(
        ProctoringSession session,
        Attempt attempt,
        Guid studentId,
        DateTime? clientTimestamp,
        DateTime serverReceivedAt,
        ProctoringVisibilityStatus visibilityState,
        bool isFullscreen,
        ProctoringNetworkStatus networkStatus,
        Guid? questionId)
    {
        var heartbeatEvents = new List<ProctoringEvent>();

        if (visibilityState == ProctoringVisibilityStatus.Hidden)
        {
            heartbeatEvents.Add(CreateHeartbeatEvent(
                session,
                attempt,
                studentId,
                EventType.TAB_SWITCHED,
                "Warning",
                clientTimestamp,
                serverReceivedAt,
                questionId,
                visibilityState,
                isFullscreen,
                networkStatus));
        }

        if (!isFullscreen)
        {
            heartbeatEvents.Add(CreateHeartbeatEvent(
                session,
                attempt,
                studentId,
                EventType.FULLSCREEN_EXITED,
                "Warning",
                clientTimestamp,
                serverReceivedAt,
                questionId,
                visibilityState,
                isFullscreen,
                networkStatus));
        }

        if (networkStatus is ProctoringNetworkStatus.Offline or ProctoringNetworkStatus.Unstable)
        {
            heartbeatEvents.Add(CreateHeartbeatEvent(
                session,
                attempt,
                studentId,
                EventType.NETWORK_DISCONNECTED,
                networkStatus == ProctoringNetworkStatus.Offline ? "High" : "Warning",
                clientTimestamp,
                serverReceivedAt,
                questionId,
                visibilityState,
                isFullscreen,
                networkStatus));
        }

        return heartbeatEvents;
    }

    private static ProctoringEvent CreateHeartbeatEvent(
        ProctoringSession session,
        Attempt attempt,
        Guid studentId,
        EventType eventType,
        string severity,
        DateTime? clientTimestamp,
        DateTime serverReceivedAt,
        Guid? questionId,
        ProctoringVisibilityStatus visibilityState,
        bool isFullscreen,
        ProctoringNetworkStatus networkStatus)
    {
        return new ProctoringEvent
        {
            ProctoringEventId = Guid.NewGuid(),
            ProctoringSessionId = session.ProctoringSessionId,
            AttemptId = attempt.AttemptId,
            AssessmentId = session.AssessmentId,
            StudentId = studentId,
            EventType = eventType,
            Severity = severity,
            ClientTimestamp = clientTimestamp,
            ServerReceivedAt = serverReceivedAt,
            QuestionId = questionId,
            MetadataJson = BuildHeartbeatMetadata(eventType, visibilityState, isFullscreen, networkStatus, questionId),
            CreatedAt = serverReceivedAt
        };
    }

    private static string BuildHeartbeatMetadata(
        EventType eventType,
        ProctoringVisibilityStatus visibilityState,
        bool isFullscreen,
        ProctoringNetworkStatus networkStatus,
        Guid? questionId)
    {
        var metadata = new Dictionary<string, object?>
        {
            ["source"] = "heartbeat",
            ["eventType"] = eventType.ToString(),
            ["visibilityState"] = visibilityState.ToString(),
            ["isFullscreen"] = isFullscreen,
            ["networkStatus"] = networkStatus.ToString(),
            ["questionId"] = questionId
        };

        return JsonSerializer.Serialize(metadata);
    }

    private static void ApplyViolationSummaryForEvent(ProctoringViolationSummary summary, ProctoringEvent eventEntity)
    {
        switch (eventEntity.EventType)
        {
            case EventType.TAB_SWITCHED:
                summary.TabSwitchCount += 1;
                break;
            case EventType.FULLSCREEN_EXITED:
                summary.FullScreenExitCount += 1;
                break;
            case EventType.COPY_ATTEMPTED:
                summary.CopyAttemptCount += 1;
                break;
            case EventType.PASTE_ATTEMPTED:
                summary.PasteAttemptCount += 1;
                break;
            case EventType.CUT_ATTEMPTED:
                summary.CutAttemptCount += 1;
                break;
            case EventType.CONTEXT_MENU_ATTEMPTED:
                summary.ContextMenuAttemptCount += 1;
                break;
            case EventType.BLOCKED_KEYBOARD_SHORTCUT:
                summary.BlockedShortcutCount += 1;
                break;
            case EventType.POSSIBLE_DEVTOOLS_OPENED:
                summary.PossibleDevtoolsCount += 1;
                break;
            case EventType.NETWORK_DISCONNECTED:
                summary.NetworkDisconnectCount += 1;
                break;
        }
    }

    private int CalculateRiskScore(ProctoringViolationSummary summary)
    {
        var weights = _proctoringSettings.RiskScoring.Weights;

        return
            (summary.TabSwitchCount * weights.TabSwitch) +
            (summary.FullScreenExitCount * weights.FullscreenExit) +
            (summary.CopyAttemptCount * weights.CopyAttempt) +
            (summary.PasteAttemptCount * weights.PasteAttempt) +
            (summary.CutAttemptCount * weights.CutAttempt) +
            (summary.ContextMenuAttemptCount * weights.ContextMenuAttempt) +
            (summary.BlockedShortcutCount * weights.BlockedShortcut) +
            (summary.PossibleDevtoolsCount * weights.PossibleDevTools) +
            (summary.NetworkDisconnectCount * weights.NetworkDisconnect);
    }

    private RiskLevel ResolveRiskLevel(int riskScore)
    {
        var levels = _proctoringSettings.RiskScoring.Levels;

        if (IsWithinRange(riskScore, levels.Critical))
        {
            return RiskLevel.Critical;
        }

        if (IsWithinRange(riskScore, levels.High))
        {
            return RiskLevel.High;
        }

        if (IsWithinRange(riskScore, levels.Medium))
        {
            return RiskLevel.Medium;
        }

        return RiskLevel.Low;
    }

    private static bool IsWithinRange(int score, RiskScoreRangeSettings range)
        => score >= range.MinScore && score <= range.MaxScore;

    private async Task SaveChangesWithWrapAsync(string errorMessage)
    {
        try
        {
            await _proctorManager.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "{ErrorMessage} SaveChanges failed in ProctorOrchestrator.", errorMessage);
            throw new InvalidOperationException(errorMessage, ex);
        }
    }

    private async Task<T> ExecuteDbOperationAsync<T>(Func<Task<T>> operation, string operationName)
    {
        try
        {
            return await operation();
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (UnauthorizedAccessException)
        {
            throw;
        }
        catch (KeyNotFoundException)
        {
            throw;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while {OperationName}.", operationName);
            throw new InvalidOperationException($"An unexpected error occurred while {operationName}.", ex);
        }
    }
}

internal static class ProctorSessionMappings
{
    public static ProctorSessionRecord ToRecord(this ProctoringSession session)
    {
        return new ProctorSessionRecord(
            session.ProctoringSessionId,
            session.AttemptId,
            session.AssessmentId ?? Guid.Empty,
            session.StudentId,
            ((ProctoringStatus)session.ProctoringStatus).ToString(),
            session.StartedAt,
            session.EndedAt);
    }

    public static ProctorSessionStateRecord ToStateRecord(
        this ProctoringSession session,
        ProctoringViolationSummary summary)
    {
        return new ProctorSessionStateRecord(
            session.ProctoringSessionId,
            session.AttemptId,
            session.AssessmentId ?? Guid.Empty,
            session.StudentId,
            ((ProctoringStatus)session.ProctoringStatus).ToString(),
            session.StartedAt,
            session.EndedAt,
            session.BrowserName,
            session.BrowserVersion,
            session.OperatingSystem,
            session.DeviceType,
            session.UserAgent,
            session.IpAddress,
            new ProctorSessionSummaryRecord(
                summary.TabSwitchCount,
                summary.FullScreenExitCount,
                summary.CopyAttemptCount,
                summary.PasteAttemptCount,
                summary.CutAttemptCount,
                summary.ContextMenuAttemptCount,
                summary.BlockedShortcutCount,
                summary.PossibleDevtoolsCount,
                summary.NetworkDisconnectCount,
                summary.RiskScore,
                summary.RiskLevel.ToString(),
                summary.LastEventAt));
    }
}
