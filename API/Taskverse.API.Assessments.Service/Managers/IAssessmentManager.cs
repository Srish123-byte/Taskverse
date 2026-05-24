using Taskverse.API.Assessments.Service.Models;
using Taskverse.Data.DataAccess;

namespace Taskverse.API.Assessments.Service.Managers;

public interface IAssessmentManager
{
    Task<Assessment> CreateAssessment(Assessment assessment, List<Guid> questionIds);
    Task DeleteAssessment(Guid assessmentId, DeleteAssessmentRequest request);
    Task<Assessment> PublishAssessment(Guid assessmentId);
    Task<PagedAssessmentQuestionListRecord> GetAssessmentQuestionList(Guid assessmentId, int pageNumber, int pageSize);
}
