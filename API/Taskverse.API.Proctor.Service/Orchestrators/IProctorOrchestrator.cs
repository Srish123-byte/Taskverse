using Taskverse.API.Proctor.Service.Models;

namespace Taskverse.API.Proctor.Service.Orchestrators;

public interface IProctorOrchestrator
{
    Task<ProctorSessionRecord> StartSession(Guid attemptId, Guid studentUserId, StartProctorSessionRequest request);
    Task<SessionHeartbeatResponseRecord> HeartbeatSession(Guid sessionId, Guid studentUserId, SessionHeartbeatRequest request);
    Task<ProctorEventBatchResultRecord> RecordEvents(Guid sessionId, Guid studentUserId, ProctorEventBatchRequest request);
    Task<ProctorSessionRecord> EndSession(Guid sessionId, Guid studentUserId, EndProctorSessionRequest request);
    Task<ProctorSessionStateRecord> GetSessionStateByAttempt(Guid attemptId, Guid studentUserId);
    Task<ProctorSessionStateRecord> GetSessionState(Guid sessionId, Guid studentUserId);
    Task<ProctorSessionStateRecord> GetSessionStateByAttempt(Guid attemptId);
}
