using Autopartspro.Application.DOTs.report;
using Autopartspro.Application.Interfaces;
using Autopartspro.Domain.Enums;
using Autopartspro.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Autopartspro.Infrastructure.Services
{
    public class ReportService : IReportService
    {
        private readonly AppDbContext _context;

        public ReportService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<RegularCustomerReportDto>> GetRegularCustomersAsync(int minimumInvoices = 3)
        {
            return await _context.Users
                .Where(u => u.Role == RoleType.Customer)
                .Select(c => new
                {
                    Customer = c,
                    InvoiceCount = c.CustomerInvoices.Count,
                    TotalSpent = c.CustomerInvoices.Where(i => i.PaymentStatus == PaymentStatus.Paid).Sum(i => i.TotalAmount)
                })
                .Where(x => x.InvoiceCount >= minimumInvoices)
                .Select(x => new RegularCustomerReportDto
                {
                    CustomerId = x.Customer.Id,
                    FullName = x.Customer.FullName,
                    Email = x.Customer.Email,
                    PhoneNumber = x.Customer.PhoneNumber,
                    TotalInvoices = x.InvoiceCount,
                    TotalSpent = x.TotalSpent
                })
                .OrderByDescending(r => r.TotalInvoices)
                .ToListAsync();
        }

        public async Task<List<HighSpenderReportDto>> GetHighSpendersAsync(decimal threshold = 1000)
        {
            return await _context.Users
                .Where(u => u.Role == RoleType.Customer)
                .Select(c => new
                {
                    Customer = c,
                    TotalSpent = c.CustomerInvoices.Where(i => i.PaymentStatus == PaymentStatus.Paid).Sum(i => i.TotalAmount),
                    InvoiceCount = c.CustomerInvoices.Count
                })
                .Where(x => x.TotalSpent >= threshold)
                .Select(x => new HighSpenderReportDto
                {
                    CustomerId = x.Customer.Id,
                    FullName = x.Customer.FullName,
                    Email = x.Customer.Email,
                    TotalSpent = x.TotalSpent,
                    InvoiceCount = x.InvoiceCount
                })
                .OrderByDescending(r => r.TotalSpent)
                .ToListAsync();
        }

        public async Task<List<PendingCreditReportDto>> GetPendingCreditsAsync()
        {
            var customersWithPending = await _context.Users
                .Include(u => u.CustomerInvoices)
                .Where(u => u.Role == RoleType.Customer && u.CustomerInvoices.Any(i => i.PaymentStatus == PaymentStatus.Unpaid || i.PaymentStatus == PaymentStatus.Overdue))
                .ToListAsync();

            var results = new List<PendingCreditReportDto>();

            foreach (var c in customersWithPending)
            {
                var pendingInvoices = c.CustomerInvoices
                    .Where(i => i.PaymentStatus == PaymentStatus.Unpaid || i.PaymentStatus == PaymentStatus.Overdue)
                    .ToList();

                var dto = new PendingCreditReportDto
                {
                    CustomerId = c.Id,
                    FullName = c.FullName,
                    Email = c.Email,
                    PhoneNumber = c.PhoneNumber,
                    TotalPending = pendingInvoices.Sum(i => i.TotalAmount),
                    PendingInvoiceCount = pendingInvoices.Count,
                    PendingInvoices = pendingInvoices.Select(i => new InvoiceBasicDto
                    {
                        Id = i.Id,
                        InvoiceNumber = i.InvoiceNumber,
                        TotalAmount = i.TotalAmount,
                        SaleDate = i.SaleDate,
                        PaymentStatus = i.PaymentStatus.ToString()
                    }).ToList()
                };
                results.Add(dto);
            }

            return results.OrderByDescending(r => r.TotalPending).ToList();
        }
    }
}
