using Autopartspro.Domain.Entities;

namespace Autopartspro.Application.Interfaces;

public interface IAppointmentSchedulingService
{
    int MaxPerSlot { get; }
    Task<int> CountAtSlotAsync(DateOnly date, TimeOnly time, Guid? excludeAppointmentId = null, CancellationToken ct = default);
    Task<bool> IsSlotFullAsync(DateOnly date, TimeOnly time, Guid? excludeAppointmentId = null, CancellationToken ct = default);
    Task<bool> IsProposedSlotFullAsync(DateOnly date, TimeOnly time, Guid? excludeAppointmentId = null, CancellationToken ct = default);
    TimeOnly ParseTimeSlot(string time);
    string FormatTimeSlot(TimeOnly time);
    string FormatTimeSlotLabel(TimeOnly time);
}
