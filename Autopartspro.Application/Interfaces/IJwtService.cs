using Autopartspro.Domain.Entities;

namespace Autopartspro.Application.Interfaces;

public interface IJwtService
{
    string GenerateToken(User user);
    string GenerateRefreshToken();
    bool ValidateToken(string token, out string userId);
}