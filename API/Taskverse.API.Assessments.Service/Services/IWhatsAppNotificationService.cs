using Taskverse.Data.DataAccess;

namespace Taskverse.API.Assessments.Service.Services;

/// <summary>
/// Abstraction for sending WhatsApp notifications when an assessment goes Live.
/// Implement this interface with a real provider (e.g. Meta Cloud API, Twilio)
/// when WhatsApp integration is ready.
/// </summary>
public interface IWhatsAppNotificationService
{
    /// <summary>
    /// Sends a WhatsApp notification to the relevant students/trainers
    /// informing them that the given assessment is now Live.
    /// </summary>
    /// <param name="assessment">The assessment that has just gone Live.</param>
    /// <param name="ct">Cancellation token.</param>
    Task NotifyAssessmentLiveAsync(Assessment assessment, CancellationToken ct);
}
