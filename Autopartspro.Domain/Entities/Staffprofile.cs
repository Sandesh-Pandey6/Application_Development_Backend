namespace Autopartspro.Domain.Entities;

public class StaffProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string EmployeeId { get; set; } = string.Empty;   // e.g. EMP-2025-042
    public string Department { get; set; } = string.Empty;   // Sales, Service, etc.
    public string AccessLevel { get; set; } = string.Empty;  // Staff, Manager
    public string Branch { get; set; } = string.Empty;       // Kathmandu - Main Branch
    public bool IsApprovedByAdmin { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User User { get; set; } = null!;
}