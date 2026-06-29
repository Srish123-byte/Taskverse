using Microsoft.AspNetCore.Mvc;
using Taskverse.API.Assessments.Service.Models;

namespace Taskverse.API.Assessments.Service.Orchestrators;

public interface IAssessmentOrchestrator
{
    Task<AssessmentRecord> GetAssessment(Guid assessmentId, Guid collegeId, string requesterRole, string requesterName);

    Task<AssessmentRecord> CreateAssessment(CreateAssessmentRequest request);

    Task<AssessmentRecord> UpdateAssessment(Guid assessmentId, UpdateAssessmentRequest request);

    Task DeleteAssessment(Guid assessmentId, DeleteAssessmentRequest request);

    Task<AssessmentRecord> PublishAssessment(Guid assessmentId);

    Task<AssessmentRecord> PublishAssessment(PublishAssessmentRequest request);

    Task<AssessmentAssignmentCatalogRecord> GetTrainerAssignedClassesAndBatches(AssessmentAccessibleBatchesRequest request);

    Task<PagedAssessmentSearchRecord> SearchAssessments(AssessmentSearchRequest request);

    Task<PagedAssessmentQuestionListRecord> GetAssessmentQuestionList(Guid assessmentId, AssessmentQuestionListRequest request);

    Task<List<StudentAssessmentListItemRecord>> GetStudentAssessments(
        StudentAssessmentListRequest request,
        IReadOnlyCollection<string> assessmentStatuses);

    Task<StudentAssessmentDetailRecord> GetStudentAssessmentDetail(Guid assessmentId, Guid studentUserId);

    Task<StudentAssessmentStartRecord> StartStudentAssessment(Guid assessmentId, Guid studentUserId);

    Task<StudentAttemptRecoveryRecord> GetStudentAttemptRecovery(Guid attemptId, Guid studentUserId);

    Task<ProctorSessionStateRecord> GetAttemptProctorSession(Guid attemptId, Guid collegeId, string requesterRole, string requesterName);

    Task<StudentAttemptSubmitRecord> SubmitStudentAttempt(Guid attemptId, Guid studentUserId);

    Task<StudentStreakRecord> GetStudentStreak(Guid studentUserId);

    Task<StudentAttemptAnswerRecord> SaveStudentAttemptAnswer(
        Guid attemptId,
        Guid questionId,
        Guid studentUserId,
        SaveStudentAttemptAnswerRequest request);

    ObjectResult BuildUnexpectedError(Exception exception, string message, string name = "AssessmentServiceError");

    string? ValidateInstructionWordLimit(string? instructions);
}
