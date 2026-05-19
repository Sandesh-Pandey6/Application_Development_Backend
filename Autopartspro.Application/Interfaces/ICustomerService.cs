using Autopartspro.Application.Dtos.Auth;
using Autopartspro.Application.Dtos.Customer;

namespace Autopartspro.Application.Interfaces
{
    public interface ICustomerService
    {
        // Self-service
        Task<UserProfileDto> GetProfileAsync(Guid userId);
        Task<UserProfileDto> UpdateProfileAsync(Guid userId, UpdateProfileDto dto);
        Task<UserProfileDto> ChangePasswordAsync(Guid userId, ChangePasswordDto dto);
        Task<VehicleDto> AddVehicleAsync(Guid userId, CreateVehicleDto dto);
        Task<VehicleDto> UpdateVehicleAsync(Guid userId, Guid vehicleId, UpdateVehicleDto dto);
        Task DeleteVehicleAsync(Guid userId, Guid vehicleId);

        // Staff
        Task<List<CustomerSearchResultDto>> SearchCustomersAsync(string query, string searchBy);
    }
}
