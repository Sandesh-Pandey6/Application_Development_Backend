namespace Autopartspro.Application.DOTs.customer
{
    public class CreateVehicleDto
    {
        public string Make { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public int Year { get; set; }
        public string FuelType { get; set; } = string.Empty;
        public string NumberPlate { get; set; } = string.Empty;
    }
}
