using System.ComponentModel.DataAnnotations;

namespace Autopartspro.Application.Dtos;

public record InvoiceItemDto(
    Guid id,
    Guid PartId,
    string? PartName,
    int Quantity,
    decimal UnitPrice,
    decimal SubTotal
);

public record InvoiceDto(
    Guid id,
    string InvoiceNumber,
    Guid CustomerId,
    string CustomerName,
    Guid StaffId,
    string StaffName,
    DateTime SaleDate,
    decimal SubTotal,
    decimal DiscountAmount,
    decimal TotalAmount,
    string PaymentStatus,
    List<InvoiceItemDto> Items
);

public class InvoiceItemCreateDto
{
    public Guid PartId { get; set; }
    
    [Range(1, 1000)]
    public int Quantity { get; set; }
}

public class InvoiceCreateDto
{
    public Guid CustomerId { get; set; }
    
    public Guid? StaffId { get; set; }
    
    [Range(0, 100000)]
    public decimal DiscountAmount { get; set; }
    
    [Required]
    public string PaymentStatus { get; set; } = "Paid";
    
    public List<InvoiceItemCreateDto> Items { get; set; } = new();
}
