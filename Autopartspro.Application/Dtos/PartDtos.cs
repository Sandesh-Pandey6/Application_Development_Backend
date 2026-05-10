using System.ComponentModel.DataAnnotations;

namespace Autopartspro.Application.Dtos;

public record PartDto(
    int Id,
    string Name,
    string? PartCode,
    string? Description,
    decimal Price,
    int StockQuantity,
    int? VendorId,
    string? VendorName
);

public class PartUpsertDto
{
    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(80)]
    public string? PartCode { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    [Range(0, 9999999.99)]
    public decimal Price { get; set; }

    [Range(0, int.MaxValue)]
    public int StockQuantity { get; set; }

    public int? VendorId { get; set; }
}
