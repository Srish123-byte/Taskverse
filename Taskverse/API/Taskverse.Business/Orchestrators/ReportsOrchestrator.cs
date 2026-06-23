using log4net;
using Microsoft.AspNetCore.Http;
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
