using Taskverse.Business.Interface;
using Taskverse.Data.DataAccess;

namespace Taskverse.Business.Managers;

/// <summary>
/// Assessment DB tables are temporarily removed from the context.
/// Methods throw NotImplementedException until the tables are re-added.
/// </summary>
public class AssessmentManager : IAssessmentManager
{
    public Task<Assessment?> GetById(string assessmentId)
        => throw new NotImplementedException("Assessment tables not yet configured in current DB schema.");

    public Task<List<Assessment>> GetByUserId(string userId)
        => throw new NotImplementedException("Assessment tables not yet configured in current DB schema.");

    public Task<Assessment> Create(Assessment assessment)
        => throw new NotImplementedException("Assessment tables not yet configured in current DB schema.");

    public Task<AssessmentResult?> GetResult(string assessmentId, string userId)
        => throw new NotImplementedException("Assessment tables not yet configured in current DB schema.");

    public Task UpsertResult(AssessmentResult result)
        => throw new NotImplementedException("Assessment tables not yet configured in current DB schema.");
}
