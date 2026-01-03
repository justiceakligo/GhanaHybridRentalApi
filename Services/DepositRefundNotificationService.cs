using GhanaHybridRentalApi.Data;
using GhanaHybridRentalApi.Models;
using Microsoft.EntityFrameworkCore;

namespace GhanaHybridRentalApi.Services;

public class DepositRefundNotificationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DepositRefundNotificationService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(6); // Check every 6 hours

    public DepositRefundNotificationService(
        IServiceProvider serviceProvider,
        ILogger<DepositRefundNotificationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Deposit Refund Notification Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckOverdueRefundsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking overdue refunds");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("Deposit Refund Notification Service stopped");
    }

    private async Task CheckOverdueRefundsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var now = DateTime.UtcNow;

        // Find refunds that are overdue and haven't been notified yet
        var overdueRefunds = await dbContext.DepositRefunds
            .Include(r => r.Booking)
                .ThenInclude(b => b.Renter)
            .Include(r => r.Booking)
                .ThenInclude(b => b.Vehicle)
            .Where(r => 
                r.Status == "pending" && 
                r.DueDate.HasValue && 
                r.DueDate < now && 
                !r.AdminNotified)
            .ToListAsync(cancellationToken);

        if (overdueRefunds.Any())
        {
            _logger.LogWarning("Found {Count} overdue refunds that need admin notification", overdueRefunds.Count);

            foreach (var refund in overdueRefunds)
            {
                try
                {
                    // Mark as notified
                    refund.AdminNotified = true;
                    refund.AdminNotifiedAt = now;

                    // Create audit log
                    var dueDate = refund.DueDate.GetValueOrDefault(now);
                    var daysOverdue = (now - dueDate).Days;

                    var auditLog = new RefundAuditLog
                    {
                        DepositRefundId = refund.Id,
                        Action = "admin_notified",
                        OldStatus = refund.Status,
                        NewStatus = refund.Status,
                        Notes = $"Admin notified about overdue refund. Due date: {dueDate:yyyy-MM-dd HH:mm}, Days overdue: {daysOverdue}"
                    };

                    dbContext.RefundAuditLogs.Add(auditLog);

                    // TODO: Send actual notification to admin
                    // Options:
                    // 1. Email notification
                    // 2. Create in-app notification
                    // 3. Send to Slack/Teams webhook
                    // 4. SMS to admin phone


                    _logger.LogInformation(
                        "Notified admin about overdue refund {RefundId} for booking {BookingReference}. Due: {DueDate}, Overdue by: {DaysOverdue} days",
                        refund.Id,
                        refund.Booking?.BookingReference ?? "Unknown",
                        dueDate,
                        daysOverdue
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error notifying admin about refund {RefundId}", refund.Id);
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }
        else
        {
            _logger.LogInformation("No overdue refunds found requiring notification");
        }
    }
}
