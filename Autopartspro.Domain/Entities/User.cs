namespace Autopartspro.Domain.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty; // "Admin", "Staff", "Customer"
    public bool IsEmailVerified { get; set; } = false;
    public bool IsActive { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public CustomerProfile? CustomerProfile { get; set; }
    public StaffProfile? StaffProfile { get; set; }
    public OtpCode? OtpCode { get; set; }
}