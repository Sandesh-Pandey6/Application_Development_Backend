using Autopartspro.Domain.Entities;

namespace Autopartspro.Application.Interfaces
{
    public interface IJwtService
    {
        string GenerateToken(User user);
        string? ValidateToken(string token);
        string? GetEmailFromToken(string token);
    }
}