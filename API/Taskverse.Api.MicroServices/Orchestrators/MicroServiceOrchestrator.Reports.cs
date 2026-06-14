using Microsoft.AspNetCore.Mvc;
using Taskverse.Api.MicroServices.Enums;
using Taskverse.Api.MicroServices.Models;

namespace Taskverse.Api.MicroServices.Orchestrators;

public partial class MicroServiceOrchestrator
{
    public async Task<ObjectResult> GenerateReport(GenerateReportRequestModel model)
    {
        var url = $"{GetMicroServiceUrl(MicroService.Reports)}reports/generate";
        return await Post<ReportModel>(url, model);
    }

    public async Task<ObjectResult> GetReport(string reportId)
    {
        var url = $"{GetMicroServiceUrl(MicroService.Reports)}reports/{reportId}";
        return await Get<ReportModel>(url);
    }

    public async Task<ObjectResult> GetUserPerformanceReport(string userId)
    {
        var url = $"{GetMicroServiceUrl(MicroService.Reports)}reports/user/{userId}/performance";
        return await Get<UserPerformanceReportModel>(url);
    }

    public async Task<ObjectResult> GetAssessmentReport(string assessmentId)
    {
        var url = $"{GetMicroServiceUrl(MicroService.Reports)}reports/assessment/{assessmentId}";
        return await Get<AssessmentReportModel>(url);
    }

    public async Task<ObjectResult> GetReportsByUser(string userId)
    {
        var url = $"{GetMicroServiceUrl(MicroService.Reports)}reports/user/{userId}";
        return await Get<List<ReportModel>>(url);
    }

    public async Task<ObjectResult> GetStudentResults(Guid studentId)
    {
        var url = $"{GetMicroServiceUrl(MicroService.Reports)}api/results/students/{studentId}";
        return await Get<List<StudentResultModel>>(url);
    }

    public async Task<ObjectResult> GetStudentAttemptResult(Guid studentId, Guid attemptId)
    {
        var url = $"{GetMicroServiceUrl(MicroService.Reports)}api/results/students/{studentId}/attempts/{attemptId}";
        return await Get<StudentResultModel>(url);
    }
}
