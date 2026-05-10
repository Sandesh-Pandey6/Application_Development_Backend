using Autopartspro.Application.Interfaces;
using Autopartspro.Infrastructure.Data;

namespace Autopartspro.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly AppDbContext _context;

    public NotificationService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<(List<NotificationDto> items, int total)> GetAllAsync(int pageNumber = 1, int pageSize = 10)
    {
        var items = new List<NotificationDto>();
        return await Task.FromResult((items, 0));
    }

    public async Task<NotificationDto?> GetByIdAsync(int id)
    {
        return await Task.FromResult<NotificationDto?>(null);
    }

    public async Task<bool> MarkAsReadAsync(int id)
    {
        return await Task.FromResult(true);
    }

    public async Task<int> CreateStockNotificationAsync(string title, string message)
    {
        return await Task.FromResult(1);
    }
}
