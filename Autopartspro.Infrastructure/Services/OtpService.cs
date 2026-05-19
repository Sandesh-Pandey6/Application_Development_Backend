using Autopartspro.Application.Interfaces;
using Autopartspro.Domain.Entities;
using Autopartspro.Domain.Enums;
using Autopartspro.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Autopartspro.Infrastructure.Services
{
    public class OtpService : IOtpService
    {
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;

        public OtpService(AppDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public async Task<string> GenerateAndSendOtpAsync(string email, OtpPurpose purpose)
        {
            email = email.Trim().ToLowerInvariant();

            // Invalidate all previous unused OTPs for this email + purpose
            var existingOtps = await _context.OtpVerifications
                .Where(o => o.Email == email && o.Purpose == purpose && !o.IsUsed)
                .ToListAsync();

            foreach (var old in existingOtps)
                old.IsUsed = true;

            // Generate 6-digit OTP
            var otpCode = new Random().Next(100000, 999999).ToString();

            var otp = new OtpVerification
            {
                Email = email,
                OtpCode = otpCode,
                Purpose = purpose,
                IsUsed = false,
                ExpiresAt = DateTime.UtcNow.AddMinutes(10)
            };

            _context.OtpVerifications.Add(otp);
            await _context.SaveChangesAsync();

            // Dev fallback: always log OTP (also sent by email when SMTP is configured)
            Console.WriteLine($"\n==========================================");
            Console.WriteLine($"OTP FOR {email}: {otpCode} (purpose: {purpose})");
            Console.WriteLine($"==========================================\n");

            try
            {
                await _emailService.SendOtpEmailAsync(email, otpCode, purpose.ToString());
                return $"Verification code sent to {email}. Please check your inbox (and spam folder).";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send OTP email to {email}: {ex.Message}");
                return
                    "Your account was saved. We could not send the email — use the 6-digit code shown in the API console (Visual Studio Output), then enter it on the verification screen.";
            }
        }

        public async Task<bool> VerifyOtpAsync(string email, string otpCode, OtpPurpose purpose)
        {
            email = email.Trim().ToLowerInvariant();
            otpCode = otpCode.Trim();

            var otp = await _context.OtpVerifications
                .Where(o =>
                    o.Email == email &&
                    o.OtpCode == otpCode &&
                    o.Purpose == purpose &&
                    !o.IsUsed &&
                    o.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            if (otp == null) return false;

            otp.IsUsed = true;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}