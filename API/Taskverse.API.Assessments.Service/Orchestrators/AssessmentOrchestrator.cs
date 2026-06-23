using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Taskverse.API.Assessments.Service.Managers;
using Taskverse.API.Assessments.Service.Mappings;
using Taskverse.API.Assessments.Service.Models;
using Taskverse.Data.Enums;

namespace Taskverse.API.Assessments.Service.Orchestrators;

public class AssessmentOrchestrator : IAssessmentOrchestrator
{
    private const int MaxInstructionWordCount = 1000;
    private readonly IAssessmentManager _assessmentManager;
    private readonly AssessmentSettings _assessmentSettings;
    private readonly ILogger<AssessmentOrchestrator> _logger;

    public AssessmentOrchestrator(
        IAssessmentManager assessmentManager,
        IOptions<AssessmentSettings> assessmentSettings,
        ILogger<AssessmentOrchestrator> logger)
    {
        _assessmentManager = assessmentManager;
        _assessmentSettings = assessmentSettings.Value;
        _logger = logger;
    }

    public async Task<AssessmentRecord> GetAssessment(
        Guid assessmentId,
        Guid collegeId,
        string requesterRole,
        string requesterName)
    {
        var assessment = await _assessmentManager.GetAssessment(assessmentId, collegeId, requesterRole, requesterName);
        return assessment.ToRecord();
    }

    public async Task<AssessmentRecord> CreateAssessment(CreateAssessmentRequest request)
    {
        var assessment = await _assessmentManager.CreateAssessment(
            request.ToEntity(_assessmentSettings),
            request.QuestionIds ?? []);

        return assessment.ToRecord();
    }

    public async Task<AssessmentRecord> UpdateAssessment(Guid assessmentId, UpdateAssessmentRequest request)
    {
        var assessment = await _assessmentManager.UpdateAssessment(assessmentId, request);
        return assessment.ToRecord();
    }

    public async Task DeleteAssessment(Guid assessmentId, DeleteAssessmentRequest request)
    {
        await _assessmentManager.DeleteAssessment(assessmentId, request);
    }

    public async Task<AssessmentRecord> PublishAssessment(Guid assessmentId)
    {
        var assessment = await _assessmentManager.PublishAssessment(assessmentId);
        return assessment.ToRecord();
    }

    public async Task<AssessmentRecord> PublishAssessment(PublishAssessmentRequest request)
    {
        if (request.AssessmentId.HasValue)
        {
            return await PublishAssessment(request.AssessmentId.Value);
        }

        var createRequest = request.ToCreateAssessmentRequest();
        var assessment = await _assessmentManager.ScheduleAssessment(
            createRequest.ToEntity(_assessmentSettings, AssessmentStatus.Scheduled),
            createRequest.QuestionIds);

        return assessment.ToRecord();
    }

    public async Task<AssessmentAssignmentCatalogRecord> GetTrainerAssignedClassesAndBatches(AssessmentAccessibleBatchesRequest request)
        => await _assessmentManager.GetTrainerAssignedClassesAndBatches(request);

    public async Task<PagedAssessmentSearchRecord> SearchAssessments(AssessmentSearchRequest request)
        => await _assessmentManager.SearchAssessments(request);

    public async Task<PagedAssessmentQuestionListRecord> GetAssessmentQuestionList(
        Guid assessmentId,
        AssessmentQuestionListRequest request)
        => await _assessmentManager.GetAssessmentQuestionList(
            assessmentId,
            request.PageNumber,
            request.PageSize);

    public async Task<List<StudentAssessmentListItemRecord>> GetStudentAssessments(
        StudentAssessmentListRequest request,
        IReadOnlyCollection<string> assessmentStatuses)
        => await _assessmentManager.GetStudentAssessments(request.StudentUserId, assessmentStatuses);

    public async Task<StudentAssessmentDetailRecord> GetStudentAssessmentDetail(Guid assessmentId, Guid studentUserId)
        => await _assessmentManager.GetStudentAssessmentDetail(assessmentId, studentUserId);

    public async Task<StudentAssessmentStartRecord> StartStudentAssessment(Guid assessmentId, Guid studentUserId)
        => await _assessmentManager.StartStudentAssessment(assessmentId, studentUserId);

    public async Task<StudentAttemptRecoveryRecord> GetStudentAttemptRecovery(Guid attemptId, Guid studentUserId)
        => await _assessmentManager.GetStudentAttemptRecovery(attemptId, studentUserId);

    public async Task<ProctorSessionStateRecord> GetAttemptProctorSession(Guid attemptId, Guid collegeId, string requesterRole, string requesterName)
        => await _assessmentManager.GetAttemptProctorSession(attemptId, collegeId, requesterRole, requesterName);

    public async Task<StudentAttemptSubmitRecord> SubmitStudentAttempt(Guid attemptId, Guid studentUserId)
        => await _assessmentManager.SubmitStudentAttempt(attemptId, studentUserId);

    public async Task<CodingReadinessRecord> GetCodingReadinessAsync(Guid attemptId, Guid studentUserId)
        => await _assessmentManager.GetCodingReadinessAsync(attemptId, studentUserId);

    public async Task<StudentAttemptAnswerRecord> SaveStudentAttemptAnswer(
        Guid attemptId,
        Guid questionId,
        Guid studentUserId,
        SaveStudentAttemptAnswerRequest request)
        => await _assessmentManager.SaveStudentAttemptAnswer(attemptId, questionId, studentUserId, request);

    public ObjectResult BuildUnexpectedError(
        Exception exception,
        string message,
        string name = "AssessmentServiceError")
    {
        var detail = exception.GetBaseException().Message;
        _logger.LogError(exception, "{Message} Detail: {Detail}", message, detail);

        return new ObjectResult(new
        {
            name,
            message = detail,
            detail
        })
        {
            StatusCode = StatusCodes.Status500InternalServerError
        };
    }

    public string? ValidateInstructionWordLimit(string? instructions)
    {
        return CountWords(instructions) > MaxInstructionWordCount
            ? $"Instructions cannot exceed {MaxInstructionWordCount} words."
            : null;
    }

    private static int CountWords(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized)
            ? 0
            : normalized.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
    }
}
