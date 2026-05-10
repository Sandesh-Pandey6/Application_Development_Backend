using Autopartspro.Application.DOTs.auth;

namespace Autopartspro.Application.DOTs.report
{
    public class RegularCustomerReportDto
    {
        public Guid CustomerId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public int TotalInvoices { get; set; }
        public decimal TotalSpent { get; set; }
    }

    public class HighSpenderReportDto
    {
        public Guid CustomerId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public decimal TotalSpent { get; set; }
        public int InvoiceCount { get; set; }
    }

    public class PendingCreditReportDto
    {
        public Guid CustomerId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public decimal TotalPending { get; set; }
        public int PendingInvoiceCount { get; set; }
        public List<InvoiceBasicDto> PendingInvoices { get; set; } = new();
    }

    public class InvoiceBasicDto
    {
        public Guid Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public DateTime SaleDate { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
    }
}
