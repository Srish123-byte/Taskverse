using Microsoft.AspNetCore.Mvc;
using Taskverse.Api.MicroServices.Enums;
using Taskverse.Api.MicroServices.Models;

namespace Taskverse.Api.MicroServices.Orchestrators;

public partial class MicroServiceOrchestrator
{
    public async Task<ObjectResult> GetAssessment(string assessmentId)
    {
        var url = $"{GetMicroServiceUrl(MicroService.Assessment)}assessments/{assessmentId}";
        return await Get<AssessmentModel>(url);
    }

    public async Task<ObjectResult> CreateAssessment(CreateQuestionBankAssessmentModel model)
    {
        var url = $"{GetMicroServiceUrl(MicroService.Assessment)}api/assessments";
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

    public async Task<ObjectResult> SearchQuestionBank(QuestionBankSearchModel model)
    {
        var url = $"{GetMicroServiceUrl(MicroService.Assessment)}api/questions/search";
        return await Post<PagedQuestionBankModel>(url, model);
    }

    public async Task<ObjectResult> GetAssessmentsByUser(string userId)
    {
        var url = $"{GetMicroServiceUrl(MicroService.Assessment)}assessments/user/{userId}";
        return await Get<List<AssessmentModel>>(url);
    }

    public async Task<ObjectResult> GetAssessmentResult(string assessmentId, string userId)
    {
        var url = $"{GetMicroServiceUrl(MicroService.Assessment)}assessments/{assessmentId}/results/{userId}";
        return await Get<AssessmentResultModel>(url);
    }

    public async Task<ObjectResult> GetAssessmentSummary(string assessmentId)
    {
        var url = $"{GetMicroServiceUrl(MicroService.Assessment)}assessments/{assessmentId}/summary";
        return await Get<AssessmentSummaryModel>(url);
    }

    public async Task<ObjectResult> GetAssessmentQuestionList(Guid assessmentId, AssessmentQuestionListSearchModel model)
    {
        var url = $"{GetMicroServiceUrl(MicroService.Assessment)}api/assessments/{assessmentId}/questions/list";
        return await Post<PagedAssessmentQuestionListModel>(url, model);
    }
}
