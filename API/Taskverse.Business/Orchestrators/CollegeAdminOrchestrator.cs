using log4net;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Npgsql;
using Taskverse.Api.MicroServices.Interfaces;
using Taskverse.Api.MicroServices.Models;
using Taskverse.Business.DTOs;
using Taskverse.Data.Enums;
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
    private readonly IBulkStudentUploadService _bulkStudentUploadService;

    public CollegeAdminOrchestrator(
        IMicroServiceOrchestrator microServiceOrchestrator,
        IDbContextFactory<TaskverseContext> dbContextFactory,
        IBulkStudentUploadService bulkStudentUploadService)
    {
        _microServiceOrchestrator = microServiceOrchestrator;
        _dbContextFactory = dbContextFactory;
        _bulkStudentUploadService = bulkStudentUploadService;
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

        var classesResult = await _microServiceOrchestrator.GetRegistrationClasses(collegeId.ToString());
        EnsureMicroServiceSuccess(classesResult, nameof(GetClassConfiguration));

        var registrationClasses = classesResult.DeserializeValue<List<RegistrationClassModel>>()
            ?? throw new InvalidOperationException($"GetClassConfiguration returned empty classes for collegeId={collegeId}.");

        var batchTasks = registrationClasses.ToDictionary(
            item => item.ClassId,
            item => _microServiceOrchestrator.GetRegistrationBatches(item.ClassId));

        await Task.WhenAll(batchTasks.Values);

        var registrationBatches = new List<RegistrationBatchModel>();
        foreach (var entry in batchTasks)
        {
            EnsureMicroServiceSuccess(entry.Value.Result, nameof(GetClassConfiguration));

            var batches = entry.Value.Result.DeserializeValue<List<RegistrationBatchModel>>()
                ?? throw new InvalidOperationException($"GetClassConfiguration returned empty batches for classId={entry.Key}.");

            registrationBatches.AddRange(batches);
        }

        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var classIds = registrationClasses
            .Select(item => Guid.TryParse(item.ClassId, out var parsedId) ? parsedId : Guid.Empty)
            .Where(item => item != Guid.Empty)
            .ToList();

        var batchIds = registrationBatches
            .Select(item => Guid.TryParse(item.BatchId, out var parsedId) ? parsedId : Guid.Empty)
            .Where(item => item != Guid.Empty)
            .ToList();

        var classMetadata = await context.Classes
            .AsNoTracking()
            .Where(item => classIds.Contains(item.ClassId))
            .Select(item => new
            {
                item.ClassId,
                item.AcademicYear,
                Department = item.Description,
                item.CreatedAt
            })
            .ToDictionaryAsync(item => item.ClassId);

        var batchMetadata = await context.Batches
            .AsNoTracking()
            .Where(item => batchIds.Contains(item.BatchId))
            .Select(item => new
            {
                item.BatchId,
                item.Description,
                Capacity = item.Capacity ?? 0,
                item.CreatedAt
            })
            .ToDictionaryAsync(item => item.BatchId);

        var subjectsByBatch = await context.SubjectBatches
            .AsNoTracking()
            .Where(item => batchIds.Contains(item.BatchId))
            .Select(item => new
            {
                item.BatchId,
                item.SubjectId,
                item.Subject.SubjectName
            })
            .ToListAsync();

        var studentCountsByBatch = await context.Students
            .AsNoTracking()
            .Where(item => item.CollegeId == collegeId && item.Status == UserStatus.APPROVED && item.BatchId.HasValue)
            .GroupBy(item => item.BatchId!.Value)
            .Select(group => new
            {
                BatchId = group.Key,
                Count = group.Count()
            })
            .ToDictionaryAsync(item => item.BatchId, item => item.Count);

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

        var subjectsLookup = subjectsByBatch
            .GroupBy(item => item.BatchId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderBy(item => item.SubjectName)
                    .Select(item => new
                    {
                        item.SubjectId,
                        item.SubjectName
                    })
                    .First());

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

        var assignedStudentsByBatch = await context.Students
            .AsNoTracking()
            .Where(item => item.CollegeId == collegeId && item.Status == UserStatus.APPROVED && item.BatchId.HasValue)
            .Select(item => new
            {
                BatchId = item.BatchId!.Value,
                item.StudentId,
                item.UserId,
                item.FullName,
                item.Email
            })
            .ToListAsync();

        var studentsLookup = assignedStudentsByBatch
            .GroupBy(item => item.BatchId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderBy(item => item.FullName)
                    .ThenBy(item => item.Email)
                    .Select(item => new ApprovedStudentDto
                    {
                        StudentId = item.StudentId.ToString(),
                        UserId = item.UserId.ToString(),
                        FullName = item.FullName,
                        Email = item.Email
                    })
                    .ToList());

        var batchesByClass = registrationBatches
            .Where(item =>
                Guid.TryParse(item.ClassId, out var parsedClassId) &&
                Guid.TryParse(item.BatchId, out var parsedBatchId) &&
                parsedClassId != Guid.Empty &&
                parsedBatchId != Guid.Empty)
            .GroupBy(item => Guid.Parse(item.ClassId))
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(item => new CollegeBatchSummaryDto
                    {
                        BatchId = item.BatchId,
                        ClassId = item.ClassId,
                        CollegeId = item.CollegeId,
                        Name = item.Name,
                        Description = Guid.TryParse(item.BatchId, out var batchId) &&
                                      batchMetadata.TryGetValue(batchId, out var batchInfo)
                            ? batchInfo.Description
                            : null,
                        SubjectId = Guid.TryParse(item.BatchId, out var subjectBatchId) &&
                                    subjectsLookup.TryGetValue(subjectBatchId, out var subject)
                            ? subject.SubjectId.ToString()
                            : null,
                        SubjectName = Guid.TryParse(item.BatchId, out var subjectBatchNameId) &&
                                      subjectsLookup.TryGetValue(subjectBatchNameId, out var subjectInfo)
                            ? subjectInfo.SubjectName
                            : null,
                        Capacity = Guid.TryParse(item.BatchId, out var capacityBatchId) &&
                                   batchMetadata.TryGetValue(capacityBatchId, out var capacityBatch)
                            ? capacityBatch.Capacity
                            : 0,
                        StudentCount = Guid.TryParse(item.BatchId, out var countBatchId) &&
                                       studentCountsByBatch.TryGetValue(countBatchId, out var count)
                            ? count
                            : 0,
                        CreatedAt = Guid.TryParse(item.BatchId, out var createdBatchId) &&
                                    batchMetadata.TryGetValue(createdBatchId, out var createdBatch)
                            ? createdBatch.CreatedAt
                            : DateTime.UtcNow,
                        AssignedTrainers = Guid.TryParse(item.BatchId, out var trainerBatchId) &&
                                           trainersLookup.TryGetValue(trainerBatchId, out var trainers)
                            ? trainers
                            : [],
                        AssignedStudents = Guid.TryParse(item.BatchId, out var studentBatchId) &&
                                           studentsLookup.TryGetValue(studentBatchId, out var students)
                            ? students
                            : []
                    })
                    .OrderBy(item => item.Name)
                    .ToList());

        var classSummaries = registrationClasses
            .Select(item =>
            {
                if (!Guid.TryParse(item.ClassId, out var classId))
                {
                    return null;
                }

                var classBatches = batchesByClass.TryGetValue(classId, out var foundBatches)
                    ? foundBatches
                    : [];

                return new CollegeClassSummaryDto
                {
                    ClassId = item.ClassId,
                    CollegeId = item.CollegeId,
                    Name = item.Name,
                    AcademicYear = classMetadata.TryGetValue(classId, out var classInfo)
                        ? classInfo.AcademicYear ?? item.AcademicYear
                        : item.AcademicYear,
                    Department = classMetadata.TryGetValue(classId, out var departmentInfo)
                        ? departmentInfo.Department
                        : null,
                    TotalStudents = classBatches.Sum(batch => batch.StudentCount),
                    TotalCapacity = classBatches.Sum(batch => batch.Capacity),
                    CreatedAt = classMetadata.TryGetValue(classId, out var createdClassInfo)
                        ? createdClassInfo.CreatedAt
                        : DateTime.UtcNow,
                    Batches = classBatches
                };
            })
            .Where(item => item is not null)
            .Select(item => item!)
            .OrderByDescending(item => item.CreatedAt)
            .ThenBy(item => item.Name)
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

    public async Task<List<ApprovedStudentDto>> GetApprovedUnassignedStudents(Guid collegeId)
    {
        _log.Debug($"CollegeAdminOrchestrator.GetApprovedUnassignedStudents: collegeId={collegeId}");

        var result = await _microServiceOrchestrator.GetApprovedUnassignedCollegeStudents(collegeId.ToString());
        EnsureMicroServiceSuccess(result, nameof(GetApprovedUnassignedStudents));

        var models = result.DeserializeValue<List<ApprovedStudentModel>>()
            ?? throw new InvalidOperationException($"GetApprovedUnassignedStudents returned empty for collegeId={collegeId}.");

        return models.Select(model => model.ToDto()).ToList();
    }

    public async Task<List<SubjectOptionDto>> GetSubjects()
    {
        _log.Debug("CollegeAdminOrchestrator.GetSubjects");

        var result = await _microServiceOrchestrator.GetCollegeSubjects();
        EnsureMicroServiceSuccess(result, nameof(GetSubjects));

        var models = result.DeserializeValue<List<SubjectOptionModel>>()
            ?? throw new InvalidOperationException("GetSubjects returned empty.");

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

    public async Task<CollegeClassSummaryDto> UpdateClass(Guid collegeId, string classId, UpdateCollegeClassDto dto)
    {
        _log.Debug($"CollegeAdminOrchestrator.UpdateClass: collegeId={collegeId}, classId={classId}, name={dto.Name}, academicYear={dto.AcademicYear}");

        if (!Guid.TryParse(classId, out _))
        {
            throw new InvalidOperationException("Class id is invalid.");
        }

        var result = await _microServiceOrchestrator.UpdateCollegeClass(collegeId.ToString(), classId, dto.ToMicroServiceModel());
        EnsureMicroServiceSuccess(result, nameof(UpdateClass));

        var model = result.DeserializeValue<CollegeClassSummaryModel>()
            ?? throw new InvalidOperationException($"UpdateClass returned empty for collegeId={collegeId}, classId={classId}.");

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

    public async Task<CollegeBatchSummaryDto> UpdateBatch(Guid collegeId, string classId, string batchId, UpdateCollegeBatchDto dto)
    {
        _log.Debug($"CollegeAdminOrchestrator.UpdateBatch: collegeId={collegeId}, classId={classId}, batchId={batchId}, name={dto.Name}");

        if (!Guid.TryParse(classId, out _))
        {
            throw new InvalidOperationException("Class id is invalid.");
        }

        if (!Guid.TryParse(batchId, out _))
        {
            throw new InvalidOperationException("Batch id is invalid.");
        }

        var result = await _microServiceOrchestrator.UpdateCollegeBatch(
            collegeId.ToString(),
            classId,
            batchId,
            dto.ToMicroServiceModel());
        EnsureMicroServiceSuccess(result, nameof(UpdateBatch));

        var model = result.DeserializeValue<CollegeBatchSummaryModel>()
            ?? throw new InvalidOperationException($"UpdateBatch returned empty for collegeId={collegeId}, classId={classId}, batchId={batchId}.");

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

    public async Task<CollegeBatchSummaryDto> AssignStudentToBatch(Guid collegeId, string classId, string batchId, AssignStudentToBatchDto dto)
    {
        _log.Debug(
            $"CollegeAdminOrchestrator.AssignStudentToBatch: collegeId={collegeId}, classId={classId}, batchId={batchId}, studentCount={dto.StudentIds.Count}");

        if (!Guid.TryParse(classId, out _))
        {
            throw new InvalidOperationException("Class id is invalid.");
        }

        if (!Guid.TryParse(batchId, out _))
        {
            throw new InvalidOperationException("Batch id is invalid.");
        }

        var result = await _microServiceOrchestrator.AssignCollegeBatchStudent(
            collegeId.ToString(),
            classId,
            batchId,
            dto.ToMicroServiceModel());
        EnsureMicroServiceSuccess(result, nameof(AssignStudentToBatch));

        var model = result.DeserializeValue<CollegeBatchSummaryModel>()
            ?? throw new InvalidOperationException($"AssignStudentToBatch returned empty for collegeId={collegeId}, classId={classId}, batchId={batchId}.");

        return model.ToDto();
    }

    public async Task DeleteClass(Guid collegeId, string classId)
    {
        _log.Debug($"CollegeAdminOrchestrator.DeleteClass: collegeId={collegeId}, classId={classId}");

        if (!Guid.TryParse(classId, out _))
        {
            throw new InvalidOperationException("Class id is invalid.");
        }

        var result = await _microServiceOrchestrator.DeleteCollegeClass(collegeId.ToString(), classId);
        EnsureMicroServiceSuccess(result, nameof(DeleteClass));
    }

    public async Task DeleteBatch(Guid collegeId, string classId, string batchId)
    {
        _log.Debug($"CollegeAdminOrchestrator.DeleteBatch: collegeId={collegeId}, classId={classId}, batchId={batchId}");

        if (!Guid.TryParse(classId, out _))
        {
            throw new InvalidOperationException("Class id is invalid.");
        }

        if (!Guid.TryParse(batchId, out _))
        {
            throw new InvalidOperationException("Batch id is invalid.");
        }

        var result = await _microServiceOrchestrator.DeleteCollegeBatch(collegeId.ToString(), classId, batchId);
        EnsureMicroServiceSuccess(result, nameof(DeleteBatch));
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

    public Task<BulkStudentUploadResultDto> BulkUploadStudents(Guid collegeId, BulkStudentUploadRequestDto dto)
    {
        dto.RestrictedCollegeId = collegeId;
        return _bulkStudentUploadService.UploadAsync(dto);
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

            var assessmentsThisMonthTask = context.Assessments
                .AsNoTracking()
                .CountAsync(assessment =>
                    assessment.CollegeId == collegeId &&
                    assessment.CreatedAt >= startOfThisMonth);

            var assessmentsPreviousMonthTask = context.Assessments
                .AsNoTracking()
                .CountAsync(assessment =>
                    assessment.CollegeId == collegeId &&
                    assessment.CreatedAt >= startOfPreviousMonth &&
                    assessment.CreatedAt < startOfThisMonth);

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
