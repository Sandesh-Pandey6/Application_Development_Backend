namespace Autopartspro.Domain.Entities
{
    public class SalesInvoiceItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid SalesInvoiceId { get; set; }
        public Guid PartId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal SubTotal { get; set; }

        // Navigation Properties
        public SalesInvoice SalesInvoice { get; set; } = null!;
        public Part Part { get; set; } = null!;
    }
}