using Taskverse.Data.DataAccess;

namespace Taskverse.API.Assessments.Service.Managers;

public interface IAssessmentManager
{
    Task<Assessment> CreateAssessment(Assessment assessment, List<Guid> questionIds);
    Task<Assessment> PublishAssessment(Guid assessmentId);
}
