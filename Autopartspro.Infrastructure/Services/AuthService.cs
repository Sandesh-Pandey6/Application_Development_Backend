using Autopartspro.Application.DTOs.Auth;
using Autopartspro.Application.Interfaces;
using Autopartspro.Domain.Entities;
using Autopartspro.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Autopartspro.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IJwtService _jwt;
    private readonly IEmailService _email;

    public AuthService(AppDbContext db, IJwtService jwt, IEmailService email)
    {
        _db = db;
        _jwt = jwt;
        _email = email;
    }

    // CUSTOMER REGISTER 
    public async Task<RegisterResponseDto> RegisterCustomerAsync(CustomerRegisterDto dto)
    {
        if (dto.Password != dto.ConfirmPassword)
            throw new ArgumentException("Passwords do not match.");

        if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
            throw new InvalidOperationException("Email is already registered.");

        var user = new User
        {
            FullName = dto.FullName,
            Email = dto.Email,
            Phone = dto.Phone,
            City = dto.City,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = "Customer",
            IsEmailVerified = false,
            IsActive = false
        };

        var customerProfile = new CustomerProfile { User = user };

        var vehicle = new Vehicle
        {
            CustomerProfile = customerProfile,
            Make = dto.VehicleMake,
            Model = dto.VehicleModel,
            Year = dto.VehicleYear,
            FuelType = dto.FuelType,
            NumberPlate = dto.NumberPlate,
            IsPrimary = true
        };

        customerProfile.Vehicles.Add(vehicle);

        await _db.Users.AddAsync(user);
        await _db.CustomerProfiles.AddAsync(customerProfile);
        await _db.SaveChangesAsync();

        await SendAndSaveOtpAsync(user);

        return new RegisterResponseDto
        {
            Message = "Registration successful. Check your email for the verification code.",
            Email = user.Email,
            RequiresEmailVerification = true
        };
    }

    // STAFF REGISTER 
    public async Task<RegisterResponseDto> RegisterStaffAsync(StaffRegisterDto dto)
    {
        if (dto.Password != dto.ConfirmPassword)
            throw new ArgumentException("Passwords do not match.");

        if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
            throw new InvalidOperationException("Email is already registered.");

        if (await _db.StaffProfiles.AnyAsync(s => s.EmployeeId == dto.EmployeeId))
            throw new InvalidOperationException("Employee ID is already in use.");

        var user = new User
        {
            FullName = dto.FullName,
            Email = dto.Email,
            Phone = dto.Phone,
            City = dto.City,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = "Staff",
            IsEmailVerified = false,
            IsActive = false // Admin must approve staff
        };

        var staffProfile = new StaffProfile
        {
            User = user,
            EmployeeId = dto.EmployeeId,
            Department = dto.Department,
            AccessLevel = dto.AccessLevel,
            Branch = dto.Branch,
            IsApprovedByAdmin = false
        };

        await _db.Users.AddAsync(user);
        await _db.StaffProfiles.AddAsync(staffProfile);
        await _db.SaveChangesAsync();

        await SendAndSaveOtpAsync(user);

        return new RegisterResponseDto
        {
            Message = "Registration successful. Verify your email, then wait for admin approval.",
            Email = user.Email,
            RequiresEmailVerification = true
        };
    }

    //  LOGIN 
    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == dto.Email && u.Role == dto.Role)
            ?? throw new UnauthorizedAccessException("Invalid email, password, or role.");

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email, password, or role.");

        if (!user.IsEmailVerified)
            throw new UnauthorizedAccessException("Please verify your email before logging in.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Your account is pending admin approval.");

        var token = _jwt.GenerateToken(user);
        var refreshToken = _jwt.GenerateRefreshToken();

        return new AuthResponseDto
        {
            Token = token,
            RefreshToken = refreshToken,
            Role = user.Role,
            FullName = user.FullName,
            Email = user.Email,
            ExpiresAt = DateTime.UtcNow.AddMinutes(60)
        };
    }

    //  VERIFY OTP
    public async Task<bool> VerifyOtpAsync(VerifyOtpDto dto)
    {
        var user = await _db.Users
            .Include(u => u.OtpCode)
            .FirstOrDefaultAsync(u => u.Email == dto.Email)
            ?? throw new KeyNotFoundException("User not found.");

        var otp = user.OtpCode;

        if (otp is null || otp.IsUsed || otp.Code != dto.Code)
            throw new InvalidOperationException("Invalid or expired OTP code.");

        if (otp.ExpiresAt < DateTime.UtcNow)
            throw new InvalidOperationException("OTP code has expired. Please request a new one.");

        otp.IsUsed = true;
        user.IsEmailVerified = true;

        // Auto-activate customers; staff need admin approval
        if (user.Role == "Customer")
            user.IsActive = true;

        await _db.SaveChangesAsync();
        return true;
    }

    //  RESEND OTP 
    public async Task<bool> ResendOtpAsync(ResendOtpDto dto)
    {
        var user = await _db.Users
            .Include(u => u.OtpCode)
            .FirstOrDefaultAsync(u => u.Email == dto.Email)
            ?? throw new KeyNotFoundException("User not found.");

        if (user.IsEmailVerified)
            throw new InvalidOperationException("Email is already verified.");

        // Remove old OTP if exists
        if (user.OtpCode is not null)
            _db.OtpCodes.Remove(user.OtpCode);

        await _db.SaveChangesAsync();
        await SendAndSaveOtpAsync(user);
        return true;
    }

    // HELPER: Generate, Save & Email OTP 
    private async Task SendAndSaveOtpAsync(User user)
    {
        var code = GenerateOtpCode();

        var otpEntry = new OtpCode
        {
            UserId = user.Id,
            Code = code,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            IsUsed = false
        };

        await _db.OtpCodes.AddAsync(otpEntry);
        await _db.SaveChangesAsync();

        await _email.SendOtpEmailAsync(user.Email, user.FullName, code);
    }

    private static string GenerateOtpCode()
    {
        var rng = new Random();
        return rng.Next(100000, 999999).ToString();
    }
}