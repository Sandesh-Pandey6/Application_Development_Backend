using Autopartspro.Domain.Enums;

namespace Autopartspro.Domain.Entities
{
    public class PartRequest : BaseEntity
    {
        public Guid CustomerId { get; set; }
        public string PartName { get; set; } = string.Empty;
        public string PartDescription { get; set; } = string.Empty;
        public string VehicleModel { get; set; } = string.Empty;
        public UrgencyLevel UrgencyLevel { get; set; }
        public PartRequestStatus Status { get; set; } = PartRequestStatus.Pending;

        // Navigation Properties
        public User Customer { get; set; } = null!;
    }
}