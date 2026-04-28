using Taskverse.Data.DataAccess;

namespace Taskverse.Business.Interface;

public interface IAssessmentManager
{
    Task<Assessment?> GetById(string assessmentId);
    Task<List<Assessment>> GetByUserId(string userId);
    Task<Assessment> Create(Assessment assessment);
    Task<AssessmentResult?> GetResult(string assessmentId, string userId);
    Task UpsertResult(AssessmentResult result);
}
