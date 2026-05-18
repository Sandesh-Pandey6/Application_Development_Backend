using System.ComponentModel.DataAnnotations;

namespace Autopartspro.Application.Dtos;

public record PartDto(
    Guid id,
    string PartName,
    string? PartNumber,
    string? Description,
    decimal Price,
    int StockQuantity,
    Guid VendorId,
    string? VendorName
);

public class PartUpsertDto
{
    [Required, MaxLength(150)]
    public string PartName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? PartNumber { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    [Range(0, 1000000)]
    public decimal Price { get; set; }

    [Range(0, 100000)]
    public int StockQuantity { get; set; }

    public Guid? VendorId { get; set; }
}
