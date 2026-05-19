namespace Autopartspro.Application.Dtos.Appointments;

public class ProposeRescheduleDto
{
    public string Date { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;
    public string? Message { get; set; }
}

public class SlotAvailabilityDto
{
    public string Date { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;
}
