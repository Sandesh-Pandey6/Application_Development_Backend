using Autopartspro.Domain.Enums;

namespace Autopartspro.Domain.Entities
{
    public class StaffEmployment : BaseEntity
    {
        public Guid UserId { get; set; }
        public string EmployeeId { get; set; } = string.Empty;  // e.g. EMP-2025-042
        public Department Department { get; set; }               // Sales, Inventory, etc
        public AccessLevel AccessLevel { get; set; }             // Staff, Manager
        public string BranchLocation { get; set; } = string.Empty; // e.g. Kathmandu - Main Branch
        public bool IsApprovedByAdmin { get; set; } = false;     // "reviewed by admin before activation"

        // Navigation Properties
        public User User { get; set; } = null!;
    }
}