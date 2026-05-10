namespace Autopartspro.Domain.Entities;

public class Purchase
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CustomerId { get; set; }
    public string PartName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public string Status { get; set; } = "Completed"; // Completed, Pending, Cancelled
    public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public CustomerProfile CustomerProfile { get; set; } = null!;
}
