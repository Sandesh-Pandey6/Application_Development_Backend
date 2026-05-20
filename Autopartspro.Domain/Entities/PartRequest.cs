using Autopartspro.Domain.Enums;

namespace Autopartspro.Domain.Entities
{
    public class PartRequest : BaseEntity
    {
        public Guid CustomerId { get; set; }
        public string PartName { get; set; } = string.Empty;
        public string PartDescription { get; set; } = string.Empty;
        public string VehicleModel { get; set; } = string.Empty;
        public UrgencyLevel UrgencyLevel { get; set; }
        public PartRequestStatus Status { get; set; } = PartRequestStatus.Pending;
        ///When staff expects the part to be available.
        public DateOnly? EstimatedAvailableDate { get; set; }
        public string? StaffNotes { get; set; }
        public DateTime? StaffRespondedAt { get; set; }
        public DateTime? EscalatedAt { get; set; }
        public Guid? VendorId { get; set; }
        public DateTime? VendorRequestedAt { get; set; }
        public string? VendorRequestMessage { get; set; }
        public Guid? PurchaseInvoiceId { get; set; }
        public DateTime? InvoiceRecordedAt { get; set; }

        // Navigation Properties
        public User Customer { get; set; } = null!;
        public Vendor? Vendor { get; set; }
        public PurchaseInvoice? PurchaseInvoice { get; set; }
    }
}