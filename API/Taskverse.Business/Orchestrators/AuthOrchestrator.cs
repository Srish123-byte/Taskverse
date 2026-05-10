using log4net;
using Taskverse.Api.MicroServices.Interfaces;
using Taskverse.Api.MicroServices.Models;
using Taskverse.Business.DTOs;
using Taskverse.Business.Interface;
using Taskverse.Business.Utilities;

namespace Taskverse.Business.Orchestrators;

public class AuthOrchestrator : IAuthOrchestrator
{
    private readonly IMicroServiceOrchestrator _microServiceOrchestrator;
    private static readonly ILog _log = LogManager.GetLogger(typeof(AuthOrchestrator));

    public AuthOrchestrator(IMicroServiceOrchestrator microServiceOrchestrator)
    {
        _microServiceOrchestrator = microServiceOrchestrator;
    }

    public async Task<LoginResponseDto?> Login(LoginRequestDto request)
    {
        _log.Debug($"AuthOrchestrator.Login: email={request.Email}");

        var result = await _microServiceOrchestrator.Login(new LoginRequestModel(request.Email, request.Password));
        result.EnsureSuccess(nameof(Login));

        LoginResponseModel? model = result.DeserializeValue<LoginResponseModel>();
        if (model is null)
            return null;

        return new LoginResponseDto(
            model.AccessToken,
            model.RefreshToken,
            model.ExpiresAt,
            model.UserId,
            model.Email,
            model.FirstName,
            model.LastName,
            model.Roles);
    }

    public async Task<LoginResponseDto?> RefreshToken(RefreshTokenRequestDto request)
    {
        _log.Debug("AuthOrchestrator.RefreshToken");

        var result = await _microServiceOrchestrator.RefreshToken(new RefreshTokenRequestModel(request.RefreshToken));
        result.EnsureSuccess(nameof(RefreshToken));

        LoginResponseModel? model = result.DeserializeValue<LoginResponseModel>();
        if (model is null)
            return null;

        return new LoginResponseDto(
            model.AccessToken,
            model.RefreshToken,
            model.ExpiresAt,
            model.UserId,
            model.Email,
            model.FirstName,
            model.LastName,
            model.Roles);
    }

    public async Task Logout(LogoutRequestDto request)
    {
        _log.Debug($"AuthOrchestrator.Logout: userId={request.UserId}");

        var result = await _microServiceOrchestrator.Logout(new LogoutRequestModel(request.UserId, request.RefreshToken));
        result.EnsureSuccess(nameof(Logout));
    }

    public async Task<ValidateTokenResponseDto?> ValidateToken(ValidateTokenRequestDto request)
    {
        _log.Debug("AuthOrchestrator.ValidateToken");

        var result = await _microServiceOrchestrator.ValidateToken(new ValidateTokenRequestModel(request.Token));
        result.EnsureSuccess(nameof(ValidateToken));

        ValidateTokenResponseModel? model = result.DeserializeValue<ValidateTokenResponseModel>();
        if (model is null)
            return null;

        return new ValidateTokenResponseDto(
            model.IsValid,
            model.UserId,
            model.Roles,
            model.ExpiresAt);
    }
}
