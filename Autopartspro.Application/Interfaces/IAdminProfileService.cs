using Autopartspro.Application.Dtos.Admin;

namespace Autopartspro.Application.Interfaces;

public interface IAdminProfileService
{
    Task<AdminProfileDto> GetProfileAsync(Guid adminUserId);
    Task<AdminProfileDto> UpdateProfileAsync(Guid adminUserId, UpdateAdminProfileDto dto);
}
