using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
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

    public async Task<ObjectResult> GetStudentAttemptResult(Guid attemptId)
    {
        var url = $"{GetMicroServiceUrl(MicroService.Reports)}api/results/students/attempts/{attemptId}";
        return await Get<StudentResultModel>(url);
    }

    public async Task<IActionResult> GetFile(string url)
    {
        var uri = GetValidatedUri(url);
        var client = _httpClientFactory.CreateClient(ClientName);
        PrepareClient(client, uri);
        LogRequestStart(HttpMethod.Get, uri);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await client.GetAsync(uri);
            stopwatch.Stop();
            LogRequestCompletion(HttpMethod.Get, uri, response.StatusCode, stopwatch.ElapsedMilliseconds);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsByteArrayAsync();
                var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
                var contentDisposition = response.Content.Headers.ContentDisposition?.ToString();
                var fileName = "download";
                if (!string.IsNullOrEmpty(contentDisposition) && contentDisposition.Contains("filename="))
                {
                    fileName = contentDisposition.Split("filename=")[1].Trim('"');
                }
                return new FileContentResult(content, contentType) { FileDownloadName = fileName };
            }
            
            return await GetResult<object>(response, url);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _log.Error($"[MicroServiceOrchestrator] GET File request failed for URL {url}: {ex.Message}", ex);
            throw;
        }
    }

    public async Task<IActionResult> ExportCollegeReport(Guid collegeId, string format)
    {
        var url = $"{GetMicroServiceUrl(MicroService.Reports)}api/reports/export/college/{collegeId}?format={format}";
        return await GetFile(url); 
    }

    public async Task<IActionResult> ExportBranchReport(Guid branchId, string format)
    {
        var url = $"{GetMicroServiceUrl(MicroService.Reports)}api/reports/export/branch/{branchId}?format={format}";
        return await GetFile(url); 
    }

    public async Task<IActionResult> ExportStudentReport(Guid studentId, string format)
    {
        var url = $"{GetMicroServiceUrl(MicroService.Reports)}api/reports/export/student/{studentId}?format={format}";
        return await GetFile(url); 
    }
}
