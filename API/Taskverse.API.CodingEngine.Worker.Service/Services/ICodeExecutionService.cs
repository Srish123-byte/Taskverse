using Taskverse.Data.DataAccess;

namespace Taskverse.API.CodingEngine.Worker.Service.Services;

public interface ICodeExecutionService
{
    Task ExecuteCodeAsync(CodeExecutionRequest request, List<TestCase> testCases, CancellationToken cancellationToken);
}
