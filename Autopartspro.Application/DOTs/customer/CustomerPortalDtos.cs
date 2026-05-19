namespace Autopartspro.Application.DOTs.customer;

public class BookAppointmentDto
{
    public string ServiceType { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string? Vehicle { get; set; }
}

public class CreatePartRequestDto
{
    public string PartName { get; set; } = string.Empty;
    public string PartDescription { get; set; } = string.Empty;
    public string VehicleModel { get; set; } = string.Empty;
    public string UrgencyLevel { get; set; } = "Normal";
}

public class SubmitReviewDto
{
    public int Rating { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Service { get; set; } = string.Empty;
}

public class RedeemPointsDto
{
    public int Points { get; set; }
}
