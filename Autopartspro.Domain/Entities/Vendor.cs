namespace Autopartspro.Domain.Entities
{
	public class Vendor : BaseEntity
	{
		public string VendorName { get; set; } = string.Empty;
		public string ContactPerson { get; set; } = string.Empty;
		public string PhoneNumber { get; set; } = string.Empty;
		public string Email { get; set; } = string.Empty;
		public string Address { get; set; } = string.Empty;

		// Navigation Properties
		public ICollection<Part> Parts { get; set; } = new List<Part>();
		public ICollection<PurchaseInvoice> PurchaseInvoices { get; set; } = new List<PurchaseInvoice>();
	}
}