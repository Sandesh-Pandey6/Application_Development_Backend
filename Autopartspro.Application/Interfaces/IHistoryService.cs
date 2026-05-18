namespace Autopartspro.Application.Interfaces;

public interface IHistoryService
{
    Task<(List<PurchaseHistoryDto> items, int total)> GetPurchaseHistoryAsync(string customerId, int pageNumber = 1, int pageSize = 10);
    Task<(List<ServiceHistoryDto> items, int total)> GetServiceHistoryAsync(string customerId, int pageNumber = 1, int pageSize = 10);
    Task<(List<InvoiceHistoryDto> items, int total)> GetInvoiceHistoryAsync(string customerId, int pageNumber = 1, int pageSize = 10);
}

public class PurchaseHistoryDto
{
    public int Id { get; set; }
    public string PartName { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public string Status { get; set; } = null!;
    public DateTime PurchaseDate { get; set; }
}

public class ServiceHistoryDto
{
    public int Id { get; set; }
    public DateTime AppointmentDate { get; set; }
    public string ServiceType { get; set; } = null!;
    public string Status { get; set; } = null!;
    public int? ReviewRating { get; set; }
    public string? ReviewComment { get; set; }
}

public class InvoiceHistoryDto
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = null!;
    public decimal OriginalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FinalAmount { get; set; }
    public string PaymentStatus { get; set; } = null!;
    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }
}
