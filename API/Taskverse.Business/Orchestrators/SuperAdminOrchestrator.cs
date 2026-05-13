using log4net;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Taskverse.Api.MicroServices.Interfaces;
using Taskverse.Api.MicroServices.Models;
using Taskverse.Business.DTOs;
using Taskverse.Business.Interface;
using Taskverse.Business.Mappings;
using Taskverse.Business.Utilities;
using Taskverse.Data.DataAccess;

namespace Taskverse.Business.Orchestrators;

public class SuperAdminOrchestrator : ISuperAdminOrchestrator
{
    private const string HealthyStatus = "Healthy";

    private readonly IMicroServiceOrchestrator _microServiceOrchestrator;
    private readonly IDbContextFactory<TaskverseContext> _dbContextFactory;
    private static readonly ILog _log = LogManager.GetLogger(typeof(SuperAdminOrchestrator));

    public SuperAdminOrchestrator(
        IMicroServiceOrchestrator microServiceOrchestrator,
        IDbContextFactory<TaskverseContext> dbContextFactory)
    {
        _microServiceOrchestrator = microServiceOrchestrator;
        _dbContextFactory = dbContextFactory;
    }

    public async Task<SuperAdminDashboardDto> GetDashboard()
    {
        _log.Debug("SuperAdminOrchestrator.GetDashboard");

        var collegesTask = GetColleges();
        var pendingTask = GetPendingColleges();
        var totalsTask = GetAssessmentTotals();

        await Task.WhenAll(collegesTask, pendingTask, totalsTask);

        var totals = await totalsTask;
        var colleges = await collegesTask;

        return new SuperAdminDashboardDto
        {
            Totals = new SuperAdminTotalsDto
            {
                ActiveColleges = colleges.Count(c => c.IsActive),
                RegisteredStudents = 0,
                AssessmentsThisMonth = totals.ThisMonth,
                AssessmentsPreviousMonth = totals.PreviousMonth
            },
            PendingApprovals = await pendingTask,
            PlatformHealth = new PlatformHealthDto
            {
                UptimePercent = 99.95,
                ErrorRatePercent = 0.05,
                ApiStatus = HealthyStatus
            },
            RecentActivity = [],
            AverageScoresByCollege = [],
            UsageTrends = []
        };
    }

    public async Task<List<CollegeDto>> GetColleges()
    {
        _log.Debug("SuperAdminOrchestrator.GetColleges");
        var result = await _microServiceOrchestrator.GetColleges();
        result.EnsureSuccess(nameof(GetColleges));

        var models = result.DeserializeValue<List<CollegeModel>>()
            ?? throw new InvalidOperationException("GetColleges returned empty.");

        return models.Select(c => c.ToDto()).ToList();
    }

    public async Task<List<CollegeDto>> GetPendingColleges()
    {
        _log.Debug("SuperAdminOrchestrator.GetPendingColleges");
        var result = await _microServiceOrchestrator.GetPendingColleges();
        result.EnsureSuccess(nameof(GetPendingColleges));

        var models = result.DeserializeValue<List<CollegeModel>>()
            ?? throw new InvalidOperationException("GetPendingColleges returned empty.");

        return models.Select(c => c.ToDto()).ToList();
    }

    public async Task<List<PendingUserDto>> GetPendingUsers()
    {
        _log.Debug("SuperAdminOrchestrator.GetPendingUsers");
        var result = await _microServiceOrchestrator.GetPendingUsers();
        result.EnsureSuccess(nameof(GetPendingUsers));

        var models = result.DeserializeValue<List<PendingUserModel>>()
            ?? throw new InvalidOperationException("GetPendingUsers returned empty.");

        return models.Select(user => user.ToDto()).ToList();
    }

    public async Task<CollegeDto> ApproveCollege(string collegeId, CollegeActionDto dto) =>
        await ExecuteCollegeAction(nameof(ApproveCollege), collegeId, dto, _microServiceOrchestrator.ApproveCollege);

    public async Task<CollegeDto> RejectCollege(string collegeId, CollegeActionDto dto) =>
        await ExecuteCollegeAction(nameof(RejectCollege), collegeId, dto, _microServiceOrchestrator.RejectCollege);

    public async Task<CollegeDto> DeactivateCollege(string collegeId, CollegeActionDto dto) =>
        await ExecuteCollegeAction(nameof(DeactivateCollege), collegeId, dto, _microServiceOrchestrator.DeactivateCollege);

    public async Task<CollegeDto> ReactivateCollege(string collegeId, CollegeActionDto dto) =>
        await ExecuteCollegeAction(nameof(ReactivateCollege), collegeId, dto, _microServiceOrchestrator.ReactivateCollege);

    private async Task<CollegeDto> ExecuteCollegeAction(
        string operationName,
        string collegeId,
        CollegeActionDto dto,
        Func<string, CollegeActionModel, Task<Microsoft.AspNetCore.Mvc.ObjectResult>> operation)
    {
        _log.Debug($"SuperAdminOrchestrator.{operationName}: collegeId={collegeId}");
        var result = await operation(collegeId, dto.ToMicroServiceModel());
        result.EnsureSuccess(operationName);

        var model = result.DeserializeValue<CollegeModel>()
            ?? throw new InvalidOperationException($"{operationName} returned empty for collegeId={collegeId}.");

        return model.ToDto();
    }

    private async Task<(int ThisMonth, int PreviousMonth)> GetAssessmentTotals()
    {
        try
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();

            var utcNow = DateTime.UtcNow;
            var startOfThisMonth = new DateTime(utcNow.Year, utcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var startOfPreviousMonth = startOfThisMonth.AddMonths(-1);

            var thisMonth = await context.Assessments.CountAsync(a => a.CreatedAt >= startOfThisMonth);
            var previousMonth = await context.Assessments.CountAsync(a =>
                a.CreatedAt >= startOfPreviousMonth && a.CreatedAt < startOfThisMonth);

            return (thisMonth, previousMonth);
        }
        catch (PostgresException ex) when (IsMissingRelation(ex))
        {
            _log.Warn("SuperAdminOrchestrator.GetAssessmentTotals: assessments table is missing. Returning zero totals.", ex);
            return (0, 0);
        }
    }

    private async Task<List<RecentActivityDto>> GetRecentActivity()
    {
        try
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();

            var recentLogs = await context.AuditLogs
                .AsNoTracking()
                .OrderByDescending(a => a.OccurredAt)
                .Take(20)
                .Join(
                    context.Users.AsNoTracking(),
                    audit => audit.UserId,
                    user => user.Id,
                    (audit, user) => new { audit, user.FullName })
                .ToListAsync();

            return recentLogs
                .Select(x => x.audit.ToDto(x.FullName))
                .ToList();
        }
        catch (PostgresException ex) when (IsMissingRelation(ex))
        {
            _log.Warn("SuperAdminOrchestrator.GetRecentActivity: required audit tables are missing. Returning empty activity list.", ex);
            return [];
        }
    }

    private async Task<List<CollegeScoreSummaryDto>> GetAverageScoresByCollege()
    {
        try
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();

            return await context.AssessmentResults
                .AsNoTracking()
                .Join(context.Users.AsNoTracking(),
                    result => result.UserId,
                    user => user.Id,
                    (result, user) => new { result, user })
                .Where(x => x.user.CollegeId.HasValue && x.result.Score.HasValue)
                .Join(context.Colleges.AsNoTracking(),
                    x => x.user.CollegeId!.Value,
                    college => college.CollegeId,
                    (x, college) => new { x.result, x.user, college })
                .GroupBy(x => new { x.college.CollegeId, x.college.Name })
                .Select(group => new CollegeScoreSummaryDto
                {
                    CollegeId = group.Key.CollegeId.ToString(),
                    CollegeName = group.Key.Name,
                    AverageScore = Math.Round(group.Average(x => x.result.Score ?? 0), 2),
                    StudentsAssessed = group.Select(x => x.user.Id).Distinct().Count()
                })
                .OrderByDescending(x => x.AverageScore)
                .Take(10)
                .ToListAsync();
        }
        catch (PostgresException ex) when (IsMissingRelation(ex))
        {
            _log.Warn("SuperAdminOrchestrator.GetAverageScoresByCollege: required assessment tables are missing. Returning empty scores.", ex);
            return [];
        }
    }

    private async Task<List<UsageTrendPointDto>> GetUsageTrends()
    {
        try
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();

            var utcToday = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
            var rangeStart = utcToday.AddDays(-29);

            return await context.AssessmentResults
                .AsNoTracking()
                .Where(result => result.CreatedAt >= rangeStart)
                .GroupBy(result => result.CreatedAt.Date)
                .Select(group => new UsageTrendPointDto
                {
                    Date = group.Key,
                    Assessments = group.Select(x => x.AssessmentId).Distinct().Count(),
                    StudentsAssessed = group.Select(x => x.UserId).Distinct().Count()
                })
                .OrderBy(point => point.Date)
                .ToListAsync();
        }
        catch (PostgresException ex) when (IsMissingRelation(ex))
        {
            _log.Warn("SuperAdminOrchestrator.GetUsageTrends: assessment_results table is missing. Returning empty trend data.", ex);
            return [];
        }
    }

    private static bool IsMissingRelation(PostgresException ex) => ex.SqlState == PostgresErrorCodes.UndefinedTable;
}
