using Taskverse.Data.DataAccess;

namespace Taskverse.API.Assessments.Service.Services;

/// <summary>
/// No-op stub implementation of <see cref="IWhatsAppNotificationService"/>.
/// Logs the intent without sending any real message.
/// Replace this class (or register a real implementation) once a
/// WhatsApp provider (e.g. Meta Cloud API, Twilio) is integrated.
/// </summary>
public class WhatsAppNotificationService : IWhatsAppNotificationService
{
    private readonly ILogger<WhatsAppNotificationService> _logger;

    public WhatsAppNotificationService(ILogger<WhatsAppNotificationService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task NotifyAssessmentLiveAsync(Assessment assessment, CancellationToken ct)
    {
        _logger.LogInformation(
            "[WhatsApp STUB] Assessment '{AssessmentName}' ({AssessmentId}) is now Live — " +
            "WhatsApp notification not yet implemented.",
            assessment.AssessmentName,
            assessment.AssessmentId);

        return Task.CompletedTask;
    }
}
