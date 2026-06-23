using Taskverse.API.Assessments.Service.Models;
using Taskverse.Data.DataAccess;

namespace Taskverse.API.Assessments.Service.Services;

public interface IStudentAttemptAnswerSaveStrategy
{
    bool CanHandle(string questionType);
    Task<AttemptAnswer> SaveAsync(
        TaskverseContext context,
        Attempt attempt,
        Assessment assessment,
        Question question,
        SaveStudentAttemptAnswerRequest request,
        DateTime answeredAtUtc,
        CancellationToken cancellationToken = default);
}
