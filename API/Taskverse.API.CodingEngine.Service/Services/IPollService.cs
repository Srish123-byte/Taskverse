using Taskverse.Data.DataAccess;

namespace Taskverse.API.CodingEngine.Service.Services;

public interface IPollService
{
    Task CollectResultAsync(CodeExecutionRequest request, CancellationToken cancellationToken);
}
