using Autopartspro.Domain.Enums;

namespace Autopartspro.Domain.Entities
{
	public class Notification : BaseEntity
	{
		public Guid UserId { get; set; }
		public string Message { get; set; } = string.Empty;
		public NotificationType Type { get; set; }
		public bool IsRead { get; set; } = false;

		// Navigation Properties
		public User User { get; set; } = null!;
	}
}