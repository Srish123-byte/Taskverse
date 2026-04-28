using log4net;
using Taskverse.Api.MicroServices.Interfaces;
using Taskverse.Api.MicroServices.Models;
using Taskverse.Business.DTOs;
using Taskverse.Business.Interface;
using Taskverse.Business.Mappings;
using Taskverse.Business.Utilities;

namespace Taskverse.Business.Orchestrators;

public class UsersOrchestrator : IUsersOrchestrator
{
    private readonly IMicroServiceOrchestrator _microServiceOrchestrator;
    private static readonly ILog _log = LogManager.GetLogger(typeof(UsersOrchestrator));

    public UsersOrchestrator(IMicroServiceOrchestrator microServiceOrchestrator)
    {
        _microServiceOrchestrator = microServiceOrchestrator;
    }

    public async Task<UserDto> GetUser(string userId)
    {
        _log.Debug($"UsersOrchestrator.GetUser: userId={userId}");

        var result = await _microServiceOrchestrator.GetUser(userId);
        result.EnsureSuccess(nameof(GetUser));

        UserModel model = result.DeserializeValue<UserModel>()
            ?? throw new InvalidOperationException($"GetUser returned an empty response for userId={userId}.");

        return model.ToDto();
    }

    public async Task<PagedUserDto> SearchUsers(string? email, string? role, bool? isActive, int pageNumber, int pageSize)
    {
        _log.Debug($"UsersOrchestrator.SearchUsers: email={email}, role={role}, isActive={isActive}, page={pageNumber}");

        var criteria = new UserSearchCriteriaModel(email, role, isActive, pageNumber, pageSize);
        var result = await _microServiceOrchestrator.SearchUsers(criteria);
        result.EnsureSuccess(nameof(SearchUsers));

        PagedUserResultModel model = result.DeserializeValue<PagedUserResultModel>()
            ?? throw new InvalidOperationException("SearchUsers returned an empty response.");

        return model.ToDto();
    }

    public async Task<UserDto> CreateUser(CreateUserDto dto)
    {
        _log.Debug($"UsersOrchestrator.CreateUser: email={dto.Email}");

        var result = await _microServiceOrchestrator.CreateUser(dto.ToMicroServiceModel());
        result.EnsureSuccess(nameof(CreateUser));

        UserModel model = result.DeserializeValue<UserModel>()
            ?? throw new InvalidOperationException("CreateUser returned an empty response.");

        return model.ToDto();
    }

    public async Task<UserDto> UpdateUser(string userId, UpdateUserDto dto)
    {
        _log.Debug($"UsersOrchestrator.UpdateUser: userId={userId}");

        var result = await _microServiceOrchestrator.UpdateUser(userId, dto.ToMicroServiceModel());
        result.EnsureSuccess(nameof(UpdateUser));

        UserModel model = result.DeserializeValue<UserModel>()
            ?? throw new InvalidOperationException($"UpdateUser returned an empty response for userId={userId}.");

        return model.ToDto();
    }

    public async Task DeleteUser(string userId)
    {
        _log.Debug($"UsersOrchestrator.DeleteUser: userId={userId}");

        var result = await _microServiceOrchestrator.DeleteUser(userId);
        result.EnsureSuccess(nameof(DeleteUser));
    }

    public async Task<List<string>> GetUserRoles(string userId)
    {
        _log.Debug($"UsersOrchestrator.GetUserRoles: userId={userId}");

        var result = await _microServiceOrchestrator.GetUserRoles(userId);
        result.EnsureSuccess(nameof(GetUserRoles));

        return result.DeserializeValue<List<string>>()
            ?? throw new InvalidOperationException($"GetUserRoles returned an empty response for userId={userId}.");
    }
}
