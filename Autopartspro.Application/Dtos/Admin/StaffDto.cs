namespace Autopartspro.Application.Dtos.Admin
{
    public class CreateStaffDto
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string EmployeeId { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string AccessLevel { get; set; } = string.Empty;
        public string BranchLocation { get; set; } = string.Empty;
    }

    public class UpdateStaffDto
    {
        public string FullName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string AccessLevel { get; set; } = string.Empty;
        public string BranchLocation { get; set; } = string.Empty;
        /// <summary>When true, approves staff for login. When false, revokes approval.</summary>
        public bool? IsApprovedByAdmin { get; set; }
    }

    public class StaffResponseDto
    {
        public Guid Id { get; set; }
        public string StaffId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string AccessLevel { get; set; } = string.Empty;
        public string BranchLocation { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool IsApprovedByAdmin { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class StaffListResponseDto
    {
        public int TotalStaff { get; set; }
        public int ActiveStaff { get; set; }
        public int PendingApproval { get; set; }
        public int Managers { get; set; }
        public int InactiveStaff { get; set; }
        public List<StaffResponseDto> Staff { get; set; } = new();
    }
}