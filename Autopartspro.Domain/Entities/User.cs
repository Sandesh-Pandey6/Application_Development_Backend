using Autopartspro.Domain.Enums;

namespace Autopartspro.Domain.Entities
{
    public class User : BaseEntity
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        /// <summary>Public contact email for admin (e.g. customer inquiries). Separate from login email.</summary>
        public string? BusinessEmail { get; set; }
        public string PasswordHash { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public RoleType Role { get; set; }
        public StatusType Status { get; set; } = StatusType.Active;
        public bool IsEmailVerified { get; set; } = false;
        /// <summary>True when staff registered the account with a temporary default password.</summary>
        public bool MustChangePassword { get; set; }

        // Navigation Properties
        public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
        public StaffEmployment? StaffEmployment { get; set; }
        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
        public ICollection<PartRequest> PartRequests { get; set; } = new List<PartRequest>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        public ICollection<SalesInvoice> CustomerInvoices { get; set; } = new List<SalesInvoice>();
        public ICollection<SalesInvoice> StaffInvoices { get; set; } = new List<SalesInvoice>();
        public ICollection<PurchaseInvoice> PurchaseInvoices { get; set; } = new List<PurchaseInvoice>();
        public ICollection<OtpVerification> OtpVerifications { get; set; } = new List<OtpVerification>();

    }
}