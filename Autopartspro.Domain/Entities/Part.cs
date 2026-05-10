using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Autopartspro.Domain.Entities;

public class Part
{
    public int Id { get; set; }

    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(80)]
    public string? PartCode { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    [Column(TypeName = "numeric(12,2)")]
    public decimal Price { get; set; }

    public int StockQuantity { get; set; }

    public int? VendorId { get; set; }
    public Vendor? Vendor { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
