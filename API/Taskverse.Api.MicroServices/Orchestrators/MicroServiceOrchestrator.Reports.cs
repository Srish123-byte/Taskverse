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

    public async Task<ObjectResult> GetStudentAttemptResult(Guid attemptId)
    {
        var url = $"{GetMicroServiceUrl(MicroService.Reports)}api/results/students/attempts/{attemptId}";
        return await Get<StudentResultModel>(url);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  NEW ENTERPRISE REPORT CLIENT METHODS
    // ═══════════════════════════════════════════════════════════════════════

    public async Task<ObjectResult> GetCollegeWiseReport(Guid? collegeId, DateTime? dateFrom, DateTime? dateTo, string? academicYear)
    {
        var url = $"{GetMicroServiceUrl(MicroService.Reports)}api/reports/super-admin/college-wise" +
                  BuildQueryString(collegeId, null, null, null, null, null, dateFrom, dateTo, academicYear, null);
        return await Get<CollegeWiseReportModel>(url);
    }

    public async Task<byte[]> ExportCollegeWisePdf(Guid? collegeId, DateTime? dateFrom, DateTime? dateTo, string? academicYear)
    {
        var url = $"{GetMicroServiceUrl(MicroService.Reports)}api/reports/super-admin/college-wise/export/pdf" +
                  BuildQueryString(collegeId, null, null, null, null, null, dateFrom, dateTo, academicYear, null);
        return await GetFileBytes(url);
    }

    public async Task<byte[]> ExportCollegeWiseExcel(Guid? collegeId, DateTime? dateFrom, DateTime? dateTo, string? academicYear)
    {
        var url = $"{GetMicroServiceUrl(MicroService.Reports)}api/reports/super-admin/college-wise/export/excel" +
                  BuildQueryString(collegeId, null, null, null, null, null, dateFrom, dateTo, academicYear, null);
        return await GetFileBytes(url);
    }

    public async Task<ObjectResult> GetBranchWiseReport(Guid? collegeId, Guid? classId, Guid? batchId, DateTime? dateFrom, DateTime? dateTo)
    {
        var url = $"{GetMicroServiceUrl(MicroService.Reports)}api/reports/college-admin/branch-wise" +
                  BuildQueryString(collegeId, classId, batchId, null, null, null, dateFrom, dateTo, null, null);
        return await Get<BranchWiseReportModel>(url);
    }

    public async Task<byte[]> ExportBranchWisePdf(Guid? collegeId, Guid? classId, Guid? batchId, DateTime? dateFrom, DateTime? dateTo)
    {
        var url = $"{GetMicroServiceUrl(MicroService.Reports)}api/reports/college-admin/branch-wise/export/pdf" +
                  BuildQueryString(collegeId, classId, batchId, null, null, null, dateFrom, dateTo, null, null);
        return await GetFileBytes(url);
    }

    public async Task<byte[]> ExportBranchWiseExcel(Guid? collegeId, Guid? classId, Guid? batchId, DateTime? dateFrom, DateTime? dateTo)
    {
        var url = $"{GetMicroServiceUrl(MicroService.Reports)}api/reports/college-admin/branch-wise/export/excel" +
                  BuildQueryString(collegeId, classId, batchId, null, null, null, dateFrom, dateTo, null, null);
        return await GetFileBytes(url);
    }

    public async Task<ObjectResult> GetStudentPerformanceReport(
        Guid? collegeId, Guid? classId, Guid? batchId, Guid? studentId,
        Guid? trainerId, Guid? assessmentId, DateTime? dateFrom, DateTime? dateTo,
        string? performanceLevel)
    {
        var url = $"{GetMicroServiceUrl(MicroService.Reports)}api/reports/trainer/student-performance" +
                  BuildQueryString(collegeId, classId, batchId, studentId, trainerId, assessmentId, dateFrom, dateTo, null, performanceLevel);
        return await Get<StudentPerformanceReportModel>(url);
    }

    public async Task<byte[]> ExportStudentPerformancePdf(
        Guid? collegeId, Guid? classId, Guid? batchId, Guid? studentId,
        Guid? trainerId, Guid? assessmentId, DateTime? dateFrom, DateTime? dateTo,
        string? performanceLevel)
    {
        var url = $"{GetMicroServiceUrl(MicroService.Reports)}api/reports/trainer/student-performance/export/pdf" +
                  BuildQueryString(collegeId, classId, batchId, studentId, trainerId, assessmentId, dateFrom, dateTo, null, performanceLevel);
        return await GetFileBytes(url);
    }

    public async Task<byte[]> ExportStudentPerformanceExcel(
        Guid? collegeId, Guid? classId, Guid? batchId, Guid? studentId,
        Guid? trainerId, Guid? assessmentId, DateTime? dateFrom, DateTime? dateTo,
        string? performanceLevel)
    {
        var url = $"{GetMicroServiceUrl(MicroService.Reports)}api/reports/trainer/student-performance/export/excel" +
                  BuildQueryString(collegeId, classId, batchId, studentId, trainerId, assessmentId, dateFrom, dateTo, null, performanceLevel);
        return await GetFileBytes(url);
    }

    public async Task<ObjectResult> GetCollegesFilter() =>
        await Get<List<FilterOptionModel>>($"{GetMicroServiceUrl(MicroService.Reports)}api/reports/filters/colleges");

    public async Task<ObjectResult> GetBranchesFilter(Guid? collegeId) =>
        await Get<List<FilterOptionModel>>($"{GetMicroServiceUrl(MicroService.Reports)}api/reports/filters/branches?collegeId={collegeId}");

    public async Task<ObjectResult> GetBatchesFilter(Guid? classId) =>
        await Get<List<FilterOptionModel>>($"{GetMicroServiceUrl(MicroService.Reports)}api/reports/filters/batches?classId={classId}");

    public async Task<ObjectResult> GetTrainersFilter(Guid? collegeId) =>
        await Get<List<FilterOptionModel>>($"{GetMicroServiceUrl(MicroService.Reports)}api/reports/filters/trainers?collegeId={collegeId}");

    // Helper method to execute requests for file bytes
    private async Task<byte[]> GetFileBytes(string url)
    {
        var uri = GetValidatedUri(url);
        var client = _httpClientFactory.CreateClient(ClientName);
        PrepareClient(client, uri);
        var response = await client.GetAsync(uri);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadAsByteArrayAsync();
        }
        throw new HttpRequestException($"Failed to download file from reports microservice. Status code: {response.StatusCode}");
    }

    private static string BuildQueryString(
        Guid? collegeId, Guid? classId, Guid? batchId, Guid? studentId,
        Guid? trainerId, Guid? assessmentId, DateTime? dateFrom, DateTime? dateTo,
        string? academicYear, string? performanceLevel)
    {
        var parts = new List<string>();
        if (collegeId.HasValue) parts.Add($"collegeId={collegeId}");
        if (classId.HasValue) parts.Add($"classId={classId}");
        if (batchId.HasValue) parts.Add($"batchId={batchId}");
        if (studentId.HasValue) parts.Add($"studentId={studentId}");
        if (trainerId.HasValue) parts.Add($"trainerId={trainerId}");
        if (assessmentId.HasValue) parts.Add($"assessmentId={assessmentId}");
        if (dateFrom.HasValue) parts.Add($"dateFrom={dateFrom.Value:yyyy-MM-dd}");
        if (dateTo.HasValue) parts.Add($"dateTo={dateTo.Value:yyyy-MM-dd}");
        if (!string.IsNullOrEmpty(academicYear)) parts.Add($"academicYear={Uri.EscapeDataString(academicYear)}");
        if (!string.IsNullOrEmpty(performanceLevel)) parts.Add($"performanceLevel={Uri.EscapeDataString(performanceLevel)}");

        return parts.Count > 0 ? "?" + string.Join("&", parts) : string.Empty;
    }
}
