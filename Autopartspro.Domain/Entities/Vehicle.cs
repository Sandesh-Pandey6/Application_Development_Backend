using Autopartspro.Domain.Enums;

namespace Autopartspro.Domain.Entities
{
    public class Vehicle : BaseEntity
    {
        public Guid CustomerId { get; set; }
        public string Make { get; set; } = string.Empty;        // e.g. Honda
        public string Model { get; set; } = string.Empty;       // e.g. City
        public int Year { get; set; }                           // e.g. 2020
        public FuelType FuelType { get; set; }                  // Petrol, Diesel, etc
        public string NumberPlate { get; set; } = string.Empty; // e.g. BA 1 Kha 2345

        // Navigation Properties
        public User Customer { get; set; } = null!;
        public ICollection<SalesInvoice> SalesInvoices { get; set; } = new List<SalesInvoice>();
    }
}