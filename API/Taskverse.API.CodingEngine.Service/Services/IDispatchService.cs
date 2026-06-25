using Taskverse.Data.DataAccess;

namespace Taskverse.API.CodingEngine.Service.Services;

public interface IDispatchService
{
    Task DispatchAsync(CodeExecutionRequest request, string workerId, CancellationToken cancellationToken);
}
