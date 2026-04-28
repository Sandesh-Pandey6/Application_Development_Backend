namespace Autopartspro.Application.Interfaces;

public interface IEmailService
{
    Task SendOtpEmailAsync(string toEmail, string fullName, string otpCode);
    Task SendGenericEmailAsync(string toEmail, string subject, string htmlBody);
} 