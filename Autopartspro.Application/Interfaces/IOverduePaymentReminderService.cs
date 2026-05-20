namespace Autopartspro.Application.Interfaces;

public interface IOverduePaymentReminderService
{
    
    /// Sends overdue reminders (notification + email) for unpaid sales invoices past the configured due period.
    
    Task ProcessDueRemindersAsync(CancellationToken cancellationToken = default);
}
