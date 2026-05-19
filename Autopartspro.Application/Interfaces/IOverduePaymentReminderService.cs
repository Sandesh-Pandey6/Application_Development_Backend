namespace Autopartspro.Application.Interfaces;

public interface IOverduePaymentReminderService
{
    /// <summary>
    /// Sends overdue reminders (notification + email) for unpaid sales invoices past the configured due period.
    /// </summary>
    Task ProcessDueRemindersAsync(CancellationToken cancellationToken = default);
}
