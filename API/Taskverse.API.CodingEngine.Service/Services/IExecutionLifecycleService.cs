using Taskverse.Data.DataAccess;
using Taskverse.Data.Enums;

namespace Taskverse.API.CodingEngine.Service.Services;

public interface IExecutionLifecycleService
{
    Task<bool> RegisterAndCheckCapacityAsync(string executionMode, CancellationToken cancellationToken);

    Task MarkTerminalAsync(CodeExecutionRequest request, CodeExecutionStatus terminalStatus, CancellationToken cancellationToken);
}
