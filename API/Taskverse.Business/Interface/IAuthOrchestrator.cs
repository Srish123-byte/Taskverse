using Taskverse.Business.DTOs;

namespace Taskverse.Business.Interface;

public interface IAuthOrchestrator
{
    Task<LoginResponseDto?> Login(LoginRequestDto request);
    Task<RefreshLoginResponseDto?> RefreshToken(RefreshTokenRequestDto request);
    Task Logout(LogoutRequestDto request);
    Task<ValidateTokenResponseDto?> ValidateToken(ValidateTokenRequestDto request);
    Task ChangePassword(ChangePasswordRequestDto request);
}
