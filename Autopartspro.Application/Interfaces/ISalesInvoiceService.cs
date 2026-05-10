namespace Autopartspro.Application.Interfaces
{
    public interface ISalesInvoiceService
    {
        Task<bool> SendInvoiceEmailAsync(Guid invoiceId);
    }
}
