using Taskverse.API.Assessments.Service.Models;
using Taskverse.Data.DataAccess;

namespace Taskverse.API.Assessments.Service.Managers;

/// <summary>
/// Defines assessment-domain operations handled by the assessments microservice.
/// </summary>
public interface IAssessmentManager
{
    /// <summary>
    /// Creates a draft assessment and attaches the selected questions.
    /// </summary>
    /// <param name="assessment">The assessment aggregate to create.</param>
    /// <param name="questionIds">The ordered question identifiers to assign.</param>
    /// <returns>The persisted assessment.</returns>
    Task<Assessment> CreateAssessment(Assessment assessment, List<Guid> questionIds);

    /// <summary>
    /// Retrieves a single assessment after applying college and role-based access checks.
    /// </summary>
    /// <param name="assessmentId">The assessment identifier.</param>
    /// <param name="collegeId">The college scope for the request.</param>
    /// <param name="requesterRole">The role of the caller.</param>
    /// <param name="requesterName">The display name of the caller.</param>
    /// <returns>The requested assessment.</returns>
    Task<Assessment> GetAssessment(Guid assessmentId, Guid collegeId, string requesterRole, string requesterName);

    /// <summary>
    /// Updates an existing assessment and refreshes its question assignments.
    /// </summary>
    /// <param name="assessmentId">The assessment identifier.</param>
    /// <param name="request">The requested assessment updates.</param>
    /// <returns>The updated assessment.</returns>
    Task<Assessment> UpdateAssessment(Guid assessmentId, UpdateAssessmentRequest request);

    /// <summary>
    /// Creates a scheduled assessment and attaches the selected questions.
    /// </summary>
    /// <param name="assessment">The assessment aggregate to schedule.</param>
    /// <param name="questionIds">The ordered question identifiers to assign.</param>
    /// <returns>The persisted scheduled assessment.</returns>
    Task<Assessment> ScheduleAssessment(Assessment assessment, List<Guid> questionIds);

    /// <summary>
    /// Soft deletes an assessment and records audit metadata for the delete request.
    /// </summary>
    /// <param name="assessmentId">The assessment identifier.</param>
    /// <param name="request">The delete request context.</param>
    Task DeleteAssessment(Guid assessmentId, DeleteAssessmentRequest request);

    /// <summary>
    /// Publishes a draft assessment that already exists in storage.
    /// </summary>
    /// <param name="assessmentId">The assessment identifier.</param>
    /// <returns>The published assessment.</returns>
    Task<Assessment> PublishAssessment(Guid assessmentId);

    /// <summary>
    /// Builds the subject and topic catalog available to the current requester.
    /// </summary>
    /// <param name="request">The requester context used to scope accessible batches.</param>
    /// <returns>The accessible subject-topic catalog.</returns>
    Task<AssessmentSubjectTopicCatalogRecord> GetSubjectTopicCatalog(AssessmentAccessibleBatchesRequest request);

    /// <summary>
    /// Builds the class and batch catalog assigned to a trainer.
    /// </summary>
    /// <param name="request">The requester context used to scope trainer assignments.</param>
    /// <returns>The assigned classes and batches.</returns>
    Task<AssessmentAssignmentCatalogRecord> GetTrainerAssignedClassesAndBatches(AssessmentAccessibleBatchesRequest request);

    /// <summary>
    /// Searches assessments for management screens using the supplied filters and paging settings.
    /// </summary>
    /// <param name="request">The assessment search request.</param>
    /// <returns>The paged assessment search result.</returns>
    Task<AssessmentManagementSearchResultRecord> SearchAssessments(AssessmentManagementSearchRequest request);

    /// <summary>
    /// Returns a paged list of questions linked to an assessment.
    /// </summary>
    /// <param name="assessmentId">The assessment identifier.</param>
    /// <param name="pageNumber">The requested page number.</param>
    /// <param name="pageSize">The requested page size.</param>
    /// <returns>The paged assessment question list.</returns>
    Task<PagedAssessmentQuestionListRecord> GetAssessmentQuestionList(Guid assessmentId, int pageNumber, int pageSize);

    /// <summary>
    /// Retrieves assessments visible to a student for the requested statuses.
    /// </summary>
    /// <param name="studentUserId">The student user identifier.</param>
    /// <param name="assessmentStatuses">The statuses to include.</param>
    /// <returns>The student assessment list.</returns>
    Task<List<StudentAssessmentListItemRecord>> GetStudentAssessments(Guid studentUserId, IReadOnlyCollection<string> assessmentStatuses);

    /// <summary>
    /// Retrieves the full detail required to render a student's assigned assessment.
    /// </summary>
    /// <param name="assessmentId">The assessment identifier.</param>
    /// <param name="studentUserId">The student user identifier.</param>
    /// <returns>The student assessment detail.</returns>
    Task<StudentAssessmentDetailRecord> GetStudentAssessmentDetail(Guid assessmentId, Guid studentUserId);

    /// <summary>
    /// Starts a student's assessment attempt.
    /// </summary>
    /// <param name="assessmentId">The assessment identifier.</param>
    /// <param name="studentUserId">The student user identifier.</param>
    /// <returns>The started attempt state.</returns>
    Task<StudentAssessmentStartRecord> StartStudentAssessment(Guid assessmentId, Guid studentUserId);

    /// <summary>
    /// Recovers a student's in-progress assessment attempt.
    /// </summary>
    /// <param name="attemptId">The attempt identifier.</param>
    /// <param name="studentUserId">The student user identifier.</param>
    /// <returns>The recoverable attempt state.</returns>
    Task<StudentAttemptRecoveryRecord> GetStudentAttemptRecovery(Guid attemptId, Guid studentUserId);

    /// <summary>
    /// Saves an answer for a single question inside a student's attempt.
    /// </summary>
    /// <param name="attemptId">The attempt identifier.</param>
    /// <param name="questionId">The question identifier.</param>
    /// <param name="studentUserId">The student user identifier.</param>
    /// <param name="request">The answer payload to save.</param>
    /// <returns>The saved answer state.</returns>
    Task<StudentAttemptAnswerRecord> SaveStudentAttemptAnswer(Guid attemptId, Guid questionId, Guid studentUserId, SaveStudentAttemptAnswerRequest request);

    /// <summary>
    /// Submits a student's assessment attempt.
    /// </summary>
    /// <param name="attemptId">The attempt identifier.</param>
    /// <param name="studentUserId">The student user identifier.</param>
    /// <returns>The submitted attempt summary.</returns>
    Task<StudentAttemptSubmitRecord> SubmitStudentAttempt(Guid attemptId, Guid studentUserId);
}
