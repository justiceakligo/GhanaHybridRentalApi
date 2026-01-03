using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GhanaHybridRentalApi.Services;

public class NotificationJobProcessor : BackgroundService
{
    private readonly ILogger<NotificationJobProcessor> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;

    public NotificationJobProcessor(ILogger<NotificationJobProcessor> logger, IServiceScopeFactory scopeFactory, IConfiguration config)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("NotificationJobProcessor starting");
        var intervalSeconds = _config.GetValue<int>("NotificationSettings:ProcessingIntervalSeconds", 15);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
                var processed = await notificationService.ProcessPendingJobsAsync(50);
                if (processed > 0)
                {
                    _logger.LogInformation("Processed {Count} notification jobs", processed);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing notification jobs");
            }

            await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
        }
        _logger.LogInformation("NotificationJobProcessor stopping");
    }
}