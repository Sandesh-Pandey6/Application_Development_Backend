namespace Autopartspro.Application.Interfaces;

public interface IPartRequestService
{
    Task<int> CreateAsync(string customerId, string partName, string vehicleModel, string description);
    Task<(List<PartRequestDto> items, int total)> GetCustomerRequestsAsync(string customerId, int pageNumber = 1, int pageSize = 10);
    Task<(List<PartRequestDto> items, int total)> GetAllAsync(int pageNumber = 1, int pageSize = 10);
    Task<PartRequestDto?> GetByIdAsync(int id);
    Task<bool> UpdateAsync(int id, string partName, string vehicleModel, string description);
    Task<bool> UpdateStatusAsync(int id, string newStatus);
    Task<bool> DeleteAsync(int id);
}

public class PartRequestDto
{
    public int Id { get; set; }
    public string CustomerId { get; set; } = null!;
    public string PartName { get; set; } = null!;
    public string VehicleModel { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
