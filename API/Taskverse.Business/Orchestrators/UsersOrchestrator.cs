using log4net;
using Microsoft.AspNetCore.Identity;
using Taskverse.Api.MicroServices.Interfaces;
using Taskverse.Api.MicroServices.Models;
using Taskverse.Business.DTOs;
using Taskverse.Business.Interface;
using Taskverse.Business.Mappings;
using Taskverse.Business.Utilities;
using Taskverse.Data.DataAccess;

namespace Taskverse.Business.Orchestrators;

public class UsersOrchestrator : IUsersOrchestrator
{
    private const string SuperAdminRole = "SuperAdmin";

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
        var criteria = new UserSearchCriteriaModel(email, role, isActive, pageNumber, pageSize);
        var result = await _microServiceOrchestrator.SearchUsers(criteria);
        result.EnsureSuccess(nameof(SearchUsers));
        PagedUserResultModel model = result.DeserializeValue<PagedUserResultModel>()
            ?? throw new InvalidOperationException("SearchUsers returned empty.");
        return model.ToDto();
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

    /// <summary>
    /// Public self-registration: checks for duplicate email, hashes password,
    /// sets PENDING_APPROVAL for non-SuperAdmin, persists directly to DB.
    /// </summary>
    public async Task<UserDto> RegisterUser(CreateUserDto dto)
    {
        _log.Debug($"UsersOrchestrator.RegisterUser: email={dto.Email}, role={dto.Role}");

        // 1. Duplicate check
        User? existing = await _usersManager.GetByEmail(dto.Email);
        if (existing is not null)
            throw new InvalidOperationException($"An account with email '{dto.Email}' already exists.");

        // 2. Determine status
        bool isSuperAdmin = dto.Role.Equals(SuperAdminRole, StringComparison.OrdinalIgnoreCase);
        string status = isSuperAdmin ? "ACTIVE" : "PENDING_APPROVAL";

        // 3. Build entity + hash password
        var newUser = new User
        {
            FullName  = dto.FullName.Trim(),
            Email     = dto.Email.Trim().ToLowerInvariant(),
            Phone     = dto.Phone?.Trim(),
            CollegeId = dto.CollegeId,
            Role      = dto.Role,
            Status    = status,
            CreatedAt = DateTime.UtcNow
        };

        if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            var hasher = new PasswordHasher<User>();
            newUser.PasswordHash = hasher.HashPassword(newUser, dto.Password);
        }

        // 4. Persist
        User created = await _usersManager.Create(newUser);

        _log.Info($"UsersOrchestrator.RegisterUser: created id={created.Id}, status={created.Status}");
        return created.ToDto();
    }
}
