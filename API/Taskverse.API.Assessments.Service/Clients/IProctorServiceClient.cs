using Taskverse.API.Assessments.Service.Models;

namespace Taskverse.API.Assessments.Service.Clients;

public interface IProctorServiceClient
{
    Task<ProctorSessionStateRecord?> GetSessionByAttemptAsync(Guid attemptId, CancellationToken cancellationToken = default);
}
