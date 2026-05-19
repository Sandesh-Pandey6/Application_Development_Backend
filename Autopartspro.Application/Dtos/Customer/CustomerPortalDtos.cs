namespace Autopartspro.Application.Dtos.Customer;

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
    /// <summary>Sales invoice for a parts purchase the customer owns (required).</summary>
    public Guid InvoiceId { get; set; }
}

public class CustomerCheckoutDto
{
    public List<CustomerCheckoutItemDto> Items { get; set; } = new();
    public string PaymentStatus { get; set; } = "Paid";
}

public class CustomerCheckoutItemDto
{
    public Guid PartId { get; set; }
    public int Quantity { get; set; }
}

