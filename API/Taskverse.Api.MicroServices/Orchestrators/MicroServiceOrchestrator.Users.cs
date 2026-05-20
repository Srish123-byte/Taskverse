using Microsoft.AspNetCore.Mvc;
using Taskverse.Api.MicroServices.Enums;
using Taskverse.Api.MicroServices.Models;

namespace Taskverse.Api.MicroServices.Orchestrators;

public partial class MicroServiceOrchestrator
{
    public async Task<ObjectResult> GetUser(string userId)
    {
        var url = $"{GetMicroServiceUrl(MicroService.Users)}users/{userId}";
        return await Get<UserModel>(url);
    }

    public async Task<ObjectResult> GetPendingUsers()
    {
        var url = $"{GetMicroServiceUrl(MicroService.Users)}api/users/pending";
        return await Get<List<PendingUserModel>>(url);
    }

    public async Task<ObjectResult> SearchUsers(UserSearchCriteriaModel criteria)
    {
        var url = $"{GetMicroServiceUrl(MicroService.Users)}api/users/search";
        return await Post<PagedPendingUserResultModel>(url, criteria);
    }

    public async Task<ObjectResult> CreateUser(CreateUserModel model)
    {
        var url = $"{GetMicroServiceUrl(MicroService.Users)}users";
        return await Post<UserModel>(url, model);
    }

    public async Task<ObjectResult> UpdateUser(string userId, UpdateUserModel model)
    {
        var url = $"{GetMicroServiceUrl(MicroService.Users)}users/{userId}";
        return await Put<UserModel>(url, model);
    }

    public async Task<ObjectResult> DeleteUser(string userId)
    {
        var url = $"{GetMicroServiceUrl(MicroService.Users)}users/{userId}";
        return await Delete(url);
    }

    public async Task<ObjectResult> GetUserRoles(string userId)
    {
        var url = $"{GetMicroServiceUrl(MicroService.Users)}users/{userId}/roles";
        return await Get<List<string>>(url);
    }
}
