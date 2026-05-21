using log4net;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
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

public class CollegeAdminOrchestrator : ICollegeAdminOrchestrator
{
    private static readonly ILog _log = LogManager.GetLogger(typeof(CollegeAdminOrchestrator));

    private readonly IMicroServiceOrchestrator _microServiceOrchestrator;
    private readonly IDbContextFactory<TaskverseContext> _dbContextFactory;

    public CollegeAdminOrchestrator(
        IMicroServiceOrchestrator microServiceOrchestrator,
        IDbContextFactory<TaskverseContext> dbContextFactory)
    {
        _microServiceOrchestrator = microServiceOrchestrator;
        _dbContextFactory = dbContextFactory;
    }

    public async Task<CollegeAdminDashboardDto> GetDashboard(Guid collegeId)
    {
        _log.Debug($"CollegeAdminOrchestrator.GetDashboard: collegeId={collegeId}");

        var pendingUsersTask = GetPendingUsersForCollege(collegeId);
        var totalsTask = GetDashboardTotals(collegeId);
        var recentActivityTask = GetRecentActivity(collegeId);
        var usageTrendsTask = GetUsageTrends(collegeId);

        await Task.WhenAll(pendingUsersTask, totalsTask, recentActivityTask, usageTrendsTask);

        var totals = await totalsTask;
        var pendingUsers = await pendingUsersTask;
        var recentActivity = await recentActivityTask;
        var usageTrends = await usageTrendsTask;

        _log.Debug(
            $"CollegeAdminOrchestrator.GetDashboard: collegeId={collegeId}, pendingApprovals={pendingUsers.Count}, students={totals.RegisteredStudents}, trainers={totals.RegisteredTrainers}, assessmentsThisMonth={totals.AssessmentsThisMonth}, assessmentsPreviousMonth={totals.AssessmentsPreviousMonth}");

        return new CollegeAdminDashboardDto
        {
            Totals = totals,
            PendingApprovals = pendingUsers,
            RecentActivity = recentActivity,
            UsageTrends = usageTrends
        };
    }

    public async Task<ClassConfigurationDto> GetClassConfiguration(Guid collegeId)
    {
        _log.Debug($"CollegeAdminOrchestrator.GetClassConfiguration: collegeId={collegeId}");

        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var classes = await context.Classes
            .AsNoTracking()
            .Where(item => item.CollegeId == collegeId)
            .OrderByDescending(item => item.CreatedAt)
            .ThenBy(item => item.Name)
            .Select(item => new
            {
                item.ClassId,
                item.CollegeId,
                item.Name,
                item.AcademicYear,
                Department = item.Description,
                item.CreatedAt
            })
            .ToListAsync();

        var batches = await context.Batches
            .AsNoTracking()
            .Where(item => item.CollegeId == collegeId)
            .OrderBy(item => item.Name)
            .Select(item => new
            {
                item.BatchId,
                item.ClassId,
                item.CollegeId,
                item.Name,
                item.Description,
                Capacity = item.Capacity ?? 0,
                item.CreatedAt
            })
            .ToListAsync();

        var studentCountsByBatch = await context.Students
            .AsNoTracking()
            .Where(item => item.CollegeId == collegeId && item.Status == UserStatus.APPROVED)
            .GroupBy(item => item.BatchId)
            .Select(group => new
            {
                BatchId = group.Key,
                Count = group.Count()
            })
            .ToDictionaryAsync(item => item.BatchId, item => item.Count);

        var batchIds = batches.Select(item => item.BatchId).ToList();
        var assignedTrainersByBatch = await context.TrainerBatches
            .AsNoTracking()
            .Where(item => batchIds.Contains(item.BatchId) && item.Trainer.CollegeId == collegeId && item.Trainer.Status == UserStatus.APPROVED)
            .Select(item => new
            {
                item.BatchId,
                item.Trainer.TrainerId,
                item.Trainer.UserId,
                item.Trainer.FullName,
                item.Trainer.Email
            })
            .ToListAsync();

        var trainersLookup = assignedTrainersByBatch
            .GroupBy(item => item.BatchId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderBy(item => item.FullName)
                    .ThenBy(item => item.Email)
                    .Select(item => new ApprovedTrainerDto
                    {
                        TrainerId = item.TrainerId.ToString(),
                        UserId = item.UserId.ToString(),
                        FullName = item.FullName,
                        Email = item.Email
                    })
                    .ToList());

        var batchesByClass = batches
            .GroupBy(item => item.ClassId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(item => new CollegeBatchSummaryDto
                    {
                        BatchId = item.BatchId.ToString(),
                        ClassId = item.ClassId.ToString(),
                        CollegeId = item.CollegeId.ToString(),
                        Name = item.Name,
                        Description = item.Description,
                        Capacity = item.Capacity,
                        StudentCount = studentCountsByBatch.TryGetValue(item.BatchId, out var count) ? count : 0,
                        CreatedAt = item.CreatedAt,
                        AssignedTrainers = trainersLookup.TryGetValue(item.BatchId, out var trainers)
                            ? trainers
                            : []
                    })
                    .OrderBy(item => item.Name)
                    .ToList());

        var classSummaries = classes
            .Select(item =>
            {
                var classBatches = batchesByClass.TryGetValue(item.ClassId, out var foundBatches)
                    ? foundBatches
                    : [];

                return new CollegeClassSummaryDto
                {
                    ClassId = item.ClassId.ToString(),
                    CollegeId = item.CollegeId.ToString(),
                    Name = item.Name,
                    AcademicYear = item.AcademicYear,
                    Department = item.Department,
                    TotalStudents = classBatches.Sum(batch => batch.StudentCount),
                    TotalCapacity = classBatches.Sum(batch => batch.Capacity),
                    CreatedAt = item.CreatedAt,
                    Batches = classBatches
                };
            })
            .ToList();

        var totalCapacity = classSummaries.Sum(item => item.TotalCapacity);
        var totalStudents = classSummaries.Sum(item => item.TotalStudents);

        return new ClassConfigurationDto
        {
            Totals = new ClassConfigurationTotalsDto
            {
                TotalClasses = classSummaries.Count,
                TotalBatches = classSummaries.Sum(item => item.Batches.Count),
                TotalStudents = totalStudents,
                CapacityUtilization = totalCapacity <= 0
                    ? 0
                    : (int)Math.Round((double)totalStudents / totalCapacity * 100, MidpointRounding.AwayFromZero)
            },
            Classes = classSummaries
        };
    }

    public async Task<List<PendingUserDto>> GetPendingUsersForCollegeAdmin(Guid collegeAdminUserId)
    {
        _log.Debug($"CollegeAdminOrchestrator.GetPendingUsersForCollegeAdmin: collegeAdminUserId={collegeAdminUserId}");

        var result = await _microServiceOrchestrator.GetCollegeAdminPendingUsers(collegeAdminUserId.ToString());
        EnsureMicroServiceSuccess(result, nameof(GetPendingUsersForCollegeAdmin));

        var models = result.DeserializeValue<List<PendingUserModel>>()
            ?? throw new InvalidOperationException($"GetPendingUsersForCollegeAdmin returned empty for userId={collegeAdminUserId}.");

        _log.Debug(
            $"CollegeAdminOrchestrator.GetPendingUsersForCollegeAdmin: collegeAdminUserId={collegeAdminUserId}, count={models.Count}");

        return models.Select(model => model.ToDto()).ToList();
    }

    public Task<List<PendingUserDto>> GetPendingUsers(Guid collegeId)
    {
        _log.Debug($"CollegeAdminOrchestrator.GetPendingUsers: collegeId={collegeId}");
        return GetPendingUsersForCollege(collegeId);
    }

    public async Task<List<ApprovedTrainerDto>> GetApprovedTrainers(Guid collegeId)
    {
        _log.Debug($"CollegeAdminOrchestrator.GetApprovedTrainers: collegeId={collegeId}");

        var result = await _microServiceOrchestrator.GetApprovedCollegeTrainers(collegeId.ToString());
        EnsureMicroServiceSuccess(result, nameof(GetApprovedTrainers));

        var models = result.DeserializeValue<List<ApprovedTrainerModel>>()
            ?? throw new InvalidOperationException($"GetApprovedTrainers returned empty for collegeId={collegeId}.");

        return models.Select(model => model.ToDto()).ToList();
    }

    private async Task<List<PendingUserDto>> GetPendingUsersForCollege(Guid collegeId)
    {
        _log.Debug($"CollegeAdminOrchestrator.GetPendingUsersForCollege: collegeId={collegeId}");

        var result = await _microServiceOrchestrator.GetCollegePendingUsers(collegeId.ToString());
        EnsureMicroServiceSuccess(result, nameof(GetPendingUsersForCollege));

        var models = result.DeserializeValue<List<PendingUserModel>>()
            ?? throw new InvalidOperationException($"GetPendingUsersForCollege returned empty for collegeId={collegeId}.");

        _log.Debug($"CollegeAdminOrchestrator.GetPendingUsersForCollege: collegeId={collegeId}, count={models.Count}");

        return models.Select(model => model.ToDto()).ToList();
    }

    public async Task<CollegeClassSummaryDto> CreateClass(Guid collegeId, CreateCollegeClassDto dto)
    {
        _log.Debug($"CollegeAdminOrchestrator.CreateClass: collegeId={collegeId}, name={dto.Name}, academicYear={dto.AcademicYear}");

        var result = await _microServiceOrchestrator.CreateCollegeClass(collegeId.ToString(), dto.ToMicroServiceModel());
        EnsureMicroServiceSuccess(result, nameof(CreateClass));

        var model = result.DeserializeValue<CollegeClassSummaryModel>()
            ?? throw new InvalidOperationException($"CreateClass returned empty for collegeId={collegeId}.");

        return model.ToDto();
    }

    public async Task<CollegeBatchSummaryDto> CreateBatch(Guid collegeId, string classId, CreateCollegeBatchDto dto)
    {
        _log.Debug($"CollegeAdminOrchestrator.CreateBatch: collegeId={collegeId}, classId={classId}, name={dto.Name}");

        if (!Guid.TryParse(classId, out _))
        {
            throw new InvalidOperationException("Class id is invalid.");
        }

        var result = await _microServiceOrchestrator.CreateCollegeBatch(collegeId.ToString(), classId, dto.ToMicroServiceModel());
        EnsureMicroServiceSuccess(result, nameof(CreateBatch));

        var model = result.DeserializeValue<CollegeBatchSummaryModel>()
            ?? throw new InvalidOperationException($"CreateBatch returned empty for collegeId={collegeId}, classId={classId}.");

        return model.ToDto();
    }

    public async Task<CollegeBatchSummaryDto> AssignBatchTrainers(Guid collegeId, string classId, string batchId, AssignBatchTrainersDto dto)
    {
        _log.Debug($"CollegeAdminOrchestrator.AssignBatchTrainers: collegeId={collegeId}, classId={classId}, batchId={batchId}, trainerCount={dto.TrainerIds.Count}");

        if (!Guid.TryParse(classId, out _))
        {
            throw new InvalidOperationException("Class id is invalid.");
        }

        if (!Guid.TryParse(batchId, out _))
        {
            throw new InvalidOperationException("Batch id is invalid.");
        }

        var result = await _microServiceOrchestrator.AssignCollegeBatchTrainers(
            collegeId.ToString(),
            classId,
            batchId,
            dto.ToMicroServiceModel());
        EnsureMicroServiceSuccess(result, nameof(AssignBatchTrainers));

        var model = result.DeserializeValue<CollegeBatchSummaryModel>()
            ?? throw new InvalidOperationException($"AssignBatchTrainers returned empty for collegeId={collegeId}, classId={classId}, batchId={batchId}.");

        return model.ToDto();
    }

    public async Task ApproveUser(Guid collegeId, string userId, UserActionDto dto)
    {
        _log.Debug($"CollegeAdminOrchestrator.ApproveUser: collegeId={collegeId}, userId={userId}");
        var result = await _microServiceOrchestrator.ApproveCollegeUser(collegeId.ToString(), userId, dto.ToMicroServiceModel());
        EnsureMicroServiceSuccess(result, nameof(ApproveUser));
    }

    public async Task RejectUser(Guid collegeId, string userId, UserActionDto dto)
    {
        _log.Debug($"CollegeAdminOrchestrator.RejectUser: collegeId={collegeId}, userId={userId}");
        var result = await _microServiceOrchestrator.RejectCollegeUser(collegeId.ToString(), userId, dto.ToMicroServiceModel());
        EnsureMicroServiceSuccess(result, nameof(RejectUser));
    }

    private async Task<CollegeAdminTotalsDto> GetDashboardTotals(Guid collegeId)
    {
        try
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();

            var utcNow = DateTime.UtcNow;
            var startOfThisMonth = new DateTime(utcNow.Year, utcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var startOfPreviousMonth = startOfThisMonth.AddMonths(-1);

            var registeredStudentsTask = context.Students
                .AsNoTracking()
                .CountAsync(student => student.CollegeId == collegeId && student.Status == UserStatus.APPROVED);

            var registeredTrainersTask = context.Trainers
                .AsNoTracking()
                .CountAsync(trainer => trainer.CollegeId == collegeId && trainer.Status == UserStatus.APPROVED);

            var pendingApprovalsTask = context.Users
                .AsNoTracking()
                .CountAsync(user =>
                    user.CollegeId == collegeId &&
                    user.Status == UserStatus.PENDING_APPROVAL &&
                    user.Role.Trim().ToLower() != "collegeadmin" &&
                    user.Role.Trim().ToLower() != "superadmin");

            var assessmentsThisMonthTask = (
                from assessment in context.Assessments.AsNoTracking()
                join user in context.Users.AsNoTracking() on assessment.CreatedBy equals user.Id
                where user.CollegeId == collegeId && assessment.CreatedAt >= startOfThisMonth
                select assessment.AssessmentId)
                .Distinct()
                .CountAsync();

            var assessmentsPreviousMonthTask = (
                from assessment in context.Assessments.AsNoTracking()
                join user in context.Users.AsNoTracking() on assessment.CreatedBy equals user.Id
                where user.CollegeId == collegeId &&
                      assessment.CreatedAt >= startOfPreviousMonth &&
                      assessment.CreatedAt < startOfThisMonth
                select assessment.AssessmentId)
                .Distinct()
                .CountAsync();

            await Task.WhenAll(
                registeredStudentsTask,
                registeredTrainersTask,
                pendingApprovalsTask,
                assessmentsThisMonthTask,
                assessmentsPreviousMonthTask);

            return new CollegeAdminTotalsDto
            {
                RegisteredStudents = await registeredStudentsTask,
                RegisteredTrainers = await registeredTrainersTask,
                PendingApprovals = await pendingApprovalsTask,
                AssessmentsThisMonth = await assessmentsThisMonthTask,
                AssessmentsPreviousMonth = await assessmentsPreviousMonthTask
            };
        }
        catch (PostgresException ex) when (IsMissingRelation(ex))
        {
            _log.Warn($"CollegeAdminOrchestrator.GetDashboardTotals: required tables are missing for collegeId={collegeId}. Returning zero totals.", ex);
            return new CollegeAdminTotalsDto();
        }
    }

    private async Task<List<RecentActivityDto>> GetRecentActivity(Guid collegeId)
    {
        try
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();

            var recentLogs = await context.AuditLogs
                .AsNoTracking()
                .Join(
                    context.Users.AsNoTracking().Where(user => user.CollegeId == collegeId),
                    audit => audit.UserId,
                    user => user.Id,
                    (audit, user) => new { audit, user.FullName })
                .OrderByDescending(x => x.audit.OccurredAt)
                .Take(20)
                .ToListAsync();

            return recentLogs.Select(x => x.audit.ToDto(x.FullName)).ToList();
        }
        catch (PostgresException ex) when (IsMissingRelation(ex))
        {
            _log.Warn($"CollegeAdminOrchestrator.GetRecentActivity: required audit tables are missing for collegeId={collegeId}. Returning empty activity list.", ex);
            return [];
        }
    }

    private async Task<List<UsageTrendPointDto>> GetUsageTrends(Guid collegeId)
    {
        try
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();

            var utcToday = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
            var rangeStart = utcToday.AddDays(-29);

            var trends = await context.Results
                .AsNoTracking()
                .Join(
                    context.Students.AsNoTracking().Where(student => student.CollegeId == collegeId),
                    result => result.StudentId,
                    student => student.StudentId,
                    (result, _) => result)
                .Where(result => result.GeneratedAt >= rangeStart)
                .GroupBy(result => result.GeneratedAt.Date)
                .Select(group => new UsageTrendPointDto
                {
                    Date = group.Key,
                    Assessments = group.Select(x => x.AssessmentId).Distinct().Count(),
                    StudentsAssessed = group.Select(x => x.StudentId).Distinct().Count()
                })
                .OrderBy(point => point.Date)
                .ToListAsync();

            return trends;
        }
        catch (PostgresException ex) when (IsMissingRelation(ex))
        {
            _log.Warn($"CollegeAdminOrchestrator.GetUsageTrends: assessment tables are missing for collegeId={collegeId}. Returning empty trend data.", ex);
            return [];
        }
    }

    private static string NormalizeRole(string role) =>
        (role ?? string.Empty).Trim().Replace(" ", string.Empty).ToLowerInvariant();

    private static bool IsMissingRelation(PostgresException ex) => ex.SqlState == PostgresErrorCodes.UndefinedTable;

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
