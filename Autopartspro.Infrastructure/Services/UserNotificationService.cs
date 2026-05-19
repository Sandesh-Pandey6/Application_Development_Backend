using Autopartspro.Application.Dtos.Admin;
using Autopartspro.Application.Interfaces;
using Autopartspro.Domain.Entities;
using Autopartspro.Domain.Enums;
using Autopartspro.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Autopartspro.Infrastructure.Services;

public class UserNotificationService : IUserNotificationService
{
    private const int LowStockThreshold = 10;
    private readonly AppDbContext _context;

    public UserNotificationService(AppDbContext context) => _context = context;

    public async Task<NotificationListDto> GetForUserAsync(Guid userId, string? typeFilter)
    {
        var query = _context.Notifications
            .Where(n => n.UserId == userId)
            .AsQueryable();

        if (!string.IsNullOrEmpty(typeFilter) && typeFilter != "All")
        {
            var notifType = MapFilterType(typeFilter);
            if (notifType.HasValue)
                query = query.Where(n => n.Type == notifType.Value);
        }

        var notifications = await query
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        var all = await _context.Notifications
            .Where(n => n.UserId == userId)
            .ToListAsync();

        return new NotificationListDto
        {
            TotalUnread = all.Count(n => !n.IsRead),
            LowStockCount = all.Count(n => n.Type == NotificationType.LowStock && !n.IsRead),
            CreditReminderCount = all.Count(n => n.Type == NotificationType.CreditReminder && !n.IsRead),
            InfoCount = all.Count(n =>
                n.Type is NotificationType.General or NotificationType.AppointmentConfirmation && !n.IsRead),
            Notifications = notifications.Select(MapToDto).ToList(),
        };
    }

    public async Task MarkReadAsync(Guid notificationId, Guid userId)
    {
        var notification = await _context.Notifications.FindAsync(notificationId)
            ?? throw new KeyNotFoundException("Notification not found.");

        if (notification.UserId != userId)
            throw new UnauthorizedAccessException("You can only update your own notifications.");

        notification.IsRead = true;
        notification.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task MarkAllReadAsync(Guid userId)
    {
        var unread = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        foreach (var n in unread)
        {
            n.IsRead = true;
            n.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid notificationId, Guid userId)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId)
            ?? throw new KeyNotFoundException("Notification not found.");

        _context.Notifications.Remove(notification);
        await _context.SaveChangesAsync();
    }

    public async Task NotifyUserAsync(Guid userId, string message, NotificationType type)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        _context.Notifications.Add(new Notification
        {
            UserId = userId,
            Message = message.Trim(),
            Type = type,
            IsRead = false,
        });
        await _context.SaveChangesAsync();
    }

    public async Task NotifyUsersWithRoleAsync(RoleType role, string message, NotificationType type)
    {
        var userIds = await _context.Users
            .Where(u => u.Role == role && u.Status == StatusType.Active)
            .Select(u => u.Id)
            .ToListAsync();

        foreach (var id in userIds)
            await NotifyUserAsync(id, message, type);
    }

    public Task NotifyAdminsAsync(string message, NotificationType type) =>
        NotifyUsersWithRoleAsync(RoleType.Admin, message, type);

    public async Task NotifyAdminsAndStaffAsync(string message, NotificationType type)
    {
        var userIds = await _context.Users
            .Where(u =>
                (u.Role == RoleType.Admin || u.Role == RoleType.Staff) &&
                u.Status == StatusType.Active)
            .Select(u => u.Id)
            .Distinct()
            .ToListAsync();

        foreach (var id in userIds)
        {
            _context.Notifications.Add(new Notification
            {
                UserId = id,
                Message = message.Trim(),
                Type = type,
                IsRead = false,
            });
        }

        if (userIds.Count > 0)
            await _context.SaveChangesAsync();
    }

    public Task NotifyLowStockAsync(string partName, int stockQuantity)
    {
        if (stockQuantity > LowStockThreshold)
            return Task.CompletedTask;

        var message =
            $"{partName} stock is {stockQuantity} unit(s) — at or below the minimum threshold of {LowStockThreshold}. Reorder soon.";
        return NotifyAdminsAndStaffAsync(message, NotificationType.LowStock);
    }

    private static NotificationResponseDto MapToDto(Notification n) => new()
    {
        Id = n.Id,
        Title = GetTitle(n.Type),
        Message = n.Message,
        Type = MapDisplayType(n.Type),
        IsRead = n.IsRead,
        CreatedAt = n.CreatedAt,
    };

    private static string GetTitle(NotificationType type) => type switch
    {
        NotificationType.LowStock => "Low stock alert",
        NotificationType.CreditReminder => "Payment reminder",
        NotificationType.AppointmentConfirmation => "Appointment",
        NotificationType.General => "Update",
        _ => "Notification",
    };

    private static string MapDisplayType(NotificationType type) => type switch
    {
        NotificationType.LowStock => "Low Stock",
        NotificationType.CreditReminder => "Credit Reminder",
        NotificationType.AppointmentConfirmation => "Appointment",
        NotificationType.General => "Info",
        _ => "Info",
    };

    private static NotificationType? MapFilterType(string type) => type switch
    {
        "Low Stock" => NotificationType.LowStock,
        "Credit Reminder" => NotificationType.CreditReminder,
        "Appointment" => NotificationType.AppointmentConfirmation,
        "Info" => NotificationType.General,
        _ when Enum.TryParse<NotificationType>(type, true, out var parsed) => parsed,
        _ => null,
    };
}
