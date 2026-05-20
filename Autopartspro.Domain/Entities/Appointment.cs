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
		/// Staff-proposed date when the requested slot is full.
		public DateOnly? ProposedDate { get; set; }
		public TimeOnly? ProposedTime { get; set; }
		public string? StaffNotes { get; set; }

		// Navigation Properties
		public User Customer { get; set; } = null!;
		public ICollection<Review> Reviews { get; set; } = new List<Review>();
	}
}