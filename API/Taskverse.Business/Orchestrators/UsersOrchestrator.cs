using log4net;
using Microsoft.AspNetCore.Identity;
using Taskverse.Api.MicroServices.Interfaces;
using Taskverse.Api.MicroServices.Models;
using Taskverse.Business.DTOs;
using Taskverse.Data.Enums;
using Taskverse.Business.Interface;
using Taskverse.Business.Mappings;
using Taskverse.Business.Utilities;
using Taskverse.Data.DataAccess;

namespace Taskverse.Business.Orchestrators;

public class UsersOrchestrator : IUsersOrchestrator
{
    private const string SuperAdminRole = "SuperAdmin";
    private const int EnrollmentNumberMaxLength = 50;

    private readonly IMicroServiceOrchestrator _microServiceOrchestrator;
    private readonly IUsersManager _usersManager;
    private static readonly ILog _log = LogManager.GetLogger(typeof(UsersOrchestrator));

    public UsersOrchestrator(
        IMicroServiceOrchestrator microServiceOrchestrator,
        IUsersManager usersManager)
    {
        _microServiceOrchestrator = microServiceOrchestrator;
        _usersManager = usersManager ?? throw new ArgumentNullException(nameof(usersManager));
    }

    public async Task<UserDto?> GetUser(string userId)
    {
        _log.Debug($"UsersOrchestrator.GetUser: userId={userId}");
        var result = await _microServiceOrchestrator.GetUser(userId);
        result.EnsureSuccess(nameof(GetUser));
        UserModel model = result.DeserializeValue<UserModel>()
            ?? throw new InvalidOperationException($"GetUser returned empty for userId={userId}.");
        return model.ToDto();
    }

    public async Task<PagedUserDto?> SearchUsers(string? email, string? role, bool? isActive, int pageNumber, int pageSize)
    {
        _log.Debug($"UsersOrchestrator.SearchUsers: email={email}, role={role}");
        var criteria = new UserSearchCriteriaModel(
            Status:     null,
            Role:       role,
            SearchTerm: email,
            PageNumber: pageNumber,
            PageSize:   pageSize);
        var result = await _microServiceOrchestrator.SearchUsers(criteria);
        result.EnsureSuccess(nameof(SearchUsers));
        PagedPendingUserResultModel model = result.DeserializeValue<PagedPendingUserResultModel>()
            ?? throw new InvalidOperationException("SearchUsers returned empty.");
        return model.ToPagedUserDto();
    }

    public async Task<UserDto?> CreateUser(CreateUserDto dto)
    {
        _log.Debug($"UsersOrchestrator.CreateUser: email={dto.Email}");
        var result = await _microServiceOrchestrator.CreateUser(dto.ToMicroServiceModel());
        result.EnsureSuccess(nameof(CreateUser));
        UserModel model = result.DeserializeValue<UserModel>()
            ?? throw new InvalidOperationException("CreateUser returned empty.");
        return model.ToDto();
    }

    public async Task<UserDto?> UpdateUser(string userId, UpdateUserDto dto)
    {
        _log.Debug($"UsersOrchestrator.UpdateUser: userId={userId}");
        var result = await _microServiceOrchestrator.UpdateUser(userId, dto.ToMicroServiceModel());
        result.EnsureSuccess(nameof(UpdateUser));
        UserModel model = result.DeserializeValue<UserModel>()
            ?? throw new InvalidOperationException($"UpdateUser returned empty for userId={userId}.");
        return model.ToDto();
    }

    public async Task DeleteUser(string userId)
    {
        _log.Debug($"UsersOrchestrator.DeleteUser: userId={userId}");
        var result = await _microServiceOrchestrator.DeleteUser(userId);
        result.EnsureSuccess(nameof(DeleteUser));
    }

    public async Task<List<string>?> GetUserRoles(string userId)
    {
        _log.Debug($"UsersOrchestrator.GetUserRoles: userId={userId}");
        var result = await _microServiceOrchestrator.GetUserRoles(userId);
        result.EnsureSuccess(nameof(GetUserRoles));
        return result.DeserializeValue<List<string>>()
            ?? throw new InvalidOperationException($"GetUserRoles returned empty for userId={userId}.");
    }

    public async Task<List<RegistrationCollegeDto>> GetApprovedRegistrationColleges()
    {
        _log.Debug("UsersOrchestrator.GetApprovedRegistrationColleges");
        var result = await _microServiceOrchestrator.GetApprovedRegistrationColleges();
        result.EnsureSuccess(nameof(GetApprovedRegistrationColleges));

        var models = result.DeserializeValue<List<RegistrationCollegeModel>>()
            ?? throw new InvalidOperationException("GetApprovedRegistrationColleges returned empty.");

        return models.Select(model => new RegistrationCollegeDto
        {
            CollegeId = model.CollegeId,
            Name = model.Name
        }).ToList();
    }

    public async Task<List<RegistrationClassDto>> GetRegistrationClasses(string collegeId)
    {
        _log.Debug($"UsersOrchestrator.GetRegistrationClasses: collegeId={collegeId}");
        var result = await _microServiceOrchestrator.GetRegistrationClasses(collegeId);
        result.EnsureSuccess(nameof(GetRegistrationClasses));

        var models = result.DeserializeValue<List<RegistrationClassModel>>()
            ?? throw new InvalidOperationException($"GetRegistrationClasses returned empty for collegeId={collegeId}.");

        return models.Select(model => new RegistrationClassDto
        {
            ClassId = model.ClassId,
            CollegeId = model.CollegeId,
            Name = model.Name,
            AcademicYear = model.AcademicYear
        }).ToList();
    }

    public async Task<List<RegistrationBatchDto>> GetRegistrationBatches(string classId)
    {
        _log.Debug($"UsersOrchestrator.GetRegistrationBatches: classId={classId}");
        var result = await _microServiceOrchestrator.GetRegistrationBatches(classId);
        result.EnsureSuccess(nameof(GetRegistrationBatches));

        var models = result.DeserializeValue<List<RegistrationBatchModel>>()
            ?? throw new InvalidOperationException($"GetRegistrationBatches returned empty for classId={classId}.");

        return models.Select(model => new RegistrationBatchDto
        {
            BatchId = model.BatchId,
            ClassId = model.ClassId,
            CollegeId = model.CollegeId,
            Name = model.Name
        }).ToList();
    }

    /// <summary>
    /// Public self-registration: checks for duplicate email, hashes password,
    /// sets PENDING_APPROVAL for non-SuperAdmin, persists directly to DB.
    /// </summary>
    public async Task<UserDto> RegisterUser(CreateUserDto dto)
    {
        _log.Debug($"UsersOrchestrator.RegisterUser: email={dto.Email}, role={dto.Role}");

        await ValidateStudentRegistrationAsync(dto);

        // Duplicate check
        _log.Debug($"UsersOrchestrator.RegisterUser: checking existing user by email={dto.Email}");
        User? existing = await _usersManager.GetByEmail(dto.Email);
        if (existing is not null)
            throw new InvalidOperationException($"An account with email '{dto.Email}' already exists.");

        // 2. Determine status
        bool isSuperAdmin = dto.Role.Equals(SuperAdminRole, StringComparison.OrdinalIgnoreCase);
        dto.Status = isSuperAdmin ? UserStatus.APPROVED : UserStatus.PENDING_APPROVAL;
        _log.Debug($"UsersOrchestrator.RegisterUser: resolved status={dto.Status} for role={dto.Role}");

        // Build entity + hash password
        var newUser = new User
        {
            FullName   = dto.FullName.Trim(),
            Email      = dto.Email.Trim().ToLowerInvariant(),
            Phone      = dto.Phone?.Trim(),
            EnrollmentNumber = string.IsNullOrWhiteSpace(dto.EnrollmentNumber) ? null : dto.EnrollmentNumber.Trim(),
            CollegeId  = dto.CollegeId,
            CollegeName = dto.CollegeName?.Trim(),
            Role       = dto.Role,
            Status     = dto.Status,
            BatchId    = dto.BatchId,
            ClassId    = dto.ClassId,
            CreatedAt  = DateTime.UtcNow
        };

        if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            var hasher = new PasswordHasher<User>();
            newUser.PasswordHash = hasher.HashPassword(newUser, dto.Password);
        }

        // Persist
        _log.Debug($"UsersOrchestrator.RegisterUser: persisting user record for email={newUser.Email}");
        User created = await _usersManager.Create(newUser);

        _log.Info($"UsersOrchestrator.RegisterUser: created id={created.Id}, status={created.Status}");
        return created.ToDto();
    }

    private async Task ValidateStudentRegistrationAsync(CreateUserDto dto)
    {
        if (!string.Equals(dto.Role, "Student", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!dto.CollegeId.HasValue || dto.CollegeId.Value == Guid.Empty)
        {
            throw new InvalidOperationException("College is required for student registration.");
        }

        if (!string.IsNullOrWhiteSpace(dto.EnrollmentNumber) &&
            dto.EnrollmentNumber.Trim().Length > EnrollmentNumberMaxLength)
        {
            throw new InvalidOperationException($"Enrollment number must not exceed {EnrollmentNumberMaxLength} characters.");
        }

        var hasClassId = dto.ClassId.HasValue && dto.ClassId.Value != Guid.Empty;
        var hasBatchId = dto.BatchId.HasValue && dto.BatchId.Value != Guid.Empty;

        if (hasClassId != hasBatchId)
        {
            throw new InvalidOperationException("Class and batch must either both be selected or both be left empty.");
        }

        if (!hasClassId)
        {
            return;
        }

        var selectedClassId = dto.ClassId!.Value;
        var selectedBatchId = dto.BatchId!.Value;

        var collegeResult = await _microServiceOrchestrator.GetApprovedRegistrationColleges();
        collegeResult.EnsureSuccess(nameof(ValidateStudentRegistrationAsync));
        var colleges = collegeResult.DeserializeValue<List<RegistrationCollegeModel>>() ?? [];
        if (!colleges.Any(college => Guid.TryParse(college.CollegeId, out var collegeId) && collegeId == dto.CollegeId.Value))
        {
            throw new InvalidOperationException("Selected college is invalid.");
        }

        var classesResult = await _microServiceOrchestrator.GetRegistrationClasses(dto.CollegeId.Value.ToString());
        classesResult.EnsureSuccess(nameof(ValidateStudentRegistrationAsync));
        var classes = classesResult.DeserializeValue<List<RegistrationClassModel>>() ?? [];
        if (!classes.Any(item => Guid.TryParse(item.ClassId, out var classId) && classId == selectedClassId))
        {
            throw new InvalidOperationException("Selected class does not belong to the selected college.");
        }

        var batchesResult = await _microServiceOrchestrator.GetRegistrationBatches(selectedClassId.ToString());
        batchesResult.EnsureSuccess(nameof(ValidateStudentRegistrationAsync));
        var batches = batchesResult.DeserializeValue<List<RegistrationBatchModel>>() ?? [];
        if (!batches.Any(item => Guid.TryParse(item.BatchId, out var batchId) && batchId == selectedBatchId))
        {
            throw new InvalidOperationException("Selected batch does not belong to the selected class.");
        }
    }
}
