using log4net;
using Microsoft.EntityFrameworkCore;
using Taskverse.Api.MicroServices.Interfaces;
using Taskverse.Api.MicroServices.Models;
using Taskverse.Business.DTOs;
using Taskverse.Data.Enums;
using Taskverse.Business.Interface;
using Taskverse.Business.Mappings;
using Taskverse.Business.Utilities;
using Taskverse.Data.DataAccess;

namespace Taskverse.Business.Orchestrators;

public class SuperAdminOrchestrator : ISuperAdminOrchestrator
{
    private const string ActiveCollegeStatus = "Active";

    private readonly IMicroServiceOrchestrator _microServiceOrchestrator;
    private readonly IDbContextFactory<TaskverseContext> _dbContextFactory;
    private readonly IBulkStudentUploadService _bulkStudentUploadService;
    private static readonly ILog _log = LogManager.GetLogger(typeof(SuperAdminOrchestrator));

    public SuperAdminOrchestrator(
        IMicroServiceOrchestrator microServiceOrchestrator,
        IDbContextFactory<TaskverseContext> dbContextFactory,
        IBulkStudentUploadService bulkStudentUploadService)
    {
        _microServiceOrchestrator = microServiceOrchestrator;
        _dbContextFactory = dbContextFactory;
        _bulkStudentUploadService = bulkStudentUploadService;
    }

    public async Task<SuperAdminDashboardDto> GetDashboard()
    {
        _log.Debug("SuperAdminOrchestrator.GetDashboard");

        try
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();

            var pendingApprovalsCount = await context.Users
                .CountAsync(u => u.Role == "CollegeAdmin" && u.Status == UserStatus.PENDING_APPROVAL);

            var activeCollegesCount = await context.Users
                .Where(u => u.Role == "CollegeAdmin" && u.Status == UserStatus.APPROVED && u.CollegeName != null)
                .Select(u => u.CollegeName)
                .Distinct()
                .CountAsync();

            var registeredStudentsCount = await context.Students.CountAsync();

            var assessmentsThisMonthCount = await context.Assessments
                .CountAsync(a => a.AssessmentStatus == AssessmentStatus.Completed);

            _log.Debug(
                $"GetDashboard: pendingApprovals={pendingApprovalsCount}, activeColleges={activeCollegesCount}, registeredStudents={registeredStudentsCount}, assessmentsThisMonth={assessmentsThisMonthCount}");

            return new SuperAdminDashboardDto
            {
                Totals = new SuperAdminTotalsDto
                {
                    PendingApprovals = pendingApprovalsCount,
                    ActiveColleges = activeCollegesCount,
                    RegisteredStudents = registeredStudentsCount,
                    AssessmentsThisMonth = assessmentsThisMonthCount
                }
            };
        }
        catch (Exception ex)
        {
            _log.Error("GetDashboard failed", ex);
            throw;
        }
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

    public Task<BulkStudentUploadResultDto> BulkUploadStudents(BulkStudentUploadRequestDto dto) =>
        _bulkStudentUploadService.UploadAsync(dto);

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

        await EnsureUserCollegeName(context, user);

        var existingStudent = await context.Students
            .FirstOrDefaultAsync(student => student.UserId == user.Id);

        if (existingStudent is not null)
        {
            existingStudent.ClassId = user.ClassId;
            existingStudent.BatchId = user.BatchId;
            existingStudent.EnrollmentNumber = string.IsNullOrWhiteSpace(user.EnrollmentNumber) ? null : user.EnrollmentNumber.Trim();
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
            BatchId = user.BatchId,
            EnrollmentNumber = string.IsNullOrWhiteSpace(user.EnrollmentNumber) ? null : user.EnrollmentNumber.Trim(),
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

        await EnsureUserCollegeName(context, user);

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

    private static async Task EnsureUserCollegeName(TaskverseContext context, User user)
    {
        if (!user.CollegeId.HasValue)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(user.CollegeName))
        {
            user.CollegeName = user.CollegeName.Trim();
            return;
        }

        var collegeName = await context.Colleges
            .AsNoTracking()
            .Where(college => college.CollegeId == user.CollegeId.Value)
            .Select(college => college.CollegeName)
            .FirstOrDefaultAsync();

        user.CollegeName = string.IsNullOrWhiteSpace(collegeName)
            ? null
            : collegeName.Trim();
    }


}
