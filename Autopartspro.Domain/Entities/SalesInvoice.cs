using Autopartspro.Domain.Enums;

namespace Autopartspro.Domain.Entities
{
    public class SalesInvoice : BaseEntity
    {
        public string InvoiceNumber { get; set; } = string.Empty;
        public Guid CustomerId { get; set; }
        public Guid? VehicleId { get; set; }
        public Guid StaffId { get; set; }
        public decimal SubTotal { get; set; }
        public bool DiscountApplied { get; set; } = false;
        public decimal DiscountAmount { get; set; } = 0;
        public decimal TotalAmount { get; set; }
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;
        public DateTime SaleDate { get; set; }
        //Set when the 3-day overdue in-app + email reminder was sent.
        public DateTime? OverdueReminderSentAt { get; set; }

        // Navigation Properties
        public User Customer { get; set; } = null!;
        public Vehicle? Vehicle { get; set; }
        public User Staff { get; set; } = null!;
        public ICollection<SalesInvoiceItem> Items { get; set; } = new List<SalesInvoiceItem>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}