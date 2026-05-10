using System.ComponentModel.DataAnnotations;

namespace Autopartspro.Application.Dtos;

public record VehicleDto(
    int Id,
    int CustomerId,
    string VehicleNumber,
    string? Make,
    string? Model,
    int? Year,
    string? VehicleType,
    string? Color
);

public class VehicleUpsertDto
{
    [Required, MaxLength(30)]
    public string VehicleNumber { get; set; } = string.Empty;

    [MaxLength(80)]
    public string? Make { get; set; }

    [MaxLength(80)]
    public string? Model { get; set; }

    public int? Year { get; set; }

    [MaxLength(40)]
    public string? VehicleType { get; set; }

    [MaxLength(40)]
    public string? Color { get; set; }
}

public record CustomerListItemDto(
    int Id,
    string FullName,
    string Phone,
    string? Email,
    int VehicleCount,
    DateTime CreatedAt
);

public record CustomerDetailDto(
    int Id,
    string FullName,
    string Phone,
    string? Email,
    string? Address,
    string? NationalId,
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

    [MaxLength(300)]
    public string? Address { get; set; }

    [MaxLength(40)]
    public string? NationalId { get; set; }

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

    [MaxLength(300)]
    public string? Address { get; set; }

    [MaxLength(40)]
    public string? NationalId { get; set; }
}

public record InvoiceSummaryDto(
    int Id,
    string InvoiceNumber,
    DateTime InvoiceDate,
    decimal TotalAmount,
    string PaymentStatus,
    int ItemCount,
    string? VehicleNumber
);

public record CustomerHistoryDto(
    CustomerDetailDto Customer,
    List<InvoiceSummaryDto> Invoices,
    decimal TotalSpent,
    int InvoiceCount
);
