namespace Autopartspro.Domain.Entities
{
    public class Part : BaseEntity
    {
        public string PartName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public string? ImageUrl { get; set; }
        public Guid VendorId { get; set; }

        // Navigation Properties
        public Vendor Vendor { get; set; } = null!;
        public ICollection<PurchaseInvoiceItem> PurchaseInvoiceItems { get; set; } = new List<PurchaseInvoiceItem>();
        public ICollection<SalesInvoiceItem> SalesInvoiceItems { get; set; } = new List<SalesInvoiceItem>();
    }
}