namespace Autopartspro.Domain.Entities;

public class CustomerProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public decimal TotalSpent { get; set; } = 0;
    public decimal CreditBalance { get; set; } = 0;
    public DateTime? CreditDueDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User User { get; set; } = null!;
    public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<PartRequest> PartRequests { get; set; } = new List<PartRequest>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    public ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();
}
