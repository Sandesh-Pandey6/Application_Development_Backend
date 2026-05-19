namespace Autopartspro.Application.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body);
        Task SendEmailAsync(string toEmail, string subject, string body, string? replyToEmail, string? replyToName);
        Task SendOtpEmailAsync(string toEmail, string otpCode, string purpose);
        Task SendWelcomeEmailAsync(string toEmail, string fullName);
        Task SendStaffApprovalEmailAsync(string toEmail, string fullName);
        Task SendStaffRejectionEmailAsync(string toEmail, string fullName);
        Task SendInvoiceEmailAsync(string toEmail, string fullName, string invoiceNumber, byte[] invoicePdf);
        Task SendLowStockAlertEmailAsync(string toEmail, string partName, int currentStock);
        Task SendCreditReminderEmailAsync(string toEmail, string fullName, decimal overdueAmount);
    }
}