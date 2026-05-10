using System.ComponentModel.DataAnnotations;

namespace Autopartspro.Application.Dtos;

public record InvoiceItemDto(
    int Id,
    int PartId,
    string PartName,
    int Quantity,
    decimal UnitPrice,
    decimal Subtotal
);

public record InvoiceDto(
    int Id,
    string InvoiceNumber,
    int CustomerId,
    string CustomerName,
    int? VehicleId,
    string? VehicleNumber,
    string? StaffName,
    DateTime InvoiceDate,
    decimal Subtotal,
    decimal DiscountAmount,
    decimal TotalAmount,
    string PaymentStatus,
    string? Notes,
    List<InvoiceItemDto> Items
);

public class InvoiceItemCreateDto
{
    [Required]
    public int PartId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }
}

public class InvoiceCreateDto
{
    [Required]
    public int CustomerId { get; set; }

    public int? VehicleId { get; set; }

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
