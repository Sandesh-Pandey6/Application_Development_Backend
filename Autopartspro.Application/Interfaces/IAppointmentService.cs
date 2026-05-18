namespace Autopartspro.Application.Interfaces;

public interface IAppointmentService
{
    Task<int> CreateAsync(string customerId, DateTime appointmentDate, TimeOnly appointmentTime, string serviceType, string? notes);
    Task<(List<AppointmentDto> items, int total)> GetCustomerAppointmentsAsync(string customerId, int pageNumber = 1, int pageSize = 10);
    Task<(List<AppointmentDto> items, int total)> GetAllAsync(int pageNumber = 1, int pageSize = 10);
    Task<AppointmentDto?> GetByIdAsync(int id);
    Task<bool> UpdateAsync(int id, DateTime appointmentDate, TimeOnly appointmentTime, string serviceType, string? notes);
    Task<bool> ApproveAsync(int id);
    Task<bool> CompleteAsync(int id);
    Task<bool> CancelAsync(int id);
    Task<bool> DeleteAsync(int id);
}

public class AppointmentDto
{
    public int Id { get; set; }
    public string CustomerId { get; set; } = null!;
    public DateTime AppointmentDate { get; set; }
    public TimeOnly AppointmentTime { get; set; }
    public string ServiceType { get; set; } = null!;
    public string? Notes { get; set; }
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
