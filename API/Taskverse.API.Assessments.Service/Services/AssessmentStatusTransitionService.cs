using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Taskverse.API.Assessments.Service.Models;
using Taskverse.Business.Enums;
using Taskverse.Data.DataAccess;

namespace Taskverse.API.Assessments.Service.Services;

/// <summary>
/// A long-running background service that polls the database every minute and
/// transitions assessments from <see cref="AssessmentStatus.Scheduled"/> (Published)
/// to <see cref="AssessmentStatus.Live"/> when their <c>start_datetime</c> has been reached.
///
/// Multi-instance safety: a PostgreSQL session-level advisory lock is acquired at the
/// start of each cycle. If another service instance already holds the lock, this instance
/// skips the cycle entirely — ensuring the status flip and WhatsApp notification fire
/// exactly once per cycle across all instances behind a load balancer.
///
/// Notification contract: the DB transaction is committed BEFORE notifications are
/// dispatched. A notification failure never rolls back a Live status transition.
/// </summary>
public class AssessmentStatusTransitionService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AssessmentStatusTransitionService> _logger;
    private readonly TimeSpan _pollingInterval;
    private readonly long _advisoryLockKey;

    public AssessmentStatusTransitionService(
        IServiceScopeFactory scopeFactory,
        ILogger<AssessmentStatusTransitionService> logger,
        IOptions<AssessmentStatusTransitionSettings> settings)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _pollingInterval = TimeSpan.FromSeconds(settings.Value.PollingIntervalSeconds);
        _advisoryLockKey = settings.Value.AdvisoryLockKey;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "AssessmentStatusTransitionService started. Polling every {Interval}s.",
            _pollingInterval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunCycleAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Host is shutting down — exit cleanly.
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unhandled error in AssessmentStatusTransitionService cycle. " +
                    "Will retry after {Interval}s.",
                    _pollingInterval.TotalSeconds);
            }

            await Task.Delay(_pollingInterval, stoppingToken);
        }

        _logger.LogInformation("AssessmentStatusTransitionService stopped.");
    }

    // ---------------------------------------------------------------------------
    // Core cycle
    // ---------------------------------------------------------------------------

    private async Task RunCycleAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TaskverseContext>();

        // Attempt to acquire a non-blocking PostgreSQL session advisory lock.
        // If another instance holds the lock, skip this cycle immediately.
        var lockAcquired = await TryAcquireAdvisoryLockAsync(context, ct);
        if (!lockAcquired)
        {
            _logger.LogDebug(
                "Advisory lock {Key} already held by another instance — skipping cycle.",
                _advisoryLockKey);
            return;
        }

        try
        {
            await TransitionAndNotifyAsync(context, scope, ct);
        }
        finally
        {
            // Always release the lock so the next cycle (on any instance) can proceed.
            await ReleaseAdvisoryLockAsync(context, ct);
        }
    }

    // ---------------------------------------------------------------------------
    // Transition + notification
    // ---------------------------------------------------------------------------

    private async Task TransitionAndNotifyAsync(
        TaskverseContext context,
        IServiceScope scope,
        CancellationToken ct)
    {
        var utcNow = DateTime.UtcNow;

        // Fetch all Scheduled assessments whose start_datetime has been reached.
        var dueAssessments = await context.Assessments
            .Where(a =>
                a.AssessmentStatus == AssessmentStatus.Scheduled &&
                a.StartDateTime.HasValue &&
                a.StartDateTime.Value <= utcNow)
            .ToListAsync(ct);

        if (dueAssessments.Count == 0)
        {
            _logger.LogDebug("No assessments due for Live transition at {UtcNow}.", utcNow);
            return;
        }

        // Flip status to Live.
        foreach (var assessment in dueAssessments)
        {
            assessment.AssessmentStatus = AssessmentStatus.Live;
            assessment.ModifiedAt = DateTime.UtcNow;
        }

        // Commit the status change BEFORE dispatching notifications.
        // A notification failure must never roll back a Live transition.
        await context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Transitioned {Count} assessment(s) from Scheduled to Live.",
            dueAssessments.Count);

        // Dispatch WhatsApp notifications for each transitioned assessment.
        var notificationService = scope.ServiceProvider
            .GetRequiredService<IWhatsAppNotificationService>();

        foreach (var assessment in dueAssessments)
        {
            try
            {
                await notificationService.NotifyAssessmentLiveAsync(assessment, ct);
            }
            catch (Exception ex)
            {
                // Log and continue — a failed notification for one assessment
                // must not block notifications for the remaining ones.
                _logger.LogError(ex,
                    "Failed to send WhatsApp notification for assessment '{AssessmentId}'.",
                    assessment.AssessmentId);
            }
        }
    }

    // ---------------------------------------------------------------------------
    // Advisory lock helpers
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Attempts to acquire a PostgreSQL session-level advisory lock (non-blocking).
    /// Returns <c>true</c> if the lock was acquired; <c>false</c> if another session holds it.
    /// </summary>
    private async Task<bool> TryAcquireAdvisoryLockAsync(TaskverseContext context, CancellationToken ct)
    {
        // SqlQuery<T>(FormattableString) treats interpolated values as parameterised inputs,
        // avoiding EF1002 and keeping the advisory lock key safely out of raw SQL text.
        // For scalar SqlQuery<T>, EF expects the projected column to be named "Value".
        var result = await context.Database
            .SqlQuery<bool>($"SELECT pg_try_advisory_lock({_advisoryLockKey}) AS \"Value\"")
            .FirstAsync(ct);

        return result;
    }

    /// <summary>
    /// Releases the previously acquired PostgreSQL session-level advisory lock.
    /// </summary>
    private async Task ReleaseAdvisoryLockAsync(TaskverseContext context, CancellationToken ct)
    {
        // ExecuteSql(FormattableString) treats interpolated values as parameterised inputs.
        await context.Database
            .ExecuteSqlAsync($"SELECT pg_advisory_unlock({_advisoryLockKey})", ct);
    }
}
