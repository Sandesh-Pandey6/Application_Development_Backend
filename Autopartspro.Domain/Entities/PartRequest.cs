namespace Autopartspro.Domain.Entities;

public class PartRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CustomerId { get; set; }
    public string RequestedPartName { get; set; } = string.Empty;
    public string VehicleModel { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending"; // Pending, Ordered, Rejected, Available
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public CustomerProfile CustomerProfile { get; set; } = null!;
}
