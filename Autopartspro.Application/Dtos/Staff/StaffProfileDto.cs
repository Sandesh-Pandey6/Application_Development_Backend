namespace Autopartspro.Application.Dtos.Staff;

public class StaffProfileDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string? ProfileImageUrl { get; set; }
}

public class UpdateStaffProfileDto
{
    public string? FullName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? City { get; set; }
}
