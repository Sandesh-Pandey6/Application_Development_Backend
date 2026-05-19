using Autopartspro.Application.Interfaces;
using Autopartspro.Domain.Enums;
using Autopartspro.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Autopartspro.Infrastructure.Services;

public class OverduePaymentReminderService : IOverduePaymentReminderService
{
    private readonly AppDbContext _db;
    private readonly IUserNotificationService _notifications;
    private readonly IEmailService _email;
    private readonly IConfiguration _config;
    private readonly ILogger<OverduePaymentReminderService> _logger;

    public OverduePaymentReminderService(
        AppDbContext db,
        IUserNotificationService notifications,
        IEmailService email,
        IConfiguration config,
        ILogger<OverduePaymentReminderService> logger)
    {
        _db = db;
        _notifications = notifications;
        _email = email;
        _config = config;
        _logger = logger;
    }

    public async Task ProcessDueRemindersAsync(CancellationToken cancellationToken = default)
    {
        var overdueDays = Math.Max(1, _config.GetValue("PaymentReminders:OverdueDays", 3));
        var cutoff = DateTime.UtcNow.AddDays(-overdueDays);

        var invoices = await _db.SalesInvoices
            .Include(i => i.Customer)
            .Include(i => i.Staff)
            .Where(i =>
                i.PaymentStatus == PaymentStatus.Unpaid &&
                i.SaleDate <= cutoff &&
                i.OverdueReminderSentAt == null)
            .ToListAsync(cancellationToken);

        if (invoices.Count == 0)
            return;

        _logger.LogInformation(
            "Processing {Count} overdue unpaid invoice(s) (older than {Days} day(s)).",
            invoices.Count,
            overdueDays);

        var sent = 0;

        foreach (var invoice in invoices)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (invoice.Customer is null)
                continue;

            try
            {
                var daysSinceSale = (int)Math.Floor((DateTime.UtcNow - invoice.SaleDate).TotalDays);

                await _notifications.NotifyUserAsync(
                    invoice.CustomerId,
                    $"Invoice {invoice.InvoiceNumber} for Rs. {invoice.TotalAmount:N2} is overdue " +
                    $"({daysSinceSale} days since purchase, unpaid for more than {overdueDays} days). " +
                    "Please arrange payment at your earliest convenience.",
                    NotificationType.CreditReminder);

                var staff = invoice.Staff;
                var staffReplyEmail = !string.IsNullOrWhiteSpace(staff?.BusinessEmail)
                    ? staff!.BusinessEmail
                    : staff?.Email;
                var staffName = string.IsNullOrWhiteSpace(staff?.FullName)
                    ? "AutoPartsPro"
                    : staff!.FullName;

                if (!string.IsNullOrWhiteSpace(invoice.Customer.Email))
                {
                    await _email.SendOverdueInvoiceReminderEmailAsync(
                        invoice.Customer.Email.Trim(),
                        invoice.Customer.FullName,
                        invoice.InvoiceNumber,
                        invoice.TotalAmount,
                        invoice.SaleDate,
                        overdueDays,
                        daysSinceSale,
                        staffReplyEmail,
                        staffName);
                }

                invoice.OverdueReminderSentAt = DateTime.UtcNow;
                invoice.UpdatedAt = DateTime.UtcNow;
                sent++;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed overdue reminder for invoice {InvoiceNumber} (customer {CustomerId}).",
                    invoice.InvoiceNumber,
                    invoice.CustomerId);
            }
        }

        if (sent > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Sent {Sent} overdue payment reminder(s).", sent);
        }
    }
}
