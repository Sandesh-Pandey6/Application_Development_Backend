namespace Autopartspro.Application.Dtos.Auth
{
    public class UserProfileDto
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool IsEmailVerified { get; set; }
        public bool MustChangePassword { get; set; }
        public string? ProfileImageUrl { get; set; }

        // Customer only
        public List<VehicleDto> Vehicles { get; set; } = new();

        // Staff only
        public StaffEmploymentDto? Employment { get; set; }
    }

    public class VehicleDto
    {
        public Guid Id { get; set; }
        public string Make { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public int Year { get; set; }
        public string FuelType { get; set; } = string.Empty;
        public string NumberPlate { get; set; } = string.Empty;
    }

    public class StaffEmploymentDto
    {
        public string EmployeeId { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string AccessLevel { get; set; } = string.Empty;
        public string BranchLocation { get; set; } = string.Empty;
        public bool IsApprovedByAdmin { get; set; }
    }
}