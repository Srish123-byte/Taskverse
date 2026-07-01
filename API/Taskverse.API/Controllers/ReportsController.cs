using Microsoft.AspNetCore.Mvc;
using Taskverse.Business.Interface;

namespace Taskverse.Api.Controllers;

public class SendReportEmailRequest
{
    public List<string> Recipients { get; set; } = [];
    public string FileName { get; set; } = "report.xlsx";
    public string FileContentBase64 { get; set; } = string.Empty;
    public string? Subject { get; set; }
    public string? Body { get; set; }
}

[ApiController]
[Route("api/reports")]
[Produces("application/json")]
public class ReportsController : TaskverseBaseController
{
    private readonly IEmailService _emailService;

    public ReportsController(IEmailService emailService)
    {
        _emailService = emailService;
    }

    [HttpPost("send-email")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SendReportEmail(
        [FromBody] SendReportEmailRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null || request.Recipients.Count == 0)
            return BadRequest(new { message = "At least one recipient is required." });

        if (string.IsNullOrWhiteSpace(request.FileContentBase64))
            return BadRequest(new { message = "File content is required." });

        byte[] fileBytes;
        try
        {
            fileBytes = Convert.FromBase64String(request.FileContentBase64);
        }
        catch
        {
            return BadRequest(new { message = "Invalid file content encoding." });
        }

        var subject = request.Subject ?? "Taskverse Report";
        var body = request.Body ?? $"<p>Please find attached the Taskverse report: <strong>{request.FileName}</strong>.</p><p>This is an automated email from Taskverse. Please do not reply.</p>";

        var message = new EmailMessage
        {
            ToAddresses = request.Recipients
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .Select(r => new EmailRecipient { Address = r.Trim() })
                .ToList(),
            Subject = subject,
            HtmlBody = body,
            Attachments =
            [
                new EmailAttachment
                {
                    FileName = request.FileName,
                    ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    Content = fileBytes
                }
            ]
        };

        try
        {
            await _emailService.SendEmailAsync(message, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.GetBaseException().Message });
        }
    }
}
