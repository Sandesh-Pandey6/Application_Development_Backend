namespace Autopartspro.Application.Interfaces
{
    public interface ISalesInvoiceService
    {
        Task<bool> SendInvoiceEmailAsync(Guid invoiceId);
        Task<byte[]> GetInvoicePdfBytesAsync(Guid invoiceId);
        Task<byte[]> GetPaidInvoicePdfAsync(Guid invoiceId);
    }
}
