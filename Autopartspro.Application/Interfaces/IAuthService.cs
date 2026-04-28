using Autopartspro.Application.DTOs.Auth;

namespace Autopartspro.Application.Interfaces;

public interface IAuthService
{
    Task<RegisterResponseDto> RegisterCustomerAsync(CustomerRegisterDto dto);
    Task<RegisterResponseDto> RegisterStaffAsync(StaffRegisterDto dto);
    Task<AuthResponseDto> LoginAsync(LoginDto dto);
    Task<bool> VerifyOtpAsync(VerifyOtpDto dto);
    Task<bool> ResendOtpAsync(ResendOtpDto dto);
} 