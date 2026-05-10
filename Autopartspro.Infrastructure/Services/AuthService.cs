using Autopartspro.Application.DOTs.auth;
using Autopartspro.Application.Interfaces;
using Autopartspro.Domain.Entities;
using Autopartspro.Domain.Enums;
using Autopartspro.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Autopartspro.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IJwtService _jwtService;
        private readonly IOtpService _otpService;
        private readonly IEmailService _emailService;

        public AuthService(AppDbContext context, IJwtService jwtService,
            IOtpService otpService, IEmailService emailService)
        {
            _context = context;
            _jwtService = jwtService;
            _otpService = otpService;
            _emailService = emailService;
        }

        // ══════════════════════════════════════════════════════
        // REGISTER — Step 1
        // ══════════════════════════════════════════════════════
        public async Task<string> RegisterAsync(RegisterDto dto)
        {
            if (!dto.AgreeToTerms)
                throw new Exception("You must agree to the Terms of Service and Privacy Policy.");

            if (dto.Password != dto.ConfirmPassword)
                throw new Exception("Passwords do not match.");

            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == dto.Email);

            if (existingUser != null && existingUser.IsEmailVerified)
                throw new Exception("Email is already registered.");

            // Remove unverified duplicate
            if (existingUser != null && !existingUser.IsEmailVerified)
            {
                _context.Users.Remove(existingUser);
                await _context.SaveChangesAsync();
            }

            var role = dto.Role == "Staff" ? RoleType.Staff : RoleType.Customer;

            var user = new User
            {
                FullName = dto.FullName,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                PhoneNumber = dto.PhoneNumber,
                City = dto.City,
                Role = role,
                Status = StatusType.Active,
                IsEmailVerified = false
            };

            _context.Users.Add(user);

            // Customer: Add Vehicle
            if (role == RoleType.Customer && !string.IsNullOrEmpty(dto.NumberPlate))
            {
                var fuelType = Enum.TryParse<FuelType>(dto.FuelType, out var ft)
                    ? ft : FuelType.Petrol;

                _context.Vehicles.Add(new Vehicle
                {
                    CustomerId = user.Id,
                    Make = dto.Make ?? string.Empty,
                    Model = dto.Model ?? string.Empty,
                    Year = dto.Year ?? 0,
                    FuelType = fuelType,
                    NumberPlate = dto.NumberPlate
                });
            }

            // Staff: Add Employment Info
            if (role == RoleType.Staff && !string.IsNullOrEmpty(dto.EmployeeId))
            {
                var empExists = await _context.StaffEmployments
                    .AnyAsync(e => e.EmployeeId == dto.EmployeeId);

                if (empExists)
                    throw new Exception("Employee ID already exists.");

                var department = Enum.TryParse<Department>(dto.Department, out var dep)
                    ? dep : Department.Sales;
                var accessLevel = Enum.TryParse<AccessLevel>(dto.AccessLevel, out var al)
                    ? al : AccessLevel.Staff;

                _context.StaffEmployments.Add(new StaffEmployment
                {
                    UserId = user.Id,
                    EmployeeId = dto.EmployeeId,
                    Department = department,
                    AccessLevel = accessLevel,
                    BranchLocation = dto.BranchLocation ?? string.Empty,
                    IsApprovedByAdmin = false
                });
            }

            await _context.SaveChangesAsync();

            return await _otpService.GenerateAndSendOtpAsync(
                dto.Email, OtpPurpose.EmailVerification);
        }

        // ══════════════════════════════════════════════════════
        // VERIFY EMAIL OTP — Step 2 of Register
        // ══════════════════════════════════════════════════════
        public async Task<AuthResponseDto> VerifyEmailOtpAsync(VerifyOtpDto dto)
        {
            var isValid = await _otpService.VerifyOtpAsync(
                dto.Email, dto.OtpCode, OtpPurpose.EmailVerification);

            if (!isValid)
                throw new Exception("Invalid or expired OTP.");

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == dto.Email)
                ?? throw new Exception("User not found.");

            user.IsEmailVerified = true;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Send welcome email
            await _emailService.SendWelcomeEmailAsync(user.Email, user.FullName);

            var token = _jwtService.GenerateToken(user);

            return new AuthResponseDto
            {
                Token = token,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role.ToString(),
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };
        }

        // ══════════════════════════════════════════════════════
        // LOGIN — Step 1
        // ══════════════════════════════════════════════════════
        public async Task<string> LoginAsync(LoginDto dto)
        {
            var user = await _context.Users
                .Include(u => u.StaffEmployment)
                .FirstOrDefaultAsync(u => u.Email == dto.Email)
                ?? throw new Exception("Invalid email or password.");

            if (!user.IsEmailVerified)
                throw new Exception("Please verify your email before logging in.");

            if (user.Status == StatusType.Inactive)
                throw new Exception("Your account has been deactivated. Contact admin.");

            // Staff must be approved by admin
            if (user.Role == RoleType.Staff &&
                user.StaffEmployment != null &&
                !user.StaffEmployment.IsApprovedByAdmin)
                throw new Exception("Your staff account is pending admin approval.");

            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                throw new Exception("Invalid email or password.");

            return await _otpService.GenerateAndSendOtpAsync(
                dto.Email, OtpPurpose.Login);
        }

        // ══════════════════════════════════════════════════════
        // VERIFY LOGIN OTP — Step 2 of Login
        // ══════════════════════════════════════════════════════
        public async Task<AuthResponseDto> VerifyLoginOtpAsync(VerifyOtpDto dto)
        {
            var isValid = await _otpService.VerifyOtpAsync(
                dto.Email, dto.OtpCode, OtpPurpose.Login);

            if (!isValid)
                throw new Exception("Invalid or expired OTP.");

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == dto.Email)
                ?? throw new Exception("User not found.");

            var token = _jwtService.GenerateToken(user);

            return new AuthResponseDto
            {
                Token = token,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role.ToString(),
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };
        }

        // ══════════════════════════════════════════════════════
        // RESEND OTP
        // ══════════════════════════════════════════════════════
        public async Task<string> ResendOtpAsync(ResendOtpDto dto)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == dto.Email)
                ?? throw new Exception("User not found.");

            var purpose = dto.Purpose switch
            {
                "EmailVerification" => OtpPurpose.EmailVerification,
                "Login" => OtpPurpose.Login,
                "PasswordReset" => OtpPurpose.PasswordReset,
                _ => throw new Exception("Invalid OTP purpose.")
            };

            return await _otpService.GenerateAndSendOtpAsync(dto.Email, purpose);
        }

        // ══════════════════════════════════════════════════════
        // FORGOT PASSWORD
        // ══════════════════════════════════════════════════════
        public async Task<string> ForgotPasswordAsync(ForgotPasswordDto dto)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == dto.Email)
                ?? throw new Exception("No account found with this email.");

            if (!user.IsEmailVerified)
                throw new Exception("Please verify your email first.");

            return await _otpService.GenerateAndSendOtpAsync(
                dto.Email, OtpPurpose.PasswordReset);
        }

        // ══════════════════════════════════════════════════════
        // RESET PASSWORD
        // ══════════════════════════════════════════════════════
        public async Task<string> ResetPasswordAsync(ResetPasswordDto dto)
        {
            if (dto.NewPassword != dto.ConfirmNewPassword)
                throw new Exception("Passwords do not match.");

            var isValid = await _otpService.VerifyOtpAsync(
                dto.Email, dto.OtpCode, OtpPurpose.PasswordReset);

            if (!isValid)
                throw new Exception("Invalid or expired OTP.");

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == dto.Email)
                ?? throw new Exception("User not found.");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return "Password reset successfully.";
        }

        // ══════════════════════════════════════════════════════
        // GET CURRENT USER
        // ══════════════════════════════════════════════════════
        public async Task<AuthResponseDto> GetCurrentUserAsync(string email)
        {
            var user = await _context.Users
                .Include(u => u.Vehicles)
                .Include(u => u.StaffEmployment)
                .FirstOrDefaultAsync(u => u.Email == email)
                ?? throw new Exception("User not found.");

            var token = _jwtService.GenerateToken(user);

            return new AuthResponseDto
            {
                Token = token,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role.ToString(),
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };
        }
    }
}