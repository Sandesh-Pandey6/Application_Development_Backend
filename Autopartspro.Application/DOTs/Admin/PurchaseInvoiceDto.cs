namespace Autopartspro.Application.DOTs.admin
{
    public class CreatePurchaseInvoiceDto
    {
        public Guid VendorId { get; set; }
        public DateTime PurchaseDate { get; set; }
        public List<PurchaseInvoiceItemDto> Items { get; set; } = new();
    }

    public class PurchaseInvoiceItemDto
    {
        public Guid PartId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public class PurchaseInvoiceResponseDto
    {
        public Guid Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public string VendorName { get; set; } = string.Empty;
        public Guid VendorId { get; set; }
        public int TotalItems { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime PurchaseDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<PurchaseInvoiceItemResponseDto> Items { get; set; } = new();
    }

    public class PurchaseInvoiceItemResponseDto
    {
        public string PartName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal SubTotal { get; set; }
    }

    public class PurchaseInvoiceListResponseDto
    {
        public int TotalInvoices { get; set; }
        public decimal TotalValue { get; set; }
        public int Completed { get; set; }
        public int PendingOrProcessing { get; set; }
        public List<PurchaseInvoiceResponseDto> Invoices { get; set; } = new();
    }
}