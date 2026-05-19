namespace Autopartspro.Application.Dtos.PartRequests;

public class SetPartAvailabilityDto
{
    public string Date { get; set; } = string.Empty;
    public string? Message { get; set; }
}

public class RejectPartRequestDto
{
    public string? Message { get; set; }
}

public class EscalatePartRequestDto
{
    public string? Message { get; set; }
}

public class RequestVendorForPartDto
{
    public Guid VendorId { get; set; }
    public string? Message { get; set; }
    public int Quantity { get; set; } = 1;
}

public class RecordPartRequestVendorInvoiceDto
{
    public string VendorInvoiceNumber { get; set; } = string.Empty;
    public Guid VendorId { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
}
