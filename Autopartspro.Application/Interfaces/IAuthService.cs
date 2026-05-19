using Autopartspro.Application.DOTs.auth;

namespace Autopartspro.Application.Interfaces
{
    public interface IAuthService
    {
        Task<string> RegisterAsync(RegisterDto dto);
        Task<AuthResponseDto> VerifyEmailOtpAsync(VerifyOtpDto dto);
        Task<AuthResponseDto> LoginAsync(LoginDto dto);
        Task<AuthResponseDto> VerifyLoginOtpAsync(VerifyOtpDto dto);
        Task<string> ResendOtpAsync(ResendOtpDto dto);
        Task<string> ForgotPasswordAsync(ForgotPasswordDto dto);
        Task<string> ResetPasswordAsync(ResetPasswordDto dto);
        Task<AuthResponseDto> GetCurrentUserAsync(string email);
    }
}