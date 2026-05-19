using Autopartspro.Application.Dtos.Auth;
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

       
        // REGISTER — Step 1
      
        public async Task<string> RegisterAsync(RegisterDto dto)
        {
            if (!dto.AgreeToTerms)
                throw new ArgumentException("You must agree to the Terms of Service and Privacy Policy.");

            if (string.IsNullOrWhiteSpace(dto.Email))
                throw new ArgumentException("Email is required.");

            if (dto.Password != dto.ConfirmPassword)
                throw new ArgumentException("Passwords do not match.");

            var email = dto.Email.Trim().ToLowerInvariant();
            var existingUser = await _context.Users
                .Include(u => u.Vehicles)
                .Include(u => u.StaffEmployment)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email);

            if (existingUser != null && existingUser.IsEmailVerified)
                throw new ArgumentException("Email is already registered.");

            var role = dto.Role?.Equals("Staff", StringComparison.OrdinalIgnoreCase) == true
                ? RoleType.Staff
                : RoleType.Customer;

            if (role == RoleType.Customer)
            {
                if (string.IsNullOrWhiteSpace(dto.NumberPlate))
                    throw new ArgumentException("Vehicle number plate is required.");

                var plate = dto.NumberPlate.Trim();
                var plateTaken = await _context.Vehicles.AnyAsync(v =>
                    v.NumberPlate.ToLower() == plate.ToLower() &&
                    (existingUser == null || v.CustomerId != existingUser.Id));

                if (plateTaken)
                    throw new ArgumentException("This number plate is already registered to another account.");
            }

            if (role == RoleType.Staff)
            {
                if (string.IsNullOrWhiteSpace(dto.EmployeeId))
                    throw new ArgumentException("Employee ID is required for staff registration.");

                var empExists = await _context.StaffEmployments
                    .AnyAsync(e => e.EmployeeId == dto.EmployeeId.Trim());

                if (empExists)
                    throw new ArgumentException("Employee ID already exists.");
            }

            if (existingUser != null && !existingUser.IsEmailVerified)
                await RemoveUnverifiedUserAsync(existingUser);

            var user = new User
            {
                FullName = dto.FullName.Trim(),
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                PhoneNumber = dto.PhoneNumber?.Trim() ?? string.Empty,
                City = dto.City?.Trim() ?? string.Empty,
                Role = role,
                Status = StatusType.Active,
                IsEmailVerified = false
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            if (role == RoleType.Customer)
            {
                var fuelType = Enum.TryParse<FuelType>(dto.FuelType, true, out var ft)
                    ? ft : FuelType.Petrol;

                _context.Vehicles.Add(new Vehicle
                {
                    CustomerId = user.Id,
                    Make = dto.Make?.Trim() ?? string.Empty,
                    Model = dto.Model?.Trim() ?? string.Empty,
                    Year = dto.Year ?? 0,
                    FuelType = fuelType,
                    NumberPlate = dto.NumberPlate!.Trim()
                });
            }

            if (role == RoleType.Staff)
            {
                var accessLevel = Enum.TryParse<AccessLevel>(dto.AccessLevel, true, out var al)
                    ? al : AccessLevel.Staff;

                _context.StaffEmployments.Add(new StaffEmployment
                {
                    UserId = user.Id,
                    EmployeeId = dto.EmployeeId!.Trim(),
                    Department = ParseDepartment(dto.Department),
                    AccessLevel = accessLevel,
                    BranchLocation = dto.BranchLocation?.Trim() ?? string.Empty,
                    IsApprovedByAdmin = false
                });
            }

            await _context.SaveChangesAsync();

            return await _otpService.GenerateAndSendOtpAsync(
                email, OtpPurpose.EmailVerification);
        }

        private async Task RemoveUnverifiedUserAsync(User user)
        {
            var vehicles = await _context.Vehicles
                .Where(v => v.CustomerId == user.Id)
                .ToListAsync();
            if (vehicles.Count > 0)
                _context.Vehicles.RemoveRange(vehicles);

            if (user.StaffEmployment != null)
                _context.StaffEmployments.Remove(user.StaffEmployment);

            var otps = await _context.OtpVerifications
                .Where(o => o.Email == user.Email)
                .ToListAsync();
            if (otps.Count > 0)
                _context.OtpVerifications.RemoveRange(otps);

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
        }

        private static Department ParseDepartment(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return Department.Sales;

            if (value.Equals("Mechanic", StringComparison.OrdinalIgnoreCase))
                return Department.Service;

            return Enum.TryParse<Department>(value, true, out var dep)
                ? dep
                : Department.Sales;
        }

        // ══════════════════════════════════════════════════════
        // VERIFY EMAIL OTP — Step 2 of Register
        // ══════════════════════════════════════════════════════
        public async Task<AuthResponseDto> VerifyEmailOtpAsync(VerifyOtpDto dto)
        {
            var email = dto.Email.Trim().ToLowerInvariant();

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email)
                ?? throw new ArgumentException("User not found.");

            // Already verified (e.g. double-click or repeat request) — return token without OTP
            if (user.IsEmailVerified)
                return BuildAuthResponse(user);

            var isValid = await _otpService.VerifyOtpAsync(
                email, dto.OtpCode, OtpPurpose.EmailVerification);

            if (!isValid)
                throw new ArgumentException("Invalid or expired OTP. Tap Resend to get a new code.");

            user.IsEmailVerified = true;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            try
            {
                await _emailService.SendWelcomeEmailAsync(user.Email, user.FullName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Welcome email failed for {user.Email}: {ex.Message}");
            }

            return BuildAuthResponse(user);
        }

        // LOGIN — email + password → JWT (no OTP on login)
        public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
        {
            var email = dto.Email.Trim().ToLowerInvariant();
            var user = await _context.Users
                .Include(u => u.StaffEmployment)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email)
                ?? throw new ArgumentException("Invalid email or password.");

            if (!user.IsEmailVerified)
                throw new ArgumentException("Please verify your email before logging in.");

            if (user.Status == StatusType.Inactive)
                throw new ArgumentException("Your account has been deactivated. Contact admin.");

            if (user.Role == RoleType.Staff &&
                user.StaffEmployment != null &&
                !user.StaffEmployment.IsApprovedByAdmin)
                throw new ArgumentException("Your staff account is pending admin approval.");

            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                throw new ArgumentException("Invalid email or password.");

            if (!string.IsNullOrWhiteSpace(dto.Role))
            {
                var expectedRole = ParseExpectedRole(dto.Role);
                if (user.Role != expectedRole)
                {
                    throw new ArgumentException(
                        $"This account is registered as {user.Role}. Please use the {user.Role} login tab.");
                }
            }

            return BuildAuthResponse(user);
        }

        private static RoleType ParseExpectedRole(string role) => role.Trim().ToLowerInvariant() switch
        {
            "admin" => RoleType.Admin,
            "staff" => RoleType.Staff,
            _ => RoleType.Customer
        };

        private AuthResponseDto BuildAuthResponse(User user)
        {
            var token = _jwtService.GenerateToken(user);
            return new AuthResponseDto
            {
                Token = token,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role.ToString(),
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                MustChangePassword = user.MustChangePassword,
            };
        }

        
        // VERIFY LOGIN OTP — Step 2 of Login
     
        public async Task<AuthResponseDto> VerifyLoginOtpAsync(VerifyOtpDto dto)
        {
            var isValid = await _otpService.VerifyOtpAsync(
                dto.Email, dto.OtpCode, OtpPurpose.Login);

            if (!isValid)
                throw new ArgumentException("Invalid or expired OTP.");

            var email = dto.Email.Trim().ToLowerInvariant();
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email)
                ?? throw new ArgumentException("User not found.");

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

       
        // RESEND OTP
        
        public async Task<string> ResendOtpAsync(ResendOtpDto dto)
        {
            var email = dto.Email.Trim().ToLowerInvariant();
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email)
                ?? throw new ArgumentException("User not found.");

            var purpose = dto.Purpose switch
            {
                "EmailVerification" => OtpPurpose.EmailVerification,
                "Login" => OtpPurpose.Login,
                "PasswordReset" => OtpPurpose.PasswordReset,
                _ => throw new ArgumentException("Invalid OTP purpose.")
            };

            if (purpose == OtpPurpose.EmailVerification && user.IsEmailVerified)
                return "Your email is already verified. You can sign in.";

            return await _otpService.GenerateAndSendOtpAsync(email, purpose);
        }

        // ══════════════════════════════════════════════════════
        // FORGOT PASSWORD
        // ══════════════════════════════════════════════════════
        public async Task<string> ForgotPasswordAsync(ForgotPasswordDto dto)
        {
            var email = dto.Email.Trim().ToLowerInvariant();
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email)
                ?? throw new ArgumentException("No account found with this email.");

            if (!user.IsEmailVerified)
                throw new ArgumentException("Please verify your email before resetting your password.");

            return await _otpService.GenerateAndSendOtpAsync(
                email, OtpPurpose.PasswordReset);
        }

        // RESET PASSWORD — verify OTP and update hash in database
        public async Task<string> ResetPasswordAsync(ResetPasswordDto dto)
        {
            if (dto.NewPassword != dto.ConfirmNewPassword)
                throw new ArgumentException("Passwords do not match.");

            var email = dto.Email.Trim().ToLowerInvariant();

            var isValid = await _otpService.VerifyOtpAsync(
                email, dto.OtpCode, OtpPurpose.PasswordReset);

            if (!isValid)
                throw new ArgumentException("Invalid or expired OTP.");

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email)
                ?? throw new ArgumentException("User not found.");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return "Password reset successfully. You can now sign in with your new password.";
        }

        // ══════════════════════════════════════════════════════
        // GET CURRENT USER
        // ══════════════════════════════════════════════════════
        public async Task<AuthResponseDto> GetCurrentUserAsync(string email)
        {
            var normalized = email.Trim().ToLowerInvariant();
            var user = await _context.Users
                .Include(u => u.Vehicles)
                .Include(u => u.StaffEmployment)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == normalized)
                ?? throw new ArgumentException("User not found.");

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