using GhanaHybridRentalApi.Data;
using Microsoft.EntityFrameworkCore;

namespace GhanaHybridRentalApi.Services;

/// <summary>
/// Background service that automatically cancels unpaid bookings after 4 hours
/// to free up vehicle availability for other renters.
/// </summary>
public class UnpaidBookingCancellationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<UnpaidBookingCancellationService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(15); // Check every 15 minutes
    private readonly TimeSpan _paymentTimeout = TimeSpan.FromHours(4); // Cancel after 4 hours

    public UnpaidBookingCancellationService(
        IServiceProvider serviceProvider,
        ILogger<UnpaidBookingCancellationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Unpaid Booking Cancellation Service started");

        // Wait 1 minute before first run to allow app to fully start
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CancelExpiredUnpaidBookingsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in unpaid booking cancellation service");
            }

            // Wait before next check
            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("Unpaid Booking Cancellation Service stopped");
    }

    private async Task CancelExpiredUnpaidBookingsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var notificationService = scope.ServiceProvider.GetService<INotificationService>();

        var cutoffTime = DateTime.UtcNow - _paymentTimeout;

        // Find bookings that are:
        // 1. Status = "pending_payment" (reserved but not paid)
        // 2. PaymentStatus = "unpaid"
        // 3. Created more than 4 hours ago
        // 4. NOT partner bookings (PaymentChannel != 'partner')
        var expiredBookings = await db.Bookings
            .Include(b => b.Renter)
            .Include(b => b.Vehicle)
            .Where(b => b.Status == "pending_payment" &&
                       b.PaymentStatus == "unpaid" &&
                       b.PaymentChannel != "partner" && // Exclude partner bookings from auto-cancellation
                       b.CreatedAt < cutoffTime)
            .ToListAsync();

        if (!expiredBookings.Any())
        {
            _logger.LogInformation("No expired unpaid bookings found");
            return;
        }

        _logger.LogInformation("Found {Count} expired unpaid bookings to cancel", expiredBookings.Count);

        foreach (var booking in expiredBookings)
        {
            try
            {
                // Cancel the booking
                booking.Status = "cancelled";
                booking.UpdatedAt = DateTime.UtcNow;

                _logger.LogInformation(
                    "Auto-cancelled unpaid booking {BookingReference} (ID: {BookingId}) - Created: {CreatedAt}, Timeout: 4 hours",
                    booking.BookingReference,
                    booking.Id,
                    booking.CreatedAt);

                // Send notification to renter about cancellation
                if (notificationService != null)
                {
                    try
                    {
                        var renterEmail = booking.Renter?.Email ?? booking.GuestEmail;
                        var renterPhone = booking.Renter?.Phone ?? booking.GuestPhone;
                        var renterName = booking.Renter?.FirstName ?? booking.GuestFirstName ?? "Valued Customer";

                        if (!string.IsNullOrWhiteSpace(renterEmail) || !string.IsNullOrWhiteSpace(renterPhone))
                        {
                            var message = $@"Dear {renterName},

Your booking reservation {booking.BookingReference} for {booking.Vehicle?.Make} {booking.Vehicle?.Model} has been automatically cancelled due to non-payment.

The booking was reserved on {booking.CreatedAt:MMM dd, yyyy 'at' h:mm tt} but payment was not received within 4 hours.

The vehicle is now available for other customers. If you still need to rent this vehicle, please create a new booking.

Need help? Contact us at support@ryverental.info

Best regards,
Ryve Rental Team";

                            var notificationJob = new Models.NotificationJob
                            {
                                TargetUserId = booking.RenterId,
                                TargetEmail = renterEmail,
                                TargetPhone = renterPhone,
                                ChannelsJson = System.Text.Json.JsonSerializer.Serialize(new[] { "email", "inapp" }),
                                Subject = "Reservation Cancelled",
                                Message = message,
                                Status = "pending",
                                SendImmediately = true,
                                CreatedAt = DateTime.UtcNow
                            };

                            await notificationService.CreateNotificationJobAsync(notificationJob);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send cancellation notification for booking {BookingId}", booking.Id);
                        // Don't fail the cancellation if notification fails
                    }
                }

                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cancel booking {BookingId}", booking.Id);
            }
        }

        _logger.LogInformation("Completed processing {Count} expired unpaid bookings", expiredBookings.Count);
    }
}
