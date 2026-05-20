using System.ComponentModel.DataAnnotations;

namespace Autopartspro.Application.Dtos;

public record InvoiceItemDto(
    Guid Id,
    Guid PartId,
    string? PartName,
    int Quantity,
    decimal UnitPrice,
    decimal Subtotal
);

public record InvoiceDto(
    Guid Id,
    string InvoiceNumber,
    Guid CustomerId,
    string CustomerName,
    string? StaffName,
    DateTime InvoiceDate,
    decimal Subtotal,
    decimal DiscountAmount,
    decimal TotalAmount,
    string PaymentStatus,
    List<InvoiceItemDto> Items,
    Guid? VehicleId = null,
    string? VehicleNumber = null,
    string? VehicleDescription = null
);

public class InvoiceItemCreateDto
{
    [Required]
    public Guid PartId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }
}

public class InvoiceCreateDto
{
    [Required]
    public Guid CustomerId { get; set; }

    public Guid? VehicleId { get; set; }

    [MaxLength(150)]
    public string? StaffName { get; set; }

    [Range(0, 9999999.99)]
    public decimal DiscountAmount { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    [MaxLength(20)]
    public string PaymentStatus { get; set; } = "Paid";

    [MinLength(1)]
    public List<InvoiceItemCreateDto> Items { get; set; } = new();
}
