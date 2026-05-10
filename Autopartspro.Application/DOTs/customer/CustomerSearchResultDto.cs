namespace Autopartspro.Application.DOTs.customer
{
    public class CustomerSearchResultDto
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public List<string> VehicleNumbers { get; set; } = new();
    }
}
