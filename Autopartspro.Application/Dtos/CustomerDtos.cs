using System.ComponentModel.DataAnnotations;

namespace Autopartspro.Application.Dtos;

public record VehicleDto(
    Guid id,
    Guid customerId,
    string NumberPlate,
    string? Make,
    string? Model,
    int? Year,
    string? FuelType
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

    [MaxLength(40)]
    public string? FuelType { get; set; }
}

public record CustomerListItemDto(
    Guid id,
    string FullName,
    string PhoneNumber,
    string? Email,
    int VehicleCount,
    DateTime CreatedAt
);

public record CustomerDetailDto(
    Guid id,
    string FullName,
    string PhoneNumber,
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
    public string PhoneNumber { get; set; } = string.Empty;

    [MaxLength(150), EmailAddress]
    public string? Email { get; set; }

    [MaxLength(300)]
    public string? City { get; set; }

    public List<VehicleUpsertDto> Vehicles { get; set; } = new();
}

public class CustomerUpdateDto
{
    [Required, MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required, MaxLength(30)]
    public string PhoneNumber { get; set; } = string.Empty;

    [MaxLength(150), EmailAddress]
    public string? Email { get; set; }

    [MaxLength(300)]
    public string? City { get; set; }
}

public record InvoiceSummaryDto(
    Guid id,
    string InvoiceNumber,
    DateTime SaleDate,
    decimal TotalAmount,
    string PaymentStatus,
    int ItemCount,
    string? NumberPlate
);

public record CustomerHistoryDto(
    CustomerDetailDto Customer,
    List<InvoiceSummaryDto> Invoices,
    decimal TotalSpent,
    int InvoiceCount
);
