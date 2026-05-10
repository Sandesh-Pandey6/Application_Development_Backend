namespace Autopartspro.Domain.Entities
{
    public class PurchaseInvoiceItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid PurchaseInvoiceId { get; set; }
        public Guid PartId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal SubTotal { get; set; }

        // Navigation Properties
        public PurchaseInvoice PurchaseInvoice { get; set; } = null!;
        public Part Part { get; set; } = null!;
    }
}