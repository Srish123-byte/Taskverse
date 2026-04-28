using Microsoft.AspNetCore.Mvc;
using Taskverse.Api.MicroServices.Enums;
using Taskverse.Api.MicroServices.Models;

namespace Taskverse.Api.MicroServices.Orchestrators;

public partial class MicroServiceOrchestrator
{
    public async Task<ObjectResult> GetExam(string examId)
    {
        var url = $"{GetMicroServiceUrl(MicroService.ExamEngine)}exams/{examId}";
        return await Get<ExamModel>(url);
    }

    public async Task<ObjectResult> CreateExam(CreateExamModel model)
    {
        var url = $"{GetMicroServiceUrl(MicroService.ExamEngine)}exams";
        return await Post<ExamModel>(url, model);
    }

    public async Task<ObjectResult> GetExamQuestions(string examId)
    {
        var url = $"{GetMicroServiceUrl(MicroService.ExamEngine)}exams/{examId}/questions";
        return await Get<List<QuestionModel>>(url);
    }

    public async Task<ObjectResult> SubmitExam(ExamSubmissionModel model)
    {
        var url = $"{GetMicroServiceUrl(MicroService.ExamEngine)}exams/submit";
        return await Post<ExamResultModel>(url, model);
    }

    public async Task<ObjectResult> GetExamResult(string submissionId)
    {
        var url = $"{GetMicroServiceUrl(MicroService.ExamEngine)}exams/results/{submissionId}";
        return await Get<ExamResultModel>(url);
    }

    public async Task<ObjectResult> GetExamsByUser(string userId)
    {
        var url = $"{GetMicroServiceUrl(MicroService.ExamEngine)}exams/user/{userId}";
        return await Get<List<ExamModel>>(url);
    }
}
