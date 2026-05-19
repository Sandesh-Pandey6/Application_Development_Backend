using Autopartspro.Application.Dtos.Auth;
using Autopartspro.Domain.Enums;

namespace Autopartspro.Application.Interfaces;

public interface IUserPasswordService
{
    Task<UserProfileDto> ChangePasswordAsync(Guid userId, ChangePasswordDto dto, params RoleType[] allowedRoles);
}
