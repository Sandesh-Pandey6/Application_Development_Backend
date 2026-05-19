using Autopartspro.Application.Dtos.Appointments;
using Autopartspro.Application.Interfaces;
using Autopartspro.Domain.Entities;
using Autopartspro.Domain.Enums;
using Autopartspro.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Autopartspro.Controllers;

[ApiController]
[Route("api/appointments")]
[Authorize(Roles = "Admin,Staff")]
public class AppointmentsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IAppointmentSchedulingService _scheduling;
    private readonly IUserNotificationService _notifications;

    public AppointmentsController(
        AppDbContext db,
        IAppointmentSchedulingService scheduling,
        IUserNotificationService notifications)
    {
        _db = db;
        _scheduling = scheduling;
        _notifications = notifications;
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? status)
    {
        var q = _db.Appointments
            .Include(a => a.Customer)
            .AsNoTracking()
            .OrderByDescending(a => a.PreferredDate)
            .ThenByDescending(a => a.PreferredTime)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<AppointmentStatus>(status, true, out var st))
            q = q.Where(a => a.Status == st);

        var list = await q.ToListAsync();
        var mapped = new List<object>();
        foreach (var a in list)
        {
            var slotFull = a.Status == AppointmentStatus.Pending &&
                           await _scheduling.IsSlotFullAsync(a.PreferredDate, a.PreferredTime, a.Id);
            mapped.Add(MapStaffAppointment(a, slotFull));
        }

        return Ok(mapped);
    }

    [HttpGet("slot-availability")]
    public async Task<IActionResult> SlotAvailability([FromQuery] string date, [FromQuery] string time, [FromQuery] Guid? excludeId)
    {
        if (!DateOnly.TryParse(date, out var d))
            return BadRequest(new { message = "Invalid date." });
        var t = _scheduling.ParseTimeSlot(time);
        var full = await _scheduling.IsProposedSlotFullAsync(d, t, excludeId);
        var count = await _scheduling.CountAtSlotAsync(d, t, excludeId);
        return Ok(new
        {
            date = d.ToString("yyyy-MM-dd"),
            time = _scheduling.FormatTimeSlot(t),
            isFull = full,
            booked = count,
            maxPerSlot = _scheduling.MaxPerSlot,
        });
    }

    [HttpPost("{id:guid}/accept")]
    public async Task<IActionResult> Accept(Guid id)
    {
        var appointment = await _db.Appointments
            .Include(a => a.Customer)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (appointment == null)
            return NotFound(new { message = "Appointment not found." });
        if (appointment.Status != AppointmentStatus.Pending)
            return BadRequest(new { message = "Only pending appointments can be accepted." });

        if (await _scheduling.IsSlotFullAsync(appointment.PreferredDate, appointment.PreferredTime, appointment.Id))
        {
            return Conflict(new
            {
                message = "This time slot is full. Propose a different time for the customer.",
                slotFull = true,
            });
        }

        appointment.Status = AppointmentStatus.Confirmed;
        appointment.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var when = $"{appointment.PreferredDate:yyyy-MM-dd} at {_scheduling.FormatTimeSlot(appointment.PreferredTime)}";
        await _notifications.NotifyUserAsync(
            appointment.CustomerId,
            $"Your {appointment.ServiceType} appointment is confirmed for {when}.",
            NotificationType.AppointmentConfirmation);

        return Ok(MapStaffAppointment(appointment, false));
    }

    [HttpPost("{id:guid}/propose-reschedule")]
    public async Task<IActionResult> ProposeReschedule(Guid id, [FromBody] ProposeRescheduleDto dto)
    {
        if (!DateOnly.TryParse(dto.Date, out var proposedDate))
            return BadRequest(new { message = "Invalid date." });

        var appointment = await _db.Appointments
            .Include(a => a.Customer)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (appointment == null)
            return NotFound(new { message = "Appointment not found." });
        if (appointment.Status is not AppointmentStatus.Pending and not AppointmentStatus.Confirmed)
            return BadRequest(new { message = "This appointment cannot be rescheduled." });

        var proposedTime = _scheduling.ParseTimeSlot(dto.Time);
        if (await _scheduling.IsProposedSlotFullAsync(proposedDate, proposedTime, appointment.Id))
            return BadRequest(new { message = "The selected time slot is also full. Choose another time." });

        appointment.ProposedDate = proposedDate;
        appointment.ProposedTime = proposedTime;
        appointment.StaffNotes = string.IsNullOrWhiteSpace(dto.Message)
            ? "Your requested time was full. Please confirm this new time."
            : dto.Message.Trim();
        appointment.Status = AppointmentStatus.RescheduleProposed;
        appointment.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var when = $"{proposedDate:yyyy-MM-dd} at {_scheduling.FormatTimeSlot(proposedTime)}";
        await _notifications.NotifyUserAsync(
            appointment.CustomerId,
            $"Your appointment was moved to {when}. Open Appointments to accept or decline. {appointment.StaffNotes}",
            NotificationType.AppointmentConfirmation);

        return Ok(MapStaffAppointment(appointment, false));
    }

    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id)
    {
        var appointment = await _db.Appointments.FirstOrDefaultAsync(a => a.Id == id);
        if (appointment == null)
            return NotFound(new { message = "Appointment not found." });
        if (appointment.Status != AppointmentStatus.Confirmed)
            return BadRequest(new { message = "Only confirmed appointments can be marked completed." });

        appointment.Status = AppointmentStatus.Completed;
        appointment.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(MapStaffAppointment(appointment, false));
    }

    private object MapStaffAppointment(Appointment a, bool slotFull) => new
    {
        id = a.Id.ToString(),
        customerId = a.CustomerId.ToString(),
        customerName = a.Customer?.FullName ?? "—",
        customerPhone = a.Customer?.PhoneNumber ?? "—",
        customerEmail = a.Customer?.Email ?? "—",
        service = a.ServiceType,
        date = a.PreferredDate.ToString("yyyy-MM-dd"),
        time = _scheduling.FormatTimeSlot(a.PreferredTime),
        timeSlot = _scheduling.FormatTimeSlotLabel(a.PreferredTime),
        status = a.Status.ToString(),
        vehicle = ExtractVehicle(a.Notes),
        notes = a.Notes,
        staffNotes = a.StaffNotes,
        proposedDate = a.ProposedDate?.ToString("yyyy-MM-dd"),
        proposedTime = a.ProposedTime.HasValue ? _scheduling.FormatTimeSlot(a.ProposedTime.Value) : null,
        proposedTimeSlot = a.ProposedTime.HasValue ? _scheduling.FormatTimeSlotLabel(a.ProposedTime.Value) : null,
        slotFull,
        createdAt = a.CreatedAt,
    };

    private static string? ExtractVehicle(string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes) || !notes.StartsWith("Vehicle:", StringComparison.OrdinalIgnoreCase))
            return null;
        var firstLine = notes.Split('\n')[0];
        return firstLine["Vehicle:".Length..].Trim();
    }
}
