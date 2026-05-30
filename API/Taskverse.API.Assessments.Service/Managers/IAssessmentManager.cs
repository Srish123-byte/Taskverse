using Taskverse.API.Assessments.Service.Models;
using Taskverse.Data.DataAccess;

namespace Taskverse.API.Assessments.Service.Managers;

public interface IAssessmentManager
{
    Task<Assessment> CreateAssessment(Assessment assessment, List<Guid> questionIds);
    Task<Assessment> ScheduleAssessment(Assessment assessment, List<Guid> questionIds);
    Task DeleteAssessment(Guid assessmentId, DeleteAssessmentRequest request);
    Task<Assessment> PublishAssessment(Guid assessmentId);
    Task<AssessmentSubjectTopicCatalogRecord> GetSubjectTopicCatalog(AssessmentAccessibleBatchesRequest request);
    Task<AssessmentAssignmentCatalogRecord> GetTrainerAssignedClassesAndBatches(AssessmentAccessibleBatchesRequest request);
    Task<PagedAssessmentQuestionListRecord> GetAssessmentQuestionList(Guid assessmentId, int pageNumber, int pageSize);
    Task<List<StudentAssessmentListItemRecord>> GetStudentAssessments(Guid studentUserId, IReadOnlyCollection<string> assessmentStatuses);
    Task<StudentAssessmentDetailRecord> GetStudentAssessmentDetail(Guid assessmentId, Guid studentUserId);
    Task<StudentAssessmentStartRecord> StartStudentAssessment(Guid assessmentId, Guid studentUserId);
    Task<StudentAttemptRecoveryRecord> GetStudentAttemptRecovery(Guid attemptId, Guid studentUserId);
    Task<StudentAttemptAnswerRecord> SaveStudentAttemptAnswer(Guid attemptId, Guid questionId, Guid studentUserId, SaveStudentAttemptAnswerRequest request);
    Task<StudentAttemptSubmitRecord> SubmitStudentAttempt(Guid attemptId, Guid studentUserId);
}
