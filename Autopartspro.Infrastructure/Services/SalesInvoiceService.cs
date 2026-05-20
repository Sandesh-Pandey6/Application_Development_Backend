using Autopartspro.Application.Interfaces;
using Autopartspro.Domain.Enums;
using Autopartspro.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Autopartspro.Infrastructure.Services
{
    public class SalesInvoiceService : ISalesInvoiceService
    {
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;

        public SalesInvoiceService(AppDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public async Task<bool> SendInvoiceEmailAsync(Guid invoiceId)
        {
            var invoice = await LoadInvoiceForPdfAsync(invoiceId);

            if (string.IsNullOrWhiteSpace(invoice.Customer.Email) ||
                invoice.Customer.Email.EndsWith("@customer.local", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "This customer has no valid email on file. Add an email to their profile before sending the invoice.");
            }

            var pdfBytes = InvoicePdfGenerator.Build(invoice);
            await _emailService.SendInvoiceEmailAsync(
                invoice.Customer.Email,
                invoice.Customer.FullName,
                invoice.InvoiceNumber,
                pdfBytes);
            return true;
        }

        public async Task<byte[]> GetInvoicePdfBytesAsync(Guid invoiceId)
        {
            var invoice = await LoadInvoiceForPdfAsync(invoiceId);
            return InvoicePdfGenerator.Build(invoice);
        }

        public async Task<byte[]> GetPaidInvoicePdfAsync(Guid invoiceId)
        {
            var invoice = await LoadInvoiceForPdfAsync(invoiceId);
            if (invoice.PaymentStatus != PaymentStatus.Paid)
            {
                throw new InvalidOperationException(
                    "PDF download and print are only available for paid invoices.");
            }

            return InvoicePdfGenerator.Build(invoice);
        }

        private async Task<Domain.Entities.SalesInvoice> LoadInvoiceForPdfAsync(Guid invoiceId)
        {
            var invoice = await _context.SalesInvoices
                .Include(i => i.Customer)
                .Include(i => i.Vehicle)
                .Include(i => i.Staff)
                .Include(i => i.Items)
                .ThenInclude(x => x.Part)
                .FirstOrDefaultAsync(i => i.Id == invoiceId);

            if (invoice == null)
                throw new KeyNotFoundException("Invoice not found.");

            return invoice;
        }
    }
}
