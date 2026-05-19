namespace Autopartspro.Application.Dtos.Admin;

public class AdminProfileDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? BusinessEmail { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
}

public class UpdateAdminProfileDto
{
    public string? FullName { get; set; }
    public string? BusinessEmail { get; set; }
    public string? PhoneNumber { get; set; }
    public string? City { get; set; }
}
