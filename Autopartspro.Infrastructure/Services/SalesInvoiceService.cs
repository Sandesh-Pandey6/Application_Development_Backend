using Autopartspro.Application.Interfaces;
using Autopartspro.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text;

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
            var invoice = await _context.SalesInvoices
                .Include(i => i.Customer)
                .FirstOrDefaultAsync(i => i.Id == invoiceId);

            if (invoice == null) throw new KeyNotFoundException("Invoice not found.");

            // Basic text-based PDF fallback for now (to satisfy byte[])
            var dummyContent = $"Invoice Number: {invoice.InvoiceNumber}\nAmount: {invoice.TotalAmount}\nStatus: {invoice.PaymentStatus}";
            var dummyPdfBytes = Encoding.UTF8.GetBytes(dummyContent);

            await _emailService.SendInvoiceEmailAsync(invoice.Customer.Email, invoice.Customer.FullName, invoice.InvoiceNumber, dummyPdfBytes);
            return true;
        }
    }
}
