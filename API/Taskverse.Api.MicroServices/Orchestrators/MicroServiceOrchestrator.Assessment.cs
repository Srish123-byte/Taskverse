using Microsoft.AspNetCore.Mvc;
using Taskverse.Api.MicroServices.Enums;
using Taskverse.Api.MicroServices.Models;

namespace Taskverse.Api.MicroServices.Orchestrators;

public partial class MicroServiceOrchestrator
{
    public async Task<ObjectResult> CreateAssessment(CreateQuestionBankAssessmentModel model)
    {
        var url = $"{GetMicroServiceUrl(MicroService.Assessment)}api/assessments";
        return await Post<QuestionBankAssessmentModel>(url, model);
    }

    public async Task<ObjectResult> GetAssessment(Guid assessmentId, Guid collegeId, string requesterRole, string requesterName)
    {
        var encodedRole = Uri.EscapeDataString(requesterRole ?? string.Empty);
        var encodedRequesterName = Uri.EscapeDataString(requesterName ?? string.Empty);
        var url =
            $"{GetMicroServiceUrl(MicroService.Assessment)}api/assessments/{assessmentId}?collegeId={collegeId}&requesterRole={encodedRole}&requesterName={encodedRequesterName}";

        return await Get<QuestionBankAssessmentModel>(url);
    }

    public async Task<ObjectResult> UpdateAssessment(UpdateQuestionBankAssessmentModel model)
    {
        var url = $"{GetMicroServiceUrl(MicroService.Assessment)}api/assessments/{model.AssessmentId}";
        return await Put<QuestionBankAssessmentModel>(url, model);
    }

    public async Task<ObjectResult> PublishAssessment(PublishQuestionBankAssessmentModel model)
    {
        var url = $"{GetMicroServiceUrl(MicroService.Assessment)}api/assessments/publish";
        return await Post<QuestionBankAssessmentModel>(url, model);
    }

    public async Task<ObjectResult> DeleteAssessment(DeleteAssessmentModel model)
    {
        var url = $"{GetMicroServiceUrl(MicroService.Assessment)}api/assessments/{model.AssessmentId}";
        return await Delete<object>(url, model);
    }

    public async Task<ObjectResult> PublishAssessment(Guid assessmentId)
    {
        var url = $"{GetMicroServiceUrl(MicroService.Assessment)}api/assessments/{assessmentId}/publish";
        return await Post<QuestionBankAssessmentModel>(url, new { });
    }

    public async Task<ObjectResult> CreateQuestions(List<CreateQuestionModel> models)
    {
        var url = $"{GetMicroServiceUrl(MicroService.Assessment)}api/questions";
        return await Post<List<AssessmentQuestionModel>>(url, models);
    }

    public async Task<ObjectResult> GetQuestion(Guid questionId, Guid collegeId)
    {
        var url = $"{GetMicroServiceUrl(MicroService.Assessment)}api/questions/{questionId}?collegeId={collegeId}";
        return await Get<AssessmentQuestionModel>(url);
    }

    public async Task<ObjectResult> UpdateQuestion(Guid questionId, CreateQuestionModel model)
    {
        var url = $"{GetMicroServiceUrl(MicroService.Assessment)}api/questions/{questionId}";
        return await Put<AssessmentQuestionModel>(url, model);
    }

    public async Task<ObjectResult> DeleteQuestions(DeleteQuestionsModel model)
    {
        var url = $"{GetMicroServiceUrl(MicroService.Assessment)}api/questions";
        return await Delete<List<Guid>>(url, model);
    }

    public async Task<ObjectResult> GetQuestionClassificationCatalog()
    {
        var url = $"{GetMicroServiceUrl(MicroService.Assessment)}api/questions/catalog";
        return await Get<QuestionClassificationCatalogModel>(url);
    }

    public async Task<ObjectResult> CreateQuestionClassificationEntry(CreateQuestionClassificationEntryModel model)
    {
        var url = $"{GetMicroServiceUrl(MicroService.Assessment)}api/questions/catalog/items";
        return await Post<QuestionClassificationEntryModel>(url, model);
    }

    public async Task<ObjectResult> GetTrainerAssignedClassesAndBatches(AssessmentBootstrapModel model)
    {
        var url = $"{GetMicroServiceUrl(MicroService.Assessment)}api/assessments/trainer/assigned-classes-batches";
        return await Post<AssessmentAssignmentCatalogModel>(url, model);
    }

    public async Task<ObjectResult> SearchQuestionBank(QuestionBankSearchModel model)
    {
        var url = $"{GetMicroServiceUrl(MicroService.Assessment)}api/questions/search";
        return await Post<PagedQuestionBankModel>(url, model);
    }

    public async Task<ObjectResult> SearchAssessments(AssessmentSearchModel model)
    {
        var url = $"{GetMicroServiceUrl(MicroService.Assessment)}api/assessments/search";
        return await Post<PagedAssessmentSearchModel>(url, model);
    }

    public async Task<ObjectResult> GetAssessmentQuestionList(Guid assessmentId, AssessmentQuestionListSearchModel model)
    {
        var url = $"{GetMicroServiceUrl(MicroService.Assessment)}api/assessments/{assessmentId}/questions/list";
        return await Post<PagedAssessmentQuestionListModel>(url, model);
    }

    public async Task<ObjectResult> GetStudentAssessments(
        StudentAssessmentListSearchModel model,
        IReadOnlyCollection<string> assessmentStatuses)
    {
        var normalizedStatuses = assessmentStatuses
            .Where(status => !string.IsNullOrWhiteSpace(status))
            .Select(status => $"assessmentStatuses={Uri.EscapeDataString(status)}");

        var query = string.Join("&", normalizedStatuses);
        var url = $"{GetMicroServiceUrl(MicroService.Assessment)}api/students/assessments";
        if (!string.IsNullOrWhiteSpace(query))
        {
            url = $"{url}?{query}";
        }

        return await Post<List<StudentAssessmentListItemModel>>(url, model);
    }

    public async Task<ObjectResult> GetStudentAssessmentDetail(Guid assessmentId, Guid studentUserId)
    {
        var url =
            $"{GetMicroServiceUrl(MicroService.Assessment)}api/students/assessments/{assessmentId}?studentUserId={studentUserId}";

        return await Get<StudentAssessmentDetailModel>(url);
    }

    public async Task<ObjectResult> StartStudentAssessment(Guid assessmentId, Guid studentUserId)
    {
        var url =
            $"{GetMicroServiceUrl(MicroService.Assessment)}api/students/assessments/{assessmentId}/start?studentUserId={studentUserId}";

        return await Post<StudentAssessmentStartModel>(url, new { });
    }

    public async Task<ObjectResult> GetStudentAttemptRecovery(Guid attemptId, Guid studentUserId)
    {
        var url =
            $"{GetMicroServiceUrl(MicroService.Assessment)}api/students/attempts/{attemptId}?studentUserId={studentUserId}";

        return await Get<StudentAttemptRecoveryModel>(url);
    }

    public async Task<ObjectResult> SaveStudentAttemptAnswer(
        Guid attemptId,
        Guid questionId,
        Guid studentUserId,
        SaveStudentAttemptAnswerModel model)
    {
        var url =
            $"{GetMicroServiceUrl(MicroService.Assessment)}api/students/attempts/{attemptId}/{questionId}/answers?studentUserId={studentUserId}";

        return await Put<StudentAttemptAnswerModel>(url, model);
    }

    public async Task<ObjectResult> SubmitStudentAttempt(Guid attemptId, Guid studentUserId)
    {
        var url =
            $"{GetMicroServiceUrl(MicroService.Assessment)}api/students/attempts/{attemptId}/submit?studentUserId={studentUserId}";

        return await Post<StudentAttemptSubmitModel>(url, new { });
    }
}
