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

            // Send OTP email
            await _emailService.SendOtpEmailAsync(email, otpCode, purpose.ToString());

            return "OTP sent successfully to " + email;
        }

        public async Task<bool> VerifyOtpAsync(string email, string otpCode, OtpPurpose purpose)
        {
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