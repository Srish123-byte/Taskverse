using System.Text;
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

public class AttendanceOrchestrator : IAttendanceOrchestrator
{
    private static readonly ILog _log = LogManager.GetLogger(typeof(AttendanceOrchestrator));

    private readonly IMicroServiceOrchestrator _microServiceOrchestrator;
    private readonly IEmailService _emailService;

    public AttendanceOrchestrator(
        IMicroServiceOrchestrator microServiceOrchestrator,
        IEmailService emailService)
    {
        _microServiceOrchestrator = microServiceOrchestrator;
        _emailService = emailService;
    }

    public async Task<List<AttendanceBatchGroupDto>> GetAttendanceBatches(Guid collegeId, Guid requesterUserId)
    {
        _log.Debug($"AttendanceOrchestrator.GetAttendanceBatches: collegeId={collegeId}, requesterUserId={requesterUserId}");

        var result = await _microServiceOrchestrator.GetAttendanceBatches(collegeId, requesterUserId);
        EnsureMicroServiceSuccess(result, nameof(GetAttendanceBatches));

        var models = result.DeserializeValue<List<AttendanceBatchGroupModel>>()
            ?? throw new InvalidOperationException("GetAttendanceBatches returned an empty response.");

        return models.Select(item => item.ToDto()).ToList();
    }

    public async Task<AttendanceRosterDto> GetAttendanceRoster(AttendanceRosterRequestDto dto)
    {
        _log.Debug($"AttendanceOrchestrator.GetAttendanceRoster: collegeId={dto.CollegeId}, batchId={dto.BatchId}, date={dto.AttendanceDate:yyyy-MM-dd}, session={(int)dto.AttendanceSession}");

        var result = await _microServiceOrchestrator.GetAttendanceRoster(dto.ToMicroServiceModel());
        EnsureMicroServiceSuccess(result, nameof(GetAttendanceRoster));

        var model = result.DeserializeValue<AttendanceRosterModel>()
            ?? throw new InvalidOperationException("GetAttendanceRoster returned an empty response.");

        return model.ToDto();
    }

    public async Task<AttendanceRosterDto> SubmitAttendance(SubmitAttendanceDto dto)
    {
        _log.Debug($"AttendanceOrchestrator.SubmitAttendance: collegeId={dto.CollegeId}, batchId={dto.BatchId}, date={dto.AttendanceDate:yyyy-MM-dd}, session={(int)dto.AttendanceSession}, entryCount={dto.Entries.Count}");

        var result = await _microServiceOrchestrator.SubmitAttendance(dto.ToMicroServiceModel());
        EnsureMicroServiceSuccess(result, nameof(SubmitAttendance));

        var model = result.DeserializeValue<AttendanceRosterModel>()
            ?? throw new InvalidOperationException("SubmitAttendance returned an empty response.");

        return model.ToDto();
    }

    public async Task<AttendanceHistoryDto> GetAttendanceHistory(AttendanceHistoryRequestDto dto)
    {
        _log.Debug($"AttendanceOrchestrator.GetAttendanceHistory: collegeId={dto.CollegeId}, batchId={dto.BatchId}, fromDate={dto.FromDate:yyyy-MM-dd}, toDate={dto.ToDate:yyyy-MM-dd}");

        var result = await _microServiceOrchestrator.GetAttendanceHistory(dto.CollegeId, dto.RequesterUserId, dto.BatchId, dto.FromDate, dto.ToDate);
        EnsureMicroServiceSuccess(result, nameof(GetAttendanceHistory));

        var model = result.DeserializeValue<AttendanceHistoryModel>()
            ?? throw new InvalidOperationException("GetAttendanceHistory returned an empty response.");

        return model.ToDto();
    }

    public async Task<AttendanceExportDto> ExportAttendance(AttendanceHistoryRequestDto dto)
    {
        _log.Debug($"AttendanceOrchestrator.ExportAttendance: collegeId={dto.CollegeId}, batchId={dto.BatchId}, fromDate={dto.FromDate:yyyy-MM-dd}, toDate={dto.ToDate:yyyy-MM-dd}");

        var result = await _microServiceOrchestrator.ExportAttendance(dto.CollegeId, dto.RequesterUserId, dto.BatchId, dto.FromDate, dto.ToDate);
        EnsureMicroServiceSuccess(result, nameof(ExportAttendance));

        var model = result.DeserializeValue<AttendanceExportModel>()
            ?? throw new InvalidOperationException("ExportAttendance returned an empty response.");

        return model.ToDto();
    }

    public async Task EmailAttendanceReport(AttendanceEmailReportDto dto)
    {
        if (dto.RecipientEmails.Count == 0)
        {
            throw new InvalidOperationException("At least one recipient email is required.");
        }

        if (dto.RecipientEmails.Count > 5)
        {
            throw new InvalidOperationException("A maximum of 5 recipient emails is allowed.");
        }

        var normalizedEmails = dto.RecipientEmails
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (normalizedEmails.Count == 0)
        {
            throw new InvalidOperationException("At least one valid recipient email is required.");
        }

        var exportDto = await ExportAttendance(new AttendanceHistoryRequestDto
        {
            CollegeId = dto.CollegeId,
            RequesterUserId = dto.RequesterUserId,
            BatchId = dto.BatchId,
            FromDate = dto.FromDate,
            ToDate = dto.ToDate
        });

        var attachmentBytes = Convert.FromBase64String(exportDto.ContentBase64);
        await _emailService.SendEmailAsync(
            new EmailMessage
            {
                ToAddresses = normalizedEmails
                    .Select(item => new EmailRecipient { Address = item })
                    .ToList(),
                Subject = $"Taskverse attendance report - {exportDto.BatchName} ({exportDto.FromDate:yyyy-MM-dd} to {exportDto.ToDate:yyyy-MM-dd})",
                HtmlBody = BuildAttendanceEmailBody(exportDto),
                Attachments =
                [
                    new EmailAttachment
                    {
                        FileName = exportDto.FileName,
                        ContentType = exportDto.ContentType,
                        Content = attachmentBytes
                    }
                ]
            });
    }

    private static string BuildAttendanceEmailBody(AttendanceExportDto exportDto)
    {
        var builder = new StringBuilder();
        builder.Append("<p>Hello,</p>");
        builder.Append($"<p>Please find attached the attendance report for <strong>{exportDto.BatchName}</strong> from <strong>{exportDto.FromDate:yyyy-MM-dd}</strong> to <strong>{exportDto.ToDate:yyyy-MM-dd}</strong>.</p>");
        builder.Append($"<p>The attached file contains <strong>{exportDto.Entries.Count}</strong> attendance entries.</p>");
        builder.Append("<table style=\"border-collapse:collapse;border:1px solid #d0d7de;\">");
        builder.Append("<thead><tr>");
        builder.Append("<th style=\"padding:8px;border:1px solid #d0d7de;\">Date</th>");
        builder.Append("<th style=\"padding:8px;border:1px solid #d0d7de;\">Session</th>");
        builder.Append("<th style=\"padding:8px;border:1px solid #d0d7de;\">Student</th>");
        builder.Append("<th style=\"padding:8px;border:1px solid #d0d7de;\">Enrollment Number</th>");
        builder.Append("<th style=\"padding:8px;border:1px solid #d0d7de;\">Email</th>");
        builder.Append("<th style=\"padding:8px;border:1px solid #d0d7de;\">Status</th>");
        builder.Append("</tr></thead><tbody>");

        foreach (var item in exportDto.Entries.OrderBy(row => row.AttendanceDate).ThenBy(row => (int)row.AttendanceSession).ThenBy(row => row.StudentName))
        {
            builder.Append("<tr>");
            builder.Append($"<td style=\"padding:8px;border:1px solid #d0d7de;\">{item.AttendanceDate:yyyy-MM-dd}</td>");
            builder.Append($"<td style=\"padding:8px;border:1px solid #d0d7de;\">{item.AttendanceSession}</td>");
            builder.Append($"<td style=\"padding:8px;border:1px solid #d0d7de;\">{System.Net.WebUtility.HtmlEncode(item.StudentName)}</td>");
            builder.Append($"<td style=\"padding:8px;border:1px solid #d0d7de;\">{System.Net.WebUtility.HtmlEncode(item.EnrollmentNumber ?? "-")}</td>");
            builder.Append($"<td style=\"padding:8px;border:1px solid #d0d7de;\">{System.Net.WebUtility.HtmlEncode(item.Email)}</td>");
            builder.Append($"<td style=\"padding:8px;border:1px solid #d0d7de;\">{item.AttendanceEntry}</td>");
            builder.Append("</tr>");
        }

        builder.Append("</tbody></table>");
        builder.Append("<p>Regards,<br/>Taskverse</p>");
        return builder.ToString();
    }

    private static void EnsureMicroServiceSuccess(Microsoft.AspNetCore.Mvc.ObjectResult result, string operationName)
    {
        if (result.IsSuccess())
        {
            return;
        }

        var message = ExtractMessage(result.Value);
        if (result.StatusCode == StatusCodes.Status404NotFound)
        {
            throw new KeyNotFoundException(message ?? $"{operationName} failed with status {result.StatusCode}.");
        }

        if (result.StatusCode == StatusCodes.Status409Conflict)
        {
            throw new InvalidOperationException(message ?? $"{operationName} failed with status {result.StatusCode}.");
        }

        throw new InvalidOperationException(message ?? $"{operationName} failed with status {result.StatusCode}.");
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
                    ?? json;
            }
            catch
            {
                return json;
            }
        }

        var token = JToken.FromObject(value);
        return token["message"]?.ToString() ?? token["Message"]?.ToString();
    }
}
