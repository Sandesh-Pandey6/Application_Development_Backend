using Autopartspro.Application.Interfaces;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace Autopartspro.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly string _senderEmail;
        private readonly string _senderName;
        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly string _smtpUser;
        private readonly string _smtpPass;

        public EmailService(IConfiguration config)
        {
            _senderEmail = config["SmtpSettings:SenderEmail"] ?? "";
            _senderName = config["SmtpSettings:SenderName"] ?? "AutoPartsPro";
            _smtpHost = config["SmtpSettings:Host"] ?? "smtp.gmail.com";
            _smtpPort = int.Parse(config["SmtpSettings:Port"] ?? "587");
            _smtpUser = config["SmtpSettings:Username"] ?? "";
            _smtpPass = (config["SmtpSettings:Password"] ?? "").Replace(" ", "");

            if (string.IsNullOrWhiteSpace(_smtpUser) || string.IsNullOrWhiteSpace(_smtpPass))
            {
                Console.WriteLine(
                    "WARNING: SmtpSettings Username/Password missing — OTP emails will fail until configured.");
            }
        }

        private async Task SendMimeMessageAsync(MimeMessage message)
        {
            using var client = new SmtpClient();
            client.Timeout = 8000;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
            await client.ConnectAsync(_smtpHost, _smtpPort, MailKit.Security.SecureSocketOptions.StartTls, cts.Token);
            await client.AuthenticateAsync(_smtpUser, _smtpPass, cts.Token);
            await client.SendAsync(message, cts.Token);
            await client.DisconnectAsync(true, cts.Token);
        }

        public Task SendEmailAsync(string toEmail, string subject, string body) =>
            SendEmailAsync(toEmail, subject, body, null, null);

        public async Task SendEmailAsync(
            string toEmail,
            string subject,
            string body,
            string? replyToEmail,
            string? replyToName)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_senderName, _senderEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = body };

            if (!string.IsNullOrWhiteSpace(replyToEmail))
            {
                message.ReplyTo.Add(new MailboxAddress(
                    string.IsNullOrWhiteSpace(replyToName) ? replyToEmail : replyToName.Trim(),
                    replyToEmail.Trim()));
            }

            try
            {
                await SendMimeMessageAsync(message);
                Console.WriteLine($"Email sent successfully to {toEmail}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SMTP ERROR sending to {toEmail}: {ex.Message}");
                throw;
            }
        }

        public async Task SendOtpEmailAsync(string toEmail, string otpCode, string purpose)
        {
            var purposeText = purpose switch
            {
                "EmailVerification" => "verify your email address",
                "Login" => "complete your login",
                "PasswordReset" => "reset your password",
                _ => "verify your identity"
            };

            var subject = purpose switch
            {
                "EmailVerification" => "Verify Your Email - AutoPartsPro",
                "Login" => "Your Login OTP - AutoPartsPro",
                "PasswordReset" => "Password Reset OTP - AutoPartsPro",
                _ => "Your OTP - AutoPartsPro"
            };

            var body = $@"
            <!DOCTYPE html>
            <html>
            <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                <div style='padding: 30px; border: 1px solid #e0e0e0; border-radius: 8px;'>
                    <h2 style='color: #1a1a2e;'>AutoPartsPro</h2>
                    <hr style='border: 1px solid #e0e0e0;' />
                    <p>Hello,</p>
                    <p>Use the OTP below to <strong>{purposeText}</strong>.</p>
                    <div style='text-align: center; margin: 30px 0;'>
                        <span style='
                            font-size: 36px;
                            font-weight: bold;
                            letter-spacing: 12px;
                            color: #1a1a2e;
                            background: #f5f5f5;
                            padding: 15px 25px;
                            border-radius: 8px;
                            display: inline-block;
                        '>{otpCode}</span>
                    </div>
                    <p style='color: #666;'>This OTP is valid for <strong>10 minutes</strong>.</p>
                    <p style='color: #666;'>If you did not request this, please ignore this email.</p>
                    <hr style='border: 1px solid #e0e0e0;' />
                    <p style='color: #999; font-size: 12px;'>AutoPartsPro — Vehicle Parts & Service Center</p>
                </div>
            </body>
            </html>";

            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendWelcomeEmailAsync(string toEmail, string fullName)
        {
            var subject = "Welcome to AutoPartsPro!";
            var body = $@"
            <!DOCTYPE html>
            <html>
            <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                <div style='padding: 30px; border: 1px solid #e0e0e0; border-radius: 8px;'>
                    <h2 style='color: #1a1a2e;'>Welcome to AutoPartsPro!</h2>
                    <hr style='border: 1px solid #e0e0e0;' />
                    <p>Hi <strong>{fullName}</strong>,</p>
                    <p>Your account has been successfully verified and activated.</p>
                    <p>Thank you for choosing AutoPartsPro!</p>
                    <hr style='border: 1px solid #e0e0e0;' />
                    <p style='color: #999; font-size: 12px;'>AutoPartsPro — Vehicle Parts & Service Center</p>
                </div>
            </body>
            </html>";

            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendStaffApprovalEmailAsync(string toEmail, string fullName)
        {
            var subject = "Your Staff Account Has Been Approved - AutoPartsPro";
            var body = $@"
            <!DOCTYPE html>
            <html>
            <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                <div style='padding: 30px; border: 1px solid #e0e0e0; border-radius: 8px;'>
                    <h2 style='color: #1a1a2e;'>Account Approved!</h2>
                    <p>Hi <strong>{fullName}</strong>, your staff account has been approved. You can now log in.</p>
                </div>
            </body>
            </html>";

            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendStaffRejectionEmailAsync(string toEmail, string fullName)
        {
            var subject = "Your Staff Account Request - AutoPartsPro";
            var body = $@"
            <!DOCTYPE html>
            <html>
            <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                <div style='padding: 30px; border: 1px solid #e0e0e0; border-radius: 8px;'>
                    <p>Hi <strong>{fullName}</strong>, your staff account request was not approved. Contact your admin for details.</p>
                </div>
            </body>
            </html>";

            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendInvoiceEmailAsync(string toEmail, string fullName,
            string invoiceNumber, byte[] invoicePdf)
        {
            if (!InvoicePdfGenerator.IsValidPdf(invoicePdf))
            {
                throw new ArgumentException(
                    "Invoice attachment is not a valid PDF. Regenerate the invoice and try again.",
                    nameof(invoicePdf));
            }

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_senderName, _senderEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = $"Your Invoice {invoiceNumber} - AutoPartsPro";

            var bodyHtml = $@"
            <!DOCTYPE html>
            <html>
            <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                <div style='padding: 30px; border: 1px solid #e0e0e0; border-radius: 8px;'>
                    <p>Hi <strong>{fullName}</strong>,</p>
                    <p>Please find your invoice <strong>{invoiceNumber}</strong> attached as a PDF.</p>
                    <p style='color:#666;font-size:13px;'>Open the attachment with a PDF reader (Adobe Acrobat, browser preview, etc.).</p>
                </div>
            </body>
            </html>";

            var fileName = SanitizePdfFileName(invoiceNumber);
            var multipart = new Multipart("mixed");
            multipart.Add(new TextPart("html") { Text = bodyHtml });

            var attachment = new MimePart("application", "pdf")
            {
                Content = new MimeContent(new MemoryStream(invoicePdf, writable: false)),
                ContentDisposition = new ContentDisposition(ContentDisposition.Attachment)
                {
                    FileName = fileName,
                    CreationDate = DateTimeOffset.UtcNow,
                    ModificationDate = DateTimeOffset.UtcNow,
                },
                ContentTransferEncoding = ContentEncoding.Base64,
            };
            attachment.ContentType.Charset = null;
            multipart.Add(attachment);
            message.Body = multipart;

            await SendMimeMessageAsync(message);
        }

        private static string SanitizePdfFileName(string invoiceNumber)
        {
            var baseName = string.IsNullOrWhiteSpace(invoiceNumber) ? "invoice" : invoiceNumber.Trim();
            foreach (var c in Path.GetInvalidFileNameChars())
                baseName = baseName.Replace(c, '_');
            return baseName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)
                ? baseName
                : $"{baseName}.pdf";
        }

        public async Task SendLowStockAlertEmailAsync(string toEmail,
            string partName, int currentStock)
        {
            var subject = $"Low Stock Alert: {partName} - AutoPartsPro";
            var body = $@"
            <!DOCTYPE html>
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <p><strong>{partName}</strong> is low on stock: <strong>{currentStock}</strong> units remaining.</p>
            </body>
            </html>";

            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendCreditReminderEmailAsync(string toEmail,
            string fullName, decimal overdueAmount)
        {
            var subject = "Payment Reminder - AutoPartsPro";
            var body = $@"
            <!DOCTYPE html>
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <p>Hi <strong>{fullName}</strong>, you have an overdue balance of <strong>Rs. {overdueAmount:N2}</strong>.</p>
            </body>
            </html>";

            await SendEmailAsync(toEmail, subject, body);
        }
    }
}
