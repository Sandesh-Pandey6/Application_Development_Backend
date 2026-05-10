using Autopartspro.Domain.Enums;

namespace Autopartspro.Application.Interfaces
{
    public interface IOtpService
    {
        Task<string> GenerateAndSendOtpAsync(string email, OtpPurpose purpose);
        Task<bool> VerifyOtpAsync(string email, string otpCode, OtpPurpose purpose);
    }
}