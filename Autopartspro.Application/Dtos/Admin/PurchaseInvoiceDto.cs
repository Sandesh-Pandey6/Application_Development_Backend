namespace Autopartspro.Application.Dtos.Admin
{
    public class CreatePurchaseInvoiceDto
    {
        /// Vendor's own invoice / bill number (required for admin entry).
        public string? VendorInvoiceNumber { get; set; }

        /// Admin flows may pass an existing vendor id.
        public Guid? VendorId { get; set; }

        /// <summary>Admin-only fallback: create/match vendor by name when VendorId is not set.</summary>
        public string? VendorName { get; set; }

        public string? VendorAddress { get; set; }
        public string? VendorPhone { get; set; }
        public DateTime PurchaseDate { get; set; }
        public List<PurchaseInvoiceItemDto> Items { get; set; } = new();
    }

    public class PurchaseInvoiceItemDto
    {
        /// <summary>Admin flows may pass an existing part id.</summary>
        public Guid? PartId { get; set; }

        /// <summary>Staff entry: product name (required when PartId is not set).</summary>
        public string? ProductName { get; set; }

        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }

        /// <summary>Optional description when auto-creating a part from a part request.</summary>
        public string? Description { get; set; }
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