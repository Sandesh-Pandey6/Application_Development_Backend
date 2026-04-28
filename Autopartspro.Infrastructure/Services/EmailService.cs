using Autopartspro.Application.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace Autopartspro.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendOtpEmailAsync(string toEmail, string fullName, string otpCode)
    {
        var subject = "Verify Your Email — AutoPartsPro";
        var body = $"""
            <div style="font-family: Arial, sans-serif; max-width: 480px; margin: auto;">
              <h2 style="color: #1a1a2e;">Verify Your Email</h2>
              <p>Hi <strong>{fullName}</strong>,</p>
              <p>Use the 6-digit code below to verify your account. It expires in <strong>10 minutes</strong>.</p>
              <div style="font-size: 36px; letter-spacing: 12px; font-weight: bold;
                          background: #f0f0f0; padding: 16px 24px; border-radius: 8px;
                          text-align: center; margin: 24px 0;">
                {otpCode}
              </div>
              <p style="color: #666; font-size: 13px;">
                If you didn't request this, please ignore this email.
              </p>
            </div>
            """;

        await SendGenericEmailAsync(toEmail, subject, body);
    }

    public async Task SendGenericEmailAsync(string toEmail, string subject, string htmlBody)
    {
        var smtp = _config.GetSection("SmtpSettings");

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(
            smtp["SenderName"] ?? "AutoPartsPro",
            smtp["SenderEmail"]!));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = htmlBody };

        using var client = new SmtpClient();
        await client.ConnectAsync(
            smtp["Host"]!,
            int.Parse(smtp["Port"] ?? "587"),
            SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(smtp["Username"]!, smtp["Password"]!);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}