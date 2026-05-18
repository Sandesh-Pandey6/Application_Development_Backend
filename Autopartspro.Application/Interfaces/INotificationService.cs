namespace Autopartspro.Application.Interfaces;

public interface INotificationService
{
    Task<(List<NotificationDto> items, int total)> GetAllAsync(int pageNumber = 1, int pageSize = 10);
    Task<NotificationDto?> GetByIdAsync(int id);
    Task<bool> MarkAsReadAsync(int id);
    Task<int> CreateStockNotificationAsync(string title, string message);
}

public class NotificationDto
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string Message { get; set; } = null!;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}
