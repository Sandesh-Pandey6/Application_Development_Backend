namespace Autopartspro.Application.Dtos.Admin;

public class UnpaidSalesInvoicesSummaryDto
{
    public int TotalUnpaid { get; set; }
    public int OverdueCount { get; set; }
    public decimal TotalOutstanding { get; set; }
    public decimal OverdueOutstanding { get; set; }
    public int OverdueDaysThreshold { get; set; } = 3;
    public List<UnpaidSalesInvoiceDto> Invoices { get; set; } = new();
}

public class UnpaidSalesInvoiceDto
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }
    public string? CustomerEmail { get; set; }
    public string? StaffName { get; set; }
    public DateTime SaleDate { get; set; }
    public decimal TotalAmount { get; set; }
    public int DaysSinceSale { get; set; }
    public bool IsOverdue { get; set; }
    public bool ReminderSent { get; set; }
    public DateTime? OverdueReminderSentAt { get; set; }
}
