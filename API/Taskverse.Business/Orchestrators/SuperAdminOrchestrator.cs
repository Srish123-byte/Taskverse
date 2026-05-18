using log4net;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Taskverse.Api.MicroServices.Interfaces;
using Taskverse.Api.MicroServices.Models;
using Taskverse.Business.DTOs;
using Taskverse.Business.Enums;
using Taskverse.Business.Interface;
using Taskverse.Business.Mappings;
using Taskverse.Business.Utilities;
using Taskverse.Data.DataAccess;

namespace Taskverse.Business.Orchestrators;

public class SuperAdminOrchestrator : ISuperAdminOrchestrator
{
    private const string HealthyStatus = "Healthy";
    private const string ActiveCollegeStatus = "Active";

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
        var pendingApprovals = await pendingTask;

        _log.Debug(
            $"SuperAdminOrchestrator.GetDashboard: colleges={colleges.Count}, activeColleges={colleges.Count(c => c.IsActive)}, pendingApprovals={pendingApprovals.Count}, assessmentsThisMonth={totals.ThisMonth}, assessmentsPreviousMonth={totals.PreviousMonth}");

        return new SuperAdminDashboardDto
        {
            Totals = new SuperAdminTotalsDto
            {
                ActiveColleges = colleges.Count(c => c.IsActive),
                RegisteredStudents = 0,
                AssessmentsThisMonth = totals.ThisMonth,
                AssessmentsPreviousMonth = totals.PreviousMonth
            },
            PendingApprovals = pendingApprovals,
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

        _log.Debug($"SuperAdminOrchestrator.GetColleges: received {models.Count} colleges from college service");

        return models.Select(c => c.ToDto()).ToList();
    }

    public async Task<List<CollegeSearchResultDto>> SearchColleges(CollegeSearchDto dto)
    {
        _log.Debug($"SuperAdminOrchestrator.SearchColleges: query={dto.Query}, status={dto.Status}");
        var result = await _microServiceOrchestrator.SearchColleges(dto.ToMicroServiceModel());
        result.EnsureSuccess(nameof(SearchColleges));

        var models = result.DeserializeValue<List<CollegeSearchResultModel>>()
            ?? throw new InvalidOperationException("SearchColleges returned empty.");

        _log.Debug($"SuperAdminOrchestrator.SearchColleges: received {models.Count} search results");

        return models.Select(model => model.ToDto()).ToList();
    }

    public async Task<List<CollegeDto>> GetPendingColleges()
    {
        _log.Debug("SuperAdminOrchestrator.GetPendingColleges");
        var result = await _microServiceOrchestrator.GetPendingColleges();
        result.EnsureSuccess(nameof(GetPendingColleges));

        var models = result.DeserializeValue<List<CollegeModel>>()
            ?? throw new InvalidOperationException("GetPendingColleges returned empty.");

        _log.Debug($"SuperAdminOrchestrator.GetPendingColleges: received {models.Count} pending colleges");

        return models.Select(c => c.ToDto()).ToList();
    }

    public async Task<List<PendingUserDto>> GetPendingUsers()
    {
        _log.Debug("SuperAdminOrchestrator.GetPendingUsers");
        var result = await _microServiceOrchestrator.GetPendingUsers();
        result.EnsureSuccess(nameof(GetPendingUsers));

        var models = result.DeserializeValue<List<PendingUserModel>>()
            ?? throw new InvalidOperationException("GetPendingUsers returned empty.");

        _log.Debug($"SuperAdminOrchestrator.GetPendingUsers: received {models.Count} pending users");

        return models.Select(user => user.ToDto()).ToList();
    }

    public async Task<PagedUsersResultDto> SearchUsers(UserSearchCriteriaDto dto)
    {
        _log.Debug($"SuperAdminOrchestrator.SearchUsers: status={dto.Status}, role={dto.Role}, searchTerm={dto.SearchTerm}, page={dto.PageNumber}, pageSize={dto.PageSize}");
        var result = await _microServiceOrchestrator.SearchUsers(dto.ToMicroServiceModel());
        result.EnsureSuccess(nameof(SearchUsers));

        var model = result.DeserializeValue<PagedPendingUserResultModel>()
            ?? throw new InvalidOperationException("SearchUsers returned empty.");

        _log.Debug($"SuperAdminOrchestrator.SearchUsers: received {model.Items.Count} users, totalCount={model.TotalCount}");

        return model.ToDto();
    }

    public async Task<CollegeDto> ApproveCollege(string collegeId, CollegeActionDto dto) =>
        await ExecuteCollegeAction(nameof(ApproveCollege), collegeId, dto, _microServiceOrchestrator.ApproveCollege);

    public async Task<CollegeDto> RejectCollege(string collegeId, CollegeActionDto dto) =>
        await ExecuteCollegeAction(nameof(RejectCollege), collegeId, dto, _microServiceOrchestrator.RejectCollege);

    public async Task<CollegeDto> DeactivateCollege(string collegeId, CollegeActionDto dto) =>
        await ExecuteCollegeAction(nameof(DeactivateCollege), collegeId, dto, _microServiceOrchestrator.DeactivateCollege);

    public async Task<CollegeDto> ReactivateCollege(string collegeId, CollegeActionDto dto) =>
        await ExecuteCollegeAction(nameof(ReactivateCollege), collegeId, dto, _microServiceOrchestrator.ReactivateCollege);

    public async Task ApproveUser(string userId, UserActionDto dto)
    {
        _log.Debug($"SuperAdminOrchestrator.ApproveUser: userId={userId}");

        _log.Debug($"SuperAdminOrchestrator.ApproveUser: opening db context and transaction for userId={userId}");
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();

        var user = await GetPendingUserEntity(context, userId);
        if (user is null)
        {
            throw new KeyNotFoundException($"Pending user not found for userId={userId}.");
        }

        var normalizedRole = NormalizeRole(user.Role);
        _log.Debug(
            $"SuperAdminOrchestrator.ApproveUser: loaded pending user entity id={user.Id}, role={normalizedRole}, collegeId={user.CollegeId}, classId={user.ClassId}, batchId={user.BatchId}");

        switch (normalizedRole)
        {
            case "collegeadmin":
                await EnsureCollegeAdminApprovalRecord(context, user);
                break;
            case "student":
                await EnsureStudentApprovalRecord(context, user, dto.PerformedByUserId);
                break;
            case "trainer":
                await EnsureTrainerApprovalRecord(context, user, dto.PerformedByUserId);
                break;
            default:
                throw new InvalidOperationException($"Unsupported pending user role '{user.Role}' for approval.");
        }

        user.Status = UserStatus.APPROVED;
        user.ModifiedAt = DateTime.UtcNow;

        var affectedRows = await context.SaveChangesAsync();
        _log.Debug($"SuperAdminOrchestrator.ApproveUser: persisted approval changes for userId={userId}, affectedRows={affectedRows}");
        await transaction.CommitAsync();
        _log.Debug($"SuperAdminOrchestrator.ApproveUser: committed transaction for userId={userId}");
    }

    public async Task RejectUser(string userId, UserActionDto _)
    {
        _log.Debug($"SuperAdminOrchestrator.RejectUser: userId={userId}");

        _log.Debug($"SuperAdminOrchestrator.RejectUser: opening db context for userId={userId}");
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var user = await GetPendingUserEntity(context, userId);
        if (user is null)
        {
            throw new KeyNotFoundException($"Pending user not found for userId={userId}.");
        }

        user.Status = UserStatus.REJECTED;
        user.ModifiedAt = DateTime.UtcNow;

        var affectedRows = await context.SaveChangesAsync();
        _log.Debug($"SuperAdminOrchestrator.RejectUser: persisted rejection for userId={userId}, affectedRows={affectedRows}");
    }

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

        _log.Debug(
            $"SuperAdminOrchestrator.{operationName}: completed for collegeId={collegeId}, status={model.Status}, isActive={model.IsActive}");

        return model.ToDto();
    }

    private static async Task<User?> GetPendingUserEntity(TaskverseContext context, string userId)
    {
        if (!Guid.TryParse(userId, out var parsedUserId))
        {
            return null;
        }

        return await context.Users.FirstOrDefaultAsync(user =>
            user.Id == parsedUserId && user.Status == UserStatus.PENDING_APPROVAL);
    }

    private static string NormalizeRole(string role) =>
        (role ?? string.Empty).Trim().Replace(" ", string.Empty).ToLowerInvariant();

    private static async Task EnsureCollegeAdminApprovalRecord(TaskverseContext context, User user)
    {
        var adminName = string.IsNullOrWhiteSpace(user.FullName)
            ? null
            : user.FullName.Trim();
        var collegeName = string.IsNullOrWhiteSpace(user.CollegeName)
            ? null
            : user.CollegeName.Trim();

        if (string.IsNullOrWhiteSpace(adminName))
        {
            throw new InvalidOperationException($"College admin user '{user.Id}' cannot be approved without an admin name.");
        }

        if (string.IsNullOrWhiteSpace(collegeName))
        {
            throw new InvalidOperationException($"College admin user '{user.Id}' cannot be approved without a college name.");
        }

        if (user.CollegeId.HasValue)
        {
            var existingCollege = await context.Colleges
                .FirstOrDefaultAsync(college => college.CollegeId == user.CollegeId.Value);

            if (existingCollege is not null)
            {
                existingCollege.AdminName = adminName;
                existingCollege.CollegeName = collegeName;
                existingCollege.ModifiedAt = DateTime.UtcNow;
                user.CollegeId = existingCollege.CollegeId;
                return;
            }
        }

        var college = new College
        {
            CollegeId = user.CollegeId ?? Guid.NewGuid(),
            CollegeName = collegeName,
            AdminName = adminName,
            Status = ActiveCollegeStatus,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };

        context.Colleges.Add(college);
        user.CollegeId = college.CollegeId;
    }

    private static async Task EnsureStudentApprovalRecord(TaskverseContext context, User user, Guid? approvedByUserId)
    {
        if (!user.CollegeId.HasValue)
        {
            throw new InvalidOperationException($"Student user '{user.Id}' cannot be approved without a college.");
        }

        if (!user.BatchId.HasValue)
        {
            throw new InvalidOperationException($"Student user '{user.Id}' cannot be approved without a batch.");
        }

        var existingStudent = await context.Students
            .FirstOrDefaultAsync(student => student.UserId == user.Id);

        if (existingStudent is not null)
        {
            existingStudent.ClassId = user.ClassId;
            existingStudent.Status = UserStatus.APPROVED;
            existingStudent.ModifiedAt = DateTime.UtcNow;
            existingStudent.ApprovedBy = approvedByUserId;
            return;
        }

        context.Students.Add(new Student
        {
            StudentId = Guid.NewGuid(),
            UserId = user.Id,
            CollegeId = user.CollegeId.Value,
            ClassId = user.ClassId,
            BatchId = user.BatchId.Value,
            FullName = user.FullName,
            Email = user.Email,
            Status = UserStatus.APPROVED,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow,
            ApprovedBy = approvedByUserId
        });
    }

    private static async Task EnsureTrainerApprovalRecord(TaskverseContext context, User user, Guid? approvedByUserId)
    {
        if (!user.CollegeId.HasValue)
        {
            throw new InvalidOperationException($"Trainer user '{user.Id}' cannot be approved without a college.");
        }

        var existingTrainer = await context.Trainers
            .FirstOrDefaultAsync(trainer => trainer.UserId == user.Id);

        if (existingTrainer is not null)
        {
            existingTrainer.Status = UserStatus.APPROVED;
            existingTrainer.ModifiedAt = DateTime.UtcNow;
            existingTrainer.ApprovedBy = approvedByUserId;
            return;
        }

        context.Trainers.Add(new Trainer
        {
            TrainerId = Guid.NewGuid(),
            UserId = user.Id,
            CollegeId = user.CollegeId.Value,
            FullName = user.FullName,
            Email = user.Email,
            Status = UserStatus.APPROVED,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow,
            ApprovedBy = approvedByUserId
        });
    }

    private async Task<(int ThisMonth, int PreviousMonth)> GetAssessmentTotals()
    {
        try
        {
            _log.Debug("SuperAdminOrchestrator.GetAssessmentTotals: opening db context");
            await using var context = await _dbContextFactory.CreateDbContextAsync();

            var utcNow = DateTime.UtcNow;
            var startOfThisMonth = new DateTime(utcNow.Year, utcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var startOfPreviousMonth = startOfThisMonth.AddMonths(-1);

            var thisMonth = await context.Assessments.CountAsync(a => a.CreatedAt >= startOfThisMonth);
            var previousMonth = await context.Assessments.CountAsync(a =>
                a.CreatedAt >= startOfPreviousMonth && a.CreatedAt < startOfThisMonth);

            _log.Debug(
                $"SuperAdminOrchestrator.GetAssessmentTotals: thisMonth={thisMonth}, previousMonth={previousMonth}, startOfThisMonth={startOfThisMonth:O}, startOfPreviousMonth={startOfPreviousMonth:O}");

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
            _log.Debug("SuperAdminOrchestrator.GetRecentActivity: opening db context");
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

            _log.Debug($"SuperAdminOrchestrator.GetRecentActivity: fetched {recentLogs.Count} activity rows");

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
            _log.Debug("SuperAdminOrchestrator.GetAverageScoresByCollege: opening db context");
            await using var context = await _dbContextFactory.CreateDbContextAsync();

            var scores = await context.AssessmentResults
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
                .GroupBy(x => new { x.college.CollegeId, CollegeName = x.college.CollegeName ?? "Unknown College" })
                .Select(group => new CollegeScoreSummaryDto
                {
                    CollegeId = group.Key.CollegeId.ToString(),
                    CollegeName = group.Key.CollegeName,
                    AverageScore = Math.Round(group.Average(x => x.result.Score ?? 0), 2),
                    StudentsAssessed = group.Select(x => x.user.Id).Distinct().Count()
                })
                .OrderByDescending(x => x.AverageScore)
                .Take(10)
                .ToListAsync();

            _log.Debug($"SuperAdminOrchestrator.GetAverageScoresByCollege: computed {scores.Count} college score summaries");
            return scores;
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
            _log.Debug("SuperAdminOrchestrator.GetUsageTrends: opening db context");
            await using var context = await _dbContextFactory.CreateDbContextAsync();

            var utcToday = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
            var rangeStart = utcToday.AddDays(-29);

            var trends = await context.AssessmentResults
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

            _log.Debug($"SuperAdminOrchestrator.GetUsageTrends: computed {trends.Count} trend points from rangeStart={rangeStart:O}");
            return trends;
        }
        catch (PostgresException ex) when (IsMissingRelation(ex))
        {
            _log.Warn("SuperAdminOrchestrator.GetUsageTrends: assessment_results table is missing. Returning empty trend data.", ex);
            return [];
        }
    }

    private static bool IsMissingRelation(PostgresException ex) => ex.SqlState == PostgresErrorCodes.UndefinedTable;
}
