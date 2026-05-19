using Autopartspro.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Autopartspro.Infrastructure.Services;

/// <summary>
/// Periodically checks for unpaid sales invoices past the due period and notifies customers.
/// </summary>
public class EmailReminderService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<EmailReminderService> _logger;

    public EmailReminderService(
        IServiceScopeFactory scopeFactory,
        IConfiguration config,
        ILogger<EmailReminderService> logger)
    {
        _scopeFactory = scopeFactory;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalHours = Math.Max(1, _config.GetValue("PaymentReminders:CheckIntervalHours", 6));
        var interval = TimeSpan.FromHours(intervalHours);

        // Run once shortly after startup, then on interval.
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<IOverduePaymentReminderService>();
                await processor.ProcessDueRemindersAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Overdue payment reminder job failed.");
            }

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
