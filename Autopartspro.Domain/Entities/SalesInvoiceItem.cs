using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Autopartspro.Domain.Entities;

public class SalesInvoiceItem
{
    public int Id { get; set; }

    public int SalesInvoiceId { get; set; }
    public SalesInvoice? SalesInvoice { get; set; }

    public int PartId { get; set; }
    public Part? Part { get; set; }

    [MaxLength(150)]
    public string PartNameSnapshot { get; set; } = string.Empty;

    public int Quantity { get; set; }

    [Column(TypeName = "numeric(12,2)")]
    public decimal UnitPrice { get; set; }

    [Column(TypeName = "numeric(12,2)")]
    public decimal Subtotal { get; set; }
}
