using System.ComponentModel.DataAnnotations;
using Autopartspro.Domain.Enums;

namespace Autopartspro.Application.Dtos;

public record VehicleDto(
    Guid Id,
    Guid CustomerId,
    string NumberPlate,
    string Make,
    string Model,
    int Year,
    string FuelType
);

public class VehicleUpsertDto
{
    [Required, MaxLength(30)]
    public string NumberPlate { get; set; } = string.Empty;

    [MaxLength(80)]
    public string? Make { get; set; }

    [MaxLength(80)]
    public string? Model { get; set; }

    public int? Year { get; set; }

    public string? FuelType { get; set; }
}

public record CustomerListItemDto(
    Guid Id,
    string FullName,
    string Phone,
    string? Email,
    int VehicleCount,
    DateTime CreatedAt
);

public record CustomerDetailDto(
    Guid Id,
    string FullName,
    string Phone,
    string? Email,
    string? City,
    DateTime CreatedAt,
    List<VehicleDto> Vehicles
);

public class CustomerCreateDto
{
    [Required, MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required, MaxLength(30)]
    public string Phone { get; set; } = string.Empty;

    [MaxLength(150), EmailAddress]
    public string? Email { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    public List<VehicleUpsertDto> Vehicles { get; set; } = new();
}

public class CustomerUpdateDto
{
    [Required, MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required, MaxLength(30)]
    public string Phone { get; set; } = string.Empty;

    [MaxLength(150), EmailAddress]
    public string? Email { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }
}

public record InvoiceSummaryDto(
    Guid Id,
    string InvoiceNumber,
    DateTime InvoiceDate,
    decimal TotalAmount,
    string PaymentStatus,
    int ItemCount
);

public record CustomerHistoryDto(
    CustomerDetailDto Customer,
    List<InvoiceSummaryDto> Invoices,
    decimal TotalSpent,
    int InvoiceCount
);
