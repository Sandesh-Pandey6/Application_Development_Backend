using Autopartspro.Application.Dtos.Admin;
using Autopartspro.Domain.Enums;

namespace Autopartspro.Application.Interfaces;

public interface IUserNotificationService
{
    Task<NotificationListDto> GetForUserAsync(Guid userId, string? typeFilter);
    Task MarkReadAsync(Guid notificationId, Guid userId);
    Task MarkAllReadAsync(Guid userId);
    Task DeleteAsync(Guid notificationId, Guid userId);
    Task NotifyUserAsync(Guid userId, string message, NotificationType type);
    Task NotifyUsersWithRoleAsync(RoleType role, string message, NotificationType type);
    Task NotifyAdminsAsync(string message, NotificationType type);
    Task NotifyAdminsAndStaffAsync(string message, NotificationType type);
    Task NotifyLowStockAsync(string partName, int stockQuantity);
}
