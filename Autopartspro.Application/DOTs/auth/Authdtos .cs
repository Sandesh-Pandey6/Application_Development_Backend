namespace Autopartspro.Application.DTOs.Auth;

//  CUSTOMER REGISTER 

public class CustomerRegisterDto
{
    // Step 1 - Personal Info
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;

    // Step 2 - Vehicle Info
    public string VehicleMake { get; set; } = string.Empty;
    public string VehicleModel { get; set; } = string.Empty;
    public int VehicleYear { get; set; }
    public string FuelType { get; set; } = string.Empty;
    public string NumberPlate { get; set; } = string.Empty;
}

// STAFF REGISTER 

public class StaffRegisterDto
{
    // Step 1 - Personal Info
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;

    // Step 2 - Employment Info
    public string EmployeeId { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string AccessLevel { get; set; } = string.Empty;
    public string Branch { get; set; } = string.Empty;
}

//  LOGIN 

public class LoginDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty; // "Customer", "Staff", "Admin"
    public bool RememberMe { get; set; } = false;
}

// OTP 

public class VerifyOtpDto
{
    public string Email { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}

public class ResendOtpDto
{
    public string Email { get; set; } = string.Empty;
}

//  RESPONSES 

public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

public class RegisterResponseDto
{
    public string Message { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool RequiresEmailVerification { get; set; } = true;
}