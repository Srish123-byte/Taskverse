using log4net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Taskverse.Api.MicroServices.Interfaces;
using Taskverse.Api.MicroServices.Models;
using Taskverse.Business.DTOs;
using Taskverse.Business.Interface;
using Taskverse.Business.Mappings;
using Taskverse.Business.Utilities;

namespace Taskverse.Business.Orchestrators;

public class ReportsOrchestrator : IReportsOrchestrator
{
    private readonly IMicroServiceOrchestrator _microServiceOrchestrator;
    private static readonly ILog _log = LogManager.GetLogger(typeof(ReportsOrchestrator));

    public ReportsOrchestrator(IMicroServiceOrchestrator microServiceOrchestrator)
    {
        _microServiceOrchestrator = microServiceOrchestrator;
    }

    public async Task<ReportDto> GenerateReport(GenerateReportDto dto)
    {
        _log.Debug($"ReportsOrchestrator.GenerateReport: type={dto.Type}, userId={dto.UserId}");

        var result = await _microServiceOrchestrator.GenerateReport(dto.ToMicroServiceModel());
        result.EnsureSuccess(nameof(GenerateReport));

        ReportModel model = result.DeserializeValue<ReportModel>()
            ?? throw new InvalidOperationException("GenerateReport returned an empty response.");

        return model.ToDto();
    }

    public async Task<ReportDto> GetReport(string reportId)
    {
        _log.Debug($"ReportsOrchestrator.GetReport: reportId={reportId}");

        var result = await _microServiceOrchestrator.GetReport(reportId);
        result.EnsureSuccess(nameof(GetReport));

        ReportModel model = result.DeserializeValue<ReportModel>()
            ?? throw new InvalidOperationException($"GetReport returned an empty response for reportId={reportId}.");

        return model.ToDto();
    }

    public async Task<UserPerformanceReportDto> GetUserPerformanceReport(string userId)
    {
        _log.Debug($"ReportsOrchestrator.GetUserPerformanceReport: userId={userId}");

        var result = await _microServiceOrchestrator.GetUserPerformanceReport(userId);
        result.EnsureSuccess(nameof(GetUserPerformanceReport));

        UserPerformanceReportModel model = result.DeserializeValue<UserPerformanceReportModel>()
            ?? throw new InvalidOperationException($"GetUserPerformanceReport returned an empty response for userId={userId}.");

        return model.ToDto();
    }

    public async Task<AssessmentReportDto> GetAssessmentReport(string assessmentId)
    {
        _log.Debug($"ReportsOrchestrator.GetAssessmentReport: assessmentId={assessmentId}");

        var result = await _microServiceOrchestrator.GetAssessmentReport(assessmentId);
        result.EnsureSuccess(nameof(GetAssessmentReport));

        AssessmentReportModel model = result.DeserializeValue<AssessmentReportModel>()
            ?? throw new InvalidOperationException($"GetAssessmentReport returned an empty response for assessmentId={assessmentId}.");

        return model.ToDto();
    }

    public async Task<List<ReportDto>> GetReportsByUser(string userId)
    {
        _log.Debug($"ReportsOrchestrator.GetReportsByUser: userId={userId}");

        var result = await _microServiceOrchestrator.GetReportsByUser(userId);
        result.EnsureSuccess(nameof(GetReportsByUser));

        List<ReportModel> models = result.DeserializeValue<List<ReportModel>>()
            ?? throw new InvalidOperationException($"GetReportsByUser returned an empty response for userId={userId}.");

        return models.Select(r => r.ToDto()).ToList();
    }

    public async Task<List<StudentResultDto>> GetStudentResults(Guid studentId)
    {
        _log.Debug($"ReportsOrchestrator.GetStudentResults: studentId={studentId}");

        var result = await _microServiceOrchestrator.GetStudentResults(studentId);
        if (!result.IsSuccess())
        {
            var message = ExtractMessage(result.Value) ?? $"GetStudentResults failed with status {result.StatusCode}.";
            throw result.StatusCode switch
            {
                StatusCodes.Status400BadRequest => new ArgumentException(message),
                StatusCodes.Status404NotFound => new KeyNotFoundException(message),
                StatusCodes.Status503ServiceUnavailable => new HttpRequestException(message),
                _ => new InvalidOperationException(message)
            };
        }

        List<StudentResultModel> models = result.DeserializeValue<List<StudentResultModel>>()
            ?? throw new InvalidOperationException($"GetStudentResults returned an empty response for studentId={studentId}.");

        return models.Select(item => item.ToDto()).ToList();
    }

    public async Task<StudentResultDto> GetStudentAttemptResult(Guid attemptId)
    {
        _log.Debug($"ReportsOrchestrator.GetStudentAttemptResult: attemptId={attemptId}");

        var result = await _microServiceOrchestrator.GetStudentAttemptResult(attemptId);
        if (!result.IsSuccess())
        {
            var message = ExtractMessage(result.Value) ?? $"GetStudentAttemptResult failed with status {result.StatusCode}.";
            throw result.StatusCode switch
            {
                StatusCodes.Status400BadRequest => new ArgumentException(message),
                StatusCodes.Status404NotFound => new KeyNotFoundException(message),
                StatusCodes.Status503ServiceUnavailable => new HttpRequestException(message),
                _ => new InvalidOperationException(message)
            };
        }

        StudentResultModel model = result.DeserializeValue<StudentResultModel>()
            ?? throw new InvalidOperationException(
                $"GetStudentAttemptResult returned an empty response for attemptId={attemptId}.");

        return model.ToDto();
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  ENTERPRISE REPORTS GATEWAY CALLS (DELEGATED TO MICROSERVICE CLIENT)
    // ═══════════════════════════════════════════════════════════════════════

    public async Task<CollegeWiseReportDto> GetCollegeWiseReport(Guid? collegeId, DateTime? dateFrom, DateTime? dateTo, string? academicYear)
    {
        var res = await _microServiceOrchestrator.GetCollegeWiseReport(collegeId, dateFrom, dateTo, academicYear);
        res.EnsureSuccess(nameof(GetCollegeWiseReport));
        var m = res.DeserializeValue<CollegeWiseReportModel>() ?? throw new InvalidOperationException();

        return new CollegeWiseReportDto
        {
            Metadata = MapMetadata(m.Metadata),
            Summary = new CollegeWiseSummaryDto
            {
                TotalColleges = m.Summary.TotalColleges,
                TotalStudents = m.Summary.TotalStudents,
                TotalTrainers = m.Summary.TotalTrainers,
                TotalAssessments = m.Summary.TotalAssessments,
                AverageScore = m.Summary.AverageScore,
                OverallPassPercentage = m.Summary.OverallPassPercentage
            },
            Rows = m.Rows.Select(r => new CollegeWiseRowDto
            {
                CollegeName = r.CollegeName,
                TotalStudents = r.TotalStudents,
                TotalTrainers = r.TotalTrainers,
                TotalAssessments = r.TotalAssessments,
                AssessmentsCompleted = r.AssessmentsCompleted,
                AverageScore = r.AverageScore,
                HighestScore = r.HighestScore,
                LowestScore = r.LowestScore,
                PassPercentage = r.PassPercentage,
                ActiveStudents = r.ActiveStudents,
                PerformanceGrade = r.PerformanceGrade
            }).ToList()
        };
    }

    public async Task<byte[]> ExportCollegeWisePdf(Guid? collegeId, DateTime? dateFrom, DateTime? dateTo, string? academicYear) =>
        await _microServiceOrchestrator.ExportCollegeWisePdf(collegeId, dateFrom, dateTo, academicYear);

    public async Task<byte[]> ExportCollegeWiseExcel(Guid? collegeId, DateTime? dateFrom, DateTime? dateTo, string? academicYear) =>
        await _microServiceOrchestrator.ExportCollegeWiseExcel(collegeId, dateFrom, dateTo, academicYear);

    public async Task<BranchWiseReportDto> GetBranchWiseReport(Guid? collegeId, Guid? classId, Guid? batchId, DateTime? dateFrom, DateTime? dateTo)
    {
        var res = await _microServiceOrchestrator.GetBranchWiseReport(collegeId, classId, batchId, dateFrom, dateTo);
        res.EnsureSuccess(nameof(GetBranchWiseReport));
        var m = res.DeserializeValue<BranchWiseReportModel>() ?? throw new InvalidOperationException();

        return new BranchWiseReportDto
        {
            Metadata = MapMetadata(m.Metadata),
            Summary = new BranchWiseSummaryDto
            {
                TotalBranches = m.Summary.TotalBranches,
                TotalStudents = m.Summary.TotalStudents,
                TotalTrainers = m.Summary.TotalTrainers,
                TotalAssessments = m.Summary.TotalAssessments,
                AverageMarks = m.Summary.AverageMarks,
                OverallPassPercentage = m.Summary.OverallPassPercentage
            },
            Rows = m.Rows.Select(r => new BranchWiseRowDto
            {
                BranchName = r.BranchName,
                TotalStudents = r.TotalStudents,
                TotalTrainers = r.TotalTrainers,
                TotalAssessments = r.TotalAssessments,
                AverageMarks = r.AverageMarks,
                HighestMarks = r.HighestMarks,
                LowestMarks = r.LowestMarks,
                PassPercentage = r.PassPercentage,
                StrongestTopics = r.StrongestTopics,
                WeakestTopics = r.WeakestTopics
            }).ToList()
        };
    }

    public async Task<byte[]> ExportBranchWisePdf(Guid? collegeId, Guid? classId, Guid? batchId, DateTime? dateFrom, DateTime? dateTo) =>
        await _microServiceOrchestrator.ExportBranchWisePdf(collegeId, classId, batchId, dateFrom, dateTo);

    public async Task<byte[]> ExportBranchWiseExcel(Guid? collegeId, Guid? classId, Guid? batchId, DateTime? dateFrom, DateTime? dateTo) =>
        await _microServiceOrchestrator.ExportBranchWiseExcel(collegeId, classId, batchId, dateFrom, dateTo);

    public async Task<StudentPerformanceReportDto> GetStudentPerformanceReport(
        Guid? collegeId, Guid? classId, Guid? batchId, Guid? studentId,
        Guid? trainerId, Guid? assessmentId, DateTime? dateFrom, DateTime? dateTo,
        string? performanceLevel)
    {
        var res = await _microServiceOrchestrator.GetStudentPerformanceReport(
            collegeId, classId, batchId, studentId, trainerId, assessmentId, dateFrom, dateTo, performanceLevel);
        res.EnsureSuccess(nameof(GetStudentPerformanceReport));
        var m = res.DeserializeValue<StudentPerformanceReportModel>() ?? throw new InvalidOperationException();

        return new StudentPerformanceReportDto
        {
            Metadata = MapMetadata(m.Metadata),
            Summary = new StudentPerformanceSummaryDto
            {
                TotalStudents = m.Summary.TotalStudents,
                AveragePercentage = m.Summary.AveragePercentage,
                PassPercentage = m.Summary.PassPercentage,
                HighestPercentage = m.Summary.HighestPercentage,
                LowestPercentage = m.Summary.LowestPercentage,
                PlacementReadyCount = m.Summary.PlacementReadyCount
            },
            Rows = m.Rows.Select(r => new StudentPerformanceRowDto
            {
                StudentId = r.StudentId,
                Name = r.Name,
                EnrollmentNumber = r.EnrollmentNumber,
                CollegeName = r.CollegeName,
                BranchName = r.BranchName,
                Semester = r.Semester,
                BatchName = r.BatchName,
                TrainerName = r.TrainerName,
                Assessments = r.Assessments.Select(a => new AssessmentBreakdownDto
                {
                    AssessmentName = a.AssessmentName,
                    AssessmentType = a.AssessmentType,
                    ObtainedMarks = a.ObtainedMarks,
                    TotalMarks = a.TotalMarks,
                    Percentage = a.Percentage,
                    Rank = a.Rank,
                    Status = a.Status,
                    Date = a.Date
                }).ToList(),
                TotalMarks = r.TotalMarks,
                TotalObtained = r.TotalObtained,
                OverallPercentage = r.OverallPercentage,
                OverallRank = r.OverallRank,
                CollegeRank = r.CollegeRank,
                BatchRank = r.BatchRank,
                CompletionRate = r.CompletionRate,
                PlacementReadiness = r.PlacementReadiness,
                PerformanceTrend = r.PerformanceTrend,
                AiInsights = new StudentAiInsightsDto
                {
                    LearningGaps = r.AiInsights.LearningGaps,
                    RootCauseAnalysis = r.AiInsights.RootCauseAnalysis,
                    WeakTopics = r.AiInsights.WeakTopics,
                    StrongTopics = r.AiInsights.StrongTopics,
                    CommunicationGaps = r.AiInsights.CommunicationGaps,
                    InterviewReadiness = r.AiInsights.InterviewReadiness,
                    RecommendedPracticeAreas = r.AiInsights.RecommendedPracticeAreas,
                    SuggestedResources = r.AiInsights.SuggestedResources,
                    PriorityLevel = r.AiInsights.PriorityLevel,
                    ImprovementPlan = r.AiInsights.ImprovementPlan
                }
            }).ToList()
        };
    }

    public async Task<byte[]> ExportStudentPerformancePdf(
        Guid? collegeId, Guid? classId, Guid? batchId, Guid? studentId,
        Guid? trainerId, Guid? assessmentId, DateTime? dateFrom, DateTime? dateTo,
        string? performanceLevel) =>
        await _microServiceOrchestrator.ExportStudentPerformancePdf(
            collegeId, classId, batchId, studentId, trainerId, assessmentId, dateFrom, dateTo, performanceLevel);

    public async Task<byte[]> ExportStudentPerformanceExcel(
        Guid? collegeId, Guid? classId, Guid? batchId, Guid? studentId,
        Guid? trainerId, Guid? assessmentId, DateTime? dateFrom, DateTime? dateTo,
        string? performanceLevel) =>
        await _microServiceOrchestrator.ExportStudentPerformanceExcel(
            collegeId, classId, batchId, studentId, trainerId, assessmentId, dateFrom, dateTo, performanceLevel);

    public async Task<List<FilterOptionDto>> GetCollegesFilter() =>
        await MapFilters(await _microServiceOrchestrator.GetCollegesFilter());

    public async Task<List<FilterOptionDto>> GetBranchesFilter(Guid? collegeId) =>
        await MapFilters(await _microServiceOrchestrator.GetBranchesFilter(collegeId));

    public async Task<List<FilterOptionDto>> GetBatchesFilter(Guid? classId) =>
        await MapFilters(await _microServiceOrchestrator.GetBatchesFilter(classId));

    public async Task<List<FilterOptionDto>> GetTrainersFilter(Guid? collegeId) =>
        await MapFilters(await _microServiceOrchestrator.GetTrainersFilter(collegeId));

    // Private helpers
    private static ReportMetadataDto MapMetadata(ReportMetadataModel m)
    {
        return new ReportMetadataDto
        {
            ReportTitle = m.ReportTitle,
            GeneratedDate = m.GeneratedDate,
            GeneratedTime = m.GeneratedTime,
            GeneratedBy = m.GeneratedBy,
            AppliedFilters = m.AppliedFilters,
            AcademicYear = m.AcademicYear
        };
    }

    private static async Task<List<FilterOptionDto>> MapFilters(ObjectResult result)
    {
        result.EnsureSuccess(nameof(MapFilters));
        var list = result.DeserializeValue<List<FilterOptionModel>>() ?? new List<FilterOptionModel>();
        return list.Select(f => new FilterOptionDto { Id = f.Id, Name = f.Name }).ToList();
    }

    private static string? ExtractMessage(object? value)
    {
        if (value is null)
        {
            return null;
        }

        if (value is string json)
        {
            try
            {
                var parsed = JObject.Parse(json);
                return parsed["message"]?.ToString()
                    ?? parsed["Message"]?.ToString()
                    ?? parsed["detail"]?.ToString()
                    ?? parsed["Detail"]?.ToString();
            }
            catch
            {
                return json;
            }
        }

        var token = JToken.FromObject(value);
        return token["message"]?.ToString()
            ?? token["Message"]?.ToString()
            ?? token["detail"]?.ToString()
            ?? token["Detail"]?.ToString()
            ?? value.ToString();
    }
}
