using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Autopartspro.Domain.Entities;

public class SalesInvoice
{
    public int Id { get; set; }

    [Required, MaxLength(40)]
    public string InvoiceNumber { get; set; } = string.Empty;

    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public int? VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }

    [MaxLength(150)]
    public string? StaffName { get; set; }

    public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "numeric(12,2)")]
    public decimal Subtotal { get; set; }

    [Column(TypeName = "numeric(12,2)")]
    public decimal DiscountAmount { get; set; }

    [Column(TypeName = "numeric(12,2)")]
    public decimal TotalAmount { get; set; }

    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Paid;

    [MaxLength(500)]
    public string? Notes { get; set; }

    public ICollection<SalesInvoiceItem> Items { get; set; } = new List<SalesInvoiceItem>();
}
