using System.ComponentModel.DataAnnotations;

namespace Autopartspro.Domain.Entities;

public class Vehicle
{
    public int Id { get; set; }

    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

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

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
