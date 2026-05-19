using Autopartspro.Application.Dtos.Staff;

namespace Autopartspro.Application.Interfaces;

public interface IStaffProfileService
{
    Task<StaffProfileDto> GetProfileAsync(Guid staffUserId);
    Task<StaffProfileDto> UpdateProfileAsync(Guid staffUserId, UpdateStaffProfileDto dto);
}
