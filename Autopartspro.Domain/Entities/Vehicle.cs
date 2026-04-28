namespace Autopartspro.Domain.Entities;

public class Vehicle
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CustomerProfileId { get; set; }
    public string Make { get; set; } = string.Empty;       // e.g. Honda
    public string Model { get; set; } = string.Empty;      // e.g. City
    public int Year { get; set; }
    public string FuelType { get; set; } = string.Empty;   // Petrol, Diesel, Electric, Hybrid
    public string NumberPlate { get; set; } = string.Empty; // e.g. BA 1 Kha 2345
    public bool IsPrimary { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public CustomerProfile CustomerProfile { get; set; } = null!;
}