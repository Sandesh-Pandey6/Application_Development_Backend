using Autopartspro.Application.Interfaces;
using Autopartspro.Domain.Enums;
using Autopartspro.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Autopartspro.Infrastructure.Services;

public class AppointmentSchedulingService : IAppointmentSchedulingService
{
    public const int DefaultMaxPerSlot = 2;

    private readonly AppDbContext _db;

    public AppointmentSchedulingService(AppDbContext db)
    {
        _db = db;
    }

    public int MaxPerSlot => DefaultMaxPerSlot;

    public async Task<int> CountAtSlotAsync(
        DateOnly date,
        TimeOnly time,
        Guid? excludeAppointmentId = null,
        CancellationToken ct = default)
    {
        var q = _db.Appointments.Where(a =>
            a.PreferredDate == date &&
            a.PreferredTime == time &&
            a.Status != AppointmentStatus.Cancelled &&
            a.Status != AppointmentStatus.Completed &&
            a.Status != AppointmentStatus.RescheduleProposed);

        if (excludeAppointmentId.HasValue)
            q = q.Where(a => a.Id != excludeAppointmentId.Value);

        return await q.CountAsync(ct);
    }

    public async Task<int> CountAtProposedSlotAsync(
        DateOnly date,
        TimeOnly time,
        Guid? excludeAppointmentId = null,
        CancellationToken ct = default)
    {
        var q = _db.Appointments.Where(a =>
            a.Status == AppointmentStatus.RescheduleProposed &&
            a.ProposedDate == date &&
            a.ProposedTime == time);

        if (excludeAppointmentId.HasValue)
            q = q.Where(a => a.Id != excludeAppointmentId.Value);

        return await q.CountAsync(ct);
    }

    public async Task<bool> IsSlotFullAsync(
        DateOnly date,
        TimeOnly time,
        Guid? excludeAppointmentId = null,
        CancellationToken ct = default)
    {
        var count = await CountAtSlotAsync(date, time, excludeAppointmentId, ct);
        return count >= MaxPerSlot;
    }

    public async Task<bool> IsProposedSlotFullAsync(
        DateOnly date,
        TimeOnly time,
        Guid? excludeAppointmentId = null,
        CancellationToken ct = default)
    {
        var preferred = await CountAtSlotAsync(date, time, excludeAppointmentId, ct);
        var proposed = await CountAtProposedSlotAsync(date, time, excludeAppointmentId, ct);
        return preferred + proposed >= MaxPerSlot;
    }

    public TimeOnly ParseTimeSlot(string time)
    {
        if (string.IsNullOrWhiteSpace(time))
            return new TimeOnly(10, 0);
        if (time.Contains("Evening", StringComparison.OrdinalIgnoreCase) ||
            time.Contains("4 PM", StringComparison.OrdinalIgnoreCase))
            return new TimeOnly(16, 0);
        if (time.Contains("Afternoon", StringComparison.OrdinalIgnoreCase) ||
            time.Contains("1 PM", StringComparison.OrdinalIgnoreCase))
            return new TimeOnly(13, 0);
        if (TimeOnly.TryParse(time, out var parsed))
            return parsed;
        return new TimeOnly(10, 0);
    }

    public string FormatTimeSlot(TimeOnly time) =>
        time.ToString("h:mm tt", System.Globalization.CultureInfo.InvariantCulture);

    public string FormatTimeSlotLabel(TimeOnly time) => time.Hour switch
    {
        >= 16 => "Evening (4 PM - 6 PM)",
        >= 13 => "Afternoon (1 PM - 4 PM)",
        _ => "Morning (10 AM - 1 PM)",
    };
}
