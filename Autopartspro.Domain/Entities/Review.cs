using Autopartspro.Domain.Enums;

namespace Autopartspro.Domain.Entities
{
    public class Review : BaseEntity
    {
        public Guid CustomerId { get; set; }
        public int Rating { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ReviewCategory ReviewCategory { get; set; }
        public Guid? RelatedInvoiceId { get; set; }
        public Guid? RelatedAppointmentId { get; set; }

        // Navigation Properties
        public User Customer { get; set; } = null!;
        public SalesInvoice? RelatedInvoice { get; set; }
        public Appointment? RelatedAppointment { get; set; }
    }
}