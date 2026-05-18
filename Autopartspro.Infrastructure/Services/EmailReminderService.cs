using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Autopartspro.Infrastructure.Services;

public class EmailReminderService : BackgroundService
{
    private readonly ILogger<EmailReminderService> _logger;

    public EmailReminderService(ILogger<EmailReminderService> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("EmailReminderService running at {time}", DateTimeOffset.Now);
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}
