namespace Autopartspro.Application.Dtos.Customer
{
    public class UpdateVehicleDto
    {
        public string? Make { get; set; }
        public string? Model { get; set; }
        public int? Year { get; set; }
        public string? FuelType { get; set; }
        public string? NumberPlate { get; set; }
    }
}
