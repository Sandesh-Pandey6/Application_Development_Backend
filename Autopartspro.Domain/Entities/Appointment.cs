using Autopartspro.Domain.Enums;

namespace Autopartspro.Domain.Entities
{
	public class Appointment : BaseEntity
	{
		public Guid CustomerId { get; set; }
		public string ServiceType { get; set; } = string.Empty;
		public DateOnly PreferredDate { get; set; }
		public TimeOnly PreferredTime { get; set; }
		public string? Notes { get; set; }
		public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;

		// Navigation Properties
		public User Customer { get; set; } = null!;
		public ICollection<Review> Reviews { get; set; } = new List<Review>();
	}
}