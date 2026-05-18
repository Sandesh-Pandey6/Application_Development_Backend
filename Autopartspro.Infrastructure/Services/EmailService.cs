using System.Net;
using System.Net.Mail;
using Autopartspro.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using MimeKit;
using MailKit.Net.Smtp;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace Autopartspro.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly string _smtpUser;
        private readonly string _smtpPass;
        private readonly string _senderEmail;
        private readonly string _senderName;

        public EmailService(IConfiguration config)
        {
            _senderEmail = config["SmtpSettings:SenderEmail"] ?? "";
            _senderName = config["SmtpSettings:SenderName"] ?? "AutoPartsPro";
            _smtpHost = config["SmtpSettings:Host"] ?? "smtp.gmail.com";
            _smtpPort = int.Parse(config["SmtpSettings:Port"] ?? "587");
            _smtpUser = config["SmtpSettings:Username"] ?? "";
            _smtpPass = (config["SmtpSettings:Password"] ?? "").Replace(" ", "");
        }

        private async Task SendMimeMessageAsync(MimeMessage message)
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(_smtpHost, _smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_smtpUser, _smtpPass);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_senderName, _senderEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = body };

            await SendMimeMessageAsync(message);
            Console.WriteLine($"✅ Email sent successfully to {toEmail}");
        }

        public async Task SendOtpEmailAsync(string toEmail, string otpCode, string purpose)
        {
            var purposeText = purpose switch
            {
                "Registration" => "verify your email address",
                "PasswordReset" => "reset your password",
                "Login" => "complete your login",
                _ => "verify your action"
            };

            var subject = $"Your AutoPartsPro Verification Code ({otpCode})";
            var bodyHtml = $@"
            <html>
            <body style='font-family: Arial, sans-serif; color: #333; line-height: 1.6;'>
                <div style='max-width: 600px; margin: 0 auto; border: 1px solid #eee; padding: 20px; border-radius: 8px;'>
                    <h2 style='color: #2c3e50; text-align: center; border-bottom: 2px solid #3498db; padding-bottom: 10px;'>AutoPartsPro Security</h2>
                    <p>Hello,</p>
                    <p>You requested a one-time password (OTP) to <strong>{purposeText}</strong>.</p>
                    <div style='background-color: #f8f9fa; padding: 15px; text-align: center; margin: 20px 0; border-radius: 5px;'>
                        <span style='font-size: 32px; font-weight: bold; letter-spacing: 5px; color: #e74c3c;'>{otpCode}</span>
                    </div>
                    <p>This code will expire in <strong>10 minutes</strong>.</p>
                    <hr style='border: none; border-top: 1px solid #eee; margin: 20px 0;' />
                    <p style='font-size: 0.8em; text-align: center; color: #95a5a6;'>AutoPartsPro Inc.</p>
                </div>
            </body>
            </html>";

            await SendEmailAsync(toEmail, subject, bodyHtml);
        }

        public async Task SendWelcomeEmailAsync(string toEmail, string fullName)
        {
            var subject = "Welcome to AutoPartsPro!";
            var body = $"<h1>Welcome {fullName}!</h1><p>Thank you for joining AutoPartsPro.</p>";
            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendStaffApprovalEmailAsync(string toEmail, string fullName)
        {
            var subject = "Your Staff Account is Approved";
            var body = $"<h1>Hello {fullName}</h1><p>Your staff account has been approved by the Admin.</p>";
            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendStaffRejectionEmailAsync(string toEmail, string fullName)
        {
            var subject = "Staff Account Application Update";
            var body = $"<h1>Hello {fullName}</h1><p>Your staff account application was not approved.</p>";
            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendInvoiceEmailAsync(string toEmail, string fullName, string invoiceNumber, byte[] invoicePdf)
        {
            var subject = $"Invoice {invoiceNumber} from AutoPartsPro";
            var bodyHtml = $@"
            <html>
            <body>
                <h2>AutoPartsPro Invoice</h2>
                <p>Dear {fullName},</p>
                <p>Please find attached your invoice <strong>{invoiceNumber}</strong>.</p>
            </body>
            </html>";

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_senderName, _senderEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = bodyHtml };
            bodyBuilder.Attachments.Add($"{invoiceNumber}.pdf", invoicePdf, ContentType.Parse("application/pdf"));
            message.Body = bodyBuilder.ToMessageBody();

            await SendMimeMessageAsync(message);
        }

        public async Task SendLowStockAlertEmailAsync(string toEmail, string partName, int currentStock)
        {
            var subject = $"URGENT: Low Stock Alert - {partName}";
            var bodyHtml = $@"
            <html>
            <body>
                <h2>Low Stock Warning</h2>
                <p>The inventory level for <strong>{partName}</strong> has dropped.</p>
                <p>Current Stock: {currentStock}</p>
            </body>
            </html>";

            await SendEmailAsync(toEmail, subject, bodyHtml);
        }

        public async Task SendCreditReminderEmailAsync(string toEmail, string fullName, decimal overdueAmount)
        {
            var subject = "Overdue Payment Reminder";
            var body = $"<h1>Hello {fullName}</h1><p>You have an overdue payment of {overdueAmount:C}.</p>";
            await SendEmailAsync(toEmail, subject, body);
        }
    }
}
