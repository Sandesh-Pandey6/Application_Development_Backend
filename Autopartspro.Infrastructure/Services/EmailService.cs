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
            _smtpPass = config["SmtpSettings:Password"] ?? "";
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
                    <p>You can now:</p>
                    <ul>
                        <li>Book service appointments</li>
                        <li>View your purchase history</li>
                        <li>Request unavailable parts</li>
                        <li>Track your loyalty rewards</li>
                    </ul>
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
                    <hr style='border: 1px solid #e0e0e0;' />
                    <p>Hi <strong>{fullName}</strong>,</p>
                    <p>Your staff account has been <strong>approved</strong> by the admin.</p>
                    <p>You can now log in and start using the system.</p>
                    <hr style='border: 1px solid #e0e0e0;' />
                    <p style='color: #999; font-size: 12px;'>AutoPartsPro — Vehicle Parts & Service Center</p>
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
                    <h2 style='color: #1a1a2e;'>Account Request Update</h2>
                    <hr style='border: 1px solid #e0e0e0;' />
                    <p>Hi <strong>{fullName}</strong>,</p>
                    <p>Unfortunately, your staff account request has not been approved at this time.</p>
                    <p>Please contact your admin for further information.</p>
                    <hr style='border: 1px solid #e0e0e0;' />
                    <p style='color: #999; font-size: 12px;'>AutoPartsPro — Vehicle Parts & Service Center</p>
                </div>
            </body>
            </html>";

            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendInvoiceEmailAsync(string toEmail, string fullName,
            string invoiceNumber, byte[] invoicePdf)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_senderName, _senderEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = $"Your Invoice {invoiceNumber} - AutoPartsPro";

            var bodyHtml = $@"
            <!DOCTYPE html>
            <html>
            <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                <div style='padding: 30px; border: 1px solid #e0e0e0; border-radius: 8px;'>
                    <h2 style='color: #1a1a2e;'>Your Invoice</h2>
                    <hr style='border: 1px solid #e0e0e0;' />
                    <p>Hi <strong>{fullName}</strong>,</p>
                    <p>Please find your invoice <strong>{invoiceNumber}</strong> attached to this email.</p>
                    <p>Thank you for your purchase at AutoPartsPro!</p>
                    <hr style='border: 1px solid #e0e0e0;' />
                    <p style='color: #999; font-size: 12px;'>AutoPartsPro — Vehicle Parts & Service Center</p>
                </div>
            </body>
            </html>";

            var bodyBuilder = new BodyBuilder { HtmlBody = bodyHtml };
            bodyBuilder.Attachments.Add($"{invoiceNumber}.pdf", invoicePdf, ContentType.Parse("application/pdf"));
            message.Body = bodyBuilder.ToMessageBody();

            await SendMimeMessageAsync(message);
        }

        public async Task SendLowStockAlertEmailAsync(string toEmail,
            string partName, int currentStock)
        {
            var subject = $"Low Stock Alert: {partName} - AutoPartsPro";
            var body = $@"
            <!DOCTYPE html>
            <html>
            <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                <div style='padding: 30px; border: 1px solid #e0e0e0; border-radius: 8px;'>
                    <h2 style='color: #e74c3c;'>Low Stock Alert</h2>
                    <hr style='border: 1px solid #e0e0e0;' />
                    <p>The following part has dropped below the minimum stock level:</p>
                    <table style='width: 100%; border-collapse: collapse;'>
                        <tr style='background: #f5f5f5;'>
                            <td style='padding: 10px; border: 1px solid #e0e0e0;'><strong>Part Name</strong></td>
                            <td style='padding: 10px; border: 1px solid #e0e0e0;'>{partName}</td>
                        </tr>
                        <tr>
                            <td style='padding: 10px; border: 1px solid #e0e0e0;'><strong>Current Stock</strong></td>
                            <td style='padding: 10px; border: 1px solid #e0e0e0; color: #e74c3c;'>
                                <strong>{currentStock} units</strong>
                            </td>
                        </tr>
                    </table>
                    <p style='margin-top: 20px;'>Please reorder this part as soon as possible.</p>
                    <hr style='border: 1px solid #e0e0e0;' />
                    <p style='color: #999; font-size: 12px;'>AutoPartsPro — Vehicle Parts & Service Center</p>
                </div>
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
            <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                <div style='padding: 30px; border: 1px solid #e0e0e0; border-radius: 8px;'>
                    <h2 style='color: #e67e22;'>Payment Reminder</h2>
                    <hr style='border: 1px solid #e0e0e0;' />
                    <p>Hi <strong>{fullName}</strong>,</p>
                    <p>This is a friendly reminder that you have an outstanding balance of
                        <strong style='color: #e74c3c;'>Rs. {overdueAmount:N2}</strong>
                        that is overdue by more than 1 month.
                    </p>
                    <p>Please settle your payment at your earliest convenience to avoid
                        any service disruptions.</p>
                    <p>If you have already made the payment, please disregard this email.</p>
                    <hr style='border: 1px solid #e0e0e0;' />
                    <p style='color: #999; font-size: 12px;'>AutoPartsPro — Vehicle Parts & Service Center</p>
                </div>
            </body>
            </html>";

            await SendEmailAsync(toEmail, subject, body);
        }
    }
}
