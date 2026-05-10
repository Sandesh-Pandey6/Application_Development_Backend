namespace Autopartspro.Application.DOTs.auth
{
    public class RegisterDto
    {
        // ── Step 1: Personal Info (both Customer & Staff) ──
        public string Role { get; set; } = "Customer"; // "Customer" or "Staff"
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
        public bool AgreeToTerms { get; set; }

        // ── Step 2: Vehicle Info (Customer only) ──
        public string? Make { get; set; }           // e.g. Honda
        public string? Model { get; set; }          // e.g. City
        public int? Year { get; set; }              // e.g. 2020
        public string? FuelType { get; set; }       // Petrol, Diesel, etc
        public string? NumberPlate { get; set; }    // e.g. BA 1 Kha 2345

        // ── Step 2: Employment Info (Staff only) ──
        public string? EmployeeId { get; set; }        // e.g. EMP-2025-042
        public string? Department { get; set; }        // Sales, Inventory, etc
        public string? AccessLevel { get; set; }       // Staff, Manager
        public string? BranchLocation { get; set; }    // e.g. Kathmandu - Main Branch
    }
}