namespace Autopartspro.Domain.Entities;

/// <summary>
/// Represents an appointment booking for vehicle service.
/// </summary>
public class Appointment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CustomerId { get; set; }
    public DateTime AppointmentDate { get; set; }
    public TimeSpan AppointmentTime { get; set; }
    public string ServiceType { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending"; // Pending, Approved, Completed, Cancelled
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public CustomerProfile CustomerProfile { get; set; } = null!;
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}
