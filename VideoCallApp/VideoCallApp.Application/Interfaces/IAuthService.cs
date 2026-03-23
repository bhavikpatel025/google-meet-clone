using VideoCallApp.Application.Common;
using VideoCallApp.Application.DTOs.Auth;

namespace VideoCallApp.Application.Interfaces;

public interface IAuthService
{
    Task<ApiResponse<AuthResponseDto>> RegisterAsync(RegisterRequestDto request);
    Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginRequestDto request);
}