namespace Autopartspro.Domain.Entities
{
    public class PurchaseInvoice : BaseEntity
    {
        public string InvoiceNumber { get; set; } = string.Empty;
        public Guid VendorId { get; set; }
        public Guid AdminId { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime PurchaseDate { get; set; }

        // Navigation Properties
        public Vendor Vendor { get; set; } = null!;
        public User Admin { get; set; } = null!;
        public ICollection<PurchaseInvoiceItem> Items { get; set; } = new List<PurchaseInvoiceItem>();
    }
}