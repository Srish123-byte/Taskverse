using Microsoft.AspNetCore.Mvc;
using Taskverse.Api.MicroServices.Enums;
using Taskverse.Api.MicroServices.Models;

namespace Taskverse.Api.MicroServices.Orchestrators;

public partial class MicroServiceOrchestrator
{
    public async Task<ObjectResult> Login(LoginRequestModel model)
    {
        var url = $"{GetMicroServiceUrl(MicroService.Auth)}api/auth/login";
        return await Post<LoginResponseModel>(url, model);
    }

    public async Task<ObjectResult> RefreshToken(RefreshTokenRequestModel model)
    {
        var url = $"{GetMicroServiceUrl(MicroService.Auth)}api/auth/refresh-token";
        return await Post<RefreshTokenResponseModel>(url, model);
    }

    public async Task<ObjectResult> Logout(LogoutRequestModel model)
    {
        var url = $"{GetMicroServiceUrl(MicroService.Auth)}api/auth/logout";
        return await Post<object>(url, model);
    }

    public async Task<ObjectResult> ValidateToken(ValidateTokenRequestModel model)
    {
        var url = $"{GetMicroServiceUrl(MicroService.Auth)}api/auth/validate";
        return await Post<ValidateTokenResponseModel>(url, model);
    }

    public async Task<ObjectResult> ChangeTemporaryPassword(ChangeTemporaryPasswordRequestModel model)
    {
        var url = $"{GetMicroServiceUrl(MicroService.Auth)}api/auth/change-temporary-password";
        return await Post<object>(url, model);
    }
}
