using log4net;
using Newtonsoft.Json.Linq;
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
        if (!result.IsSuccess())
        {
            if (result.StatusCode == 401)
            {
                throw new UnauthorizedAccessException(ExtractMessage(result.Value) ?? "Invalid credentials");
            }

            result.EnsureSuccess(nameof(Login));
        }

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
            model.CollegeName,
            model.Roles,
            model.Status);
    }

    private static string? ExtractMessage(object? value)
    {
        if (value is null)
            return null;

        if (value is string json)
        {
            try
            {
                return JObject.Parse(json)["message"]?.ToString()
                    ?? JObject.Parse(json)["Message"]?.ToString()
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

    public async Task<RefreshLoginResponseDto?> RefreshToken(RefreshTokenRequestDto request)
    {
        _log.Debug("AuthOrchestrator.RefreshToken");

        var result = await _microServiceOrchestrator.RefreshToken(new RefreshTokenRequestModel(request.RefreshToken));
        result.EnsureSuccess(nameof(RefreshToken));

        RefreshTokenResponseModel? model = result.DeserializeValue<RefreshTokenResponseModel>();
        if (model is null)
            return null;

        return new RefreshLoginResponseDto(
            model.AccessToken,
            model.RefreshToken,
            model.ExpiresAt);
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
