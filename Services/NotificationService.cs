using GhanaHybridRentalApi.Models;
using GhanaHybridRentalApi.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Npgsql;
using QRCoder;

namespace GhanaHybridRentalApi.Services;

public interface INotificationService
{
    Task SendBookingConfirmationToCustomerAsync(Booking booking);
    Task SendBookingConfirmationToDriverAsync(Booking booking);
    Task SendBookingConfirmationToOwnerAsync(Booking booking);
    Task SendBookingConfirmedNotificationAsync(Booking booking); // When owner confirms
    Task SendPaymentReceivedNotificationAsync(Booking booking, decimal amount);
    Task SendPayoutRequestNotificationAsync(User owner, decimal amount);
    Task SendNewReviewNotificationAsync(User owner, string reviewText, int rating);
    Task SendReportFiledNotificationAsync(string reportType, string description);
    Task SendReceiptAsync(Booking booking, string email);
    Task SendPickupReminderAsync(Booking booking);
    Task SendReturnReminderAsync(Booking booking);
    Task SendVehicleReturnedNotificationAsync(Booking booking);
    Task SendBookingCompletedNotificationAsync(Booking booking);
    Task<bool> SendOwnerNotificationAsync(User owner, string subject, string message);

    // IntegrationPartner application emails
    Task SendIntegrationPartnerApplicationReceivedAsync(Models.IntegrationPartner partner);
    Task SendIntegrationPartnerApplicationApprovedAsync(Models.IntegrationPartner partner, string apiKey);
    Task SendIntegrationPartnerApplicationRejectedAsync(string email, string applicationReference, string reason);

    // New: create a scheduled or immediate notification job
    Task<Guid> CreateNotificationJobAsync(Models.NotificationJob job);

    // Process pending scheduled jobs (limit cap)
    Task<int> ProcessPendingJobsAsync(int limit = 50);
}

public class NotificationService : INotificationService
{
    private readonly IWhatsAppSender _whatsAppSender;
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly IReceiptTemplateService _receiptTemplateService;
    private readonly ILogger<NotificationService> _logger;
    private readonly AppDbContext _db;

    public NotificationService(
        IWhatsAppSender whatsAppSender,
        IEmailService emailService,
        IEmailTemplateService emailTemplateService,
        IReceiptTemplateService receiptTemplateService,
        ILogger<NotificationService> logger,
        AppDbContext db)
    {
        _whatsAppSender = whatsAppSender;
        _emailService = emailService;
        _emailTemplateService = emailTemplateService;
        _receiptTemplateService = receiptTemplateService;
        _logger = logger;
        _db = db;
    }

    private async Task<NotificationSettings> GetNotificationSettingsAsync()
    {
        var setting = await _db.GlobalSettings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == "NotificationSettings");
        
        if (setting == null)
            return new NotificationSettings(); // Default: all enabled

        try
        {
            return JsonSerializer.Deserialize<NotificationSettings>(setting.ValueJson) 
                ?? new NotificationSettings();
        }
        catch
        {
            return new NotificationSettings();
        }
    }

    public async Task SendBookingConfirmationToCustomerAsync(Booking booking)
    {
        var settings = await GetNotificationSettingsAsync();
        if (!settings.NewBooking.Email)
        {
            _logger.LogInformation("New booking email notifications are disabled");
            return;
        }

        // Reload booking with related entities to ensure they're available in this DbContext
        var bookingWithEntities = await _db.Bookings
            .Include(b => b.Renter)
            .Include(b => b.Vehicle)
            .Include(b => b.PickupInspection)
            .FirstOrDefaultAsync(b => b.Id == booking.Id);

        if (bookingWithEntities == null)
        {
            _logger.LogWarning("Cannot send customer notification: Booking not found");
            return;
        }

        if (bookingWithEntities.Renter == null)
        {
            _logger.LogWarning("Cannot send customer notification: Renter is null");
            return;
        }

        // Use the reloaded booking for the rest of the method
        booking = bookingWithEntities;

        // Generate pickup inspection link for QR
        var pickupUrl = string.Empty;
        if (booking.PickupInspection?.MagicLinkToken != null)
        {
            pickupUrl = $"https://api.ryverental.com/inspect/{booking.PickupInspection.MagicLinkToken}";
        }

        // Generate QR code image (base64) for booking reference and pickup URL
        var qrCodeImage = GenerateQRCodeBase64(booking.BookingReference, pickupUrl);

        // Format pickup location from JSON
        var pickupLocation = "TBD";
        if (!string.IsNullOrWhiteSpace(booking.PickupLocationJson))
        {
            try
            {
                var locationObj = JsonSerializer.Deserialize<JsonElement>(booking.PickupLocationJson);
                if (locationObj.TryGetProperty("cityName", out var cityName))
                {
                    pickupLocation = cityName.GetString() ?? "TBD";
                }
                else if (locationObj.TryGetProperty("address", out var address))
                {
                    pickupLocation = address.GetString() ?? "TBD";
                }
            }
            catch
            {
                pickupLocation = booking.PickupLocationJson;
            }
        }

        // Calculate trip duration in days
        var tripDuration = (int)Math.Ceiling((booking.ReturnDateTime - booking.PickupDateTime).TotalDays);

        // Get owner information for contact details
        var owner = await _db.Users
            .Include(u => u.OwnerProfile)
            .FirstOrDefaultAsync(u => u.Id == booking.OwnerId);
        
        var ownerName = owner != null ? $"{owner.FirstName} {owner.LastName}".Trim() : "Vehicle Owner";
        var ownerPhone = owner?.OwnerProfile?.BusinessPhone ?? owner?.Phone ?? "Contact support";
        var ownerAddress = owner?.OwnerProfile?.BusinessAddress ?? pickupLocation;
        var ownerGpsAddress = owner?.OwnerProfile?.GpsAddress ?? "";
        var pickupInstructions = owner?.OwnerProfile?.PickupInstructions ?? "Contact owner for pickup details";

        // Build placeholders for template
        var placeholders = new Dictionary<string, string>
        {
            { "customer_name", $"{booking.Renter.FirstName} {booking.Renter.LastName}".Trim() },
            { "booking_reference", booking.BookingReference },
            { "pickup_date", booking.PickupDateTime.ToString("MMM dd, yyyy") },
            { "pickup_time", booking.PickupDateTime.ToString("HH:mm") },
            { "pickup_location", pickupLocation },
            { "return_date", booking.ReturnDateTime.ToString("MMM dd, yyyy") },
            { "return_time", booking.ReturnDateTime.ToString("HH:mm") },
            { "return_location", FormatLocationFromJson(booking.ReturnLocationJson) },
            { "trip_duration", tripDuration.ToString() },
            { "vehicle_make", booking.Vehicle?.Make ?? "" },
            { "vehicle_model", booking.Vehicle?.Model ?? "" },
            { "vehicle_plate", booking.Vehicle?.PlateNumber ?? "" },
            { "vehicle_type", booking.WithDriver ? "With Professional Driver" : "Self-Drive" },
            { "currency", booking.Currency },
            { "rental_amount", booking.RentalAmount.ToString("F2") },
            { "driver_amount", (booking.DriverAmount ?? 0m).ToString("F2") },
            { "protection_amount", (booking.ProtectionAmount ?? 0m).ToString("F2") },
            { "platform_fee", (booking.PlatformFee ?? 0m).ToString("F2") },
            { "deposit_amount", booking.DepositAmount.ToString("F2") },
            { "promo_discount", (booking.PromoDiscountAmount ?? 0m).ToString("F2") },
            { "promo_display", (booking.PromoDiscountAmount.HasValue && booking.PromoDiscountAmount.Value > 0) ? "table-row" : "none" },
            { "total_amount", booking.TotalAmount.ToString("F2") },
            { "qr_link", pickupUrl },
            { "qr_code_image", qrCodeImage },
            { "qr_code", qrCodeImage },
            { "owner_name", ownerName },
            { "owner_phone", ownerPhone },
            { "owner_address", ownerAddress },
            { "owner_gps_address", ownerGpsAddress },
            { "pickup_instructions", pickupInstructions },
            { "inspection_link", pickupUrl },
            { "support_phone", await GetSupportPhoneAsync(false) },
            { "support_email", await GetSupportEmailAsync() }
        };

        try
        {
            // Render HTML email from template
            var htmlMessage = await _emailTemplateService.RenderTemplateAsync("booking_confirmation_customer", placeholders);
            var subject = await _emailTemplateService.RenderSubjectAsync("booking_confirmation_customer", placeholders);

            // Send WhatsApp (simplified text version)
            if (!string.IsNullOrEmpty(booking.Renter.Phone))
            {
                var whatsappMsg = $@"🔖 Booking Reserved!

Ref: {booking.BookingReference}
Status: ⚠️ PENDING PAYMENT

📅 Pickup: {booking.PickupDateTime:MMM dd, yyyy @ HH:mm}
📍 Location: {booking.PickupLocationJson ?? "TBD"}

🚙 Vehicle: {booking.Vehicle?.Make} {booking.Vehicle?.Model}

💰 Total: {booking.Currency} {booking.TotalAmount:F2}

{(booking.WithDriver ? "👨‍💼 With Professional Driver" : "🚗 Self-Drive")}

⚡ Complete payment to confirm your booking.

{(!string.IsNullOrEmpty(pickupUrl) ? $"✳️ Express Check-in: {pickupUrl}" : "")}

Thank you for choosing Ryve Rental!";
                await _whatsAppSender.SendBookingNotificationAsync(booking.Renter.Phone, whatsappMsg);
            }

            // Send Email (HTML from template)
            if (!string.IsNullOrEmpty(booking.Renter.Email))
            {
                await _emailService.SendEmailAsync(booking.Renter.Email, subject, htmlMessage);
            }

            _logger.LogInformation("Sent booking confirmation to customer {CustomerId} for booking {BookingRef}", 
                booking.Renter.Id, booking.BookingReference);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send booking confirmation using template for booking {BookingRef}", booking.BookingReference);
            throw;
        }
    }

    public async Task SendBookingConfirmationToDriverAsync(Booking booking)
    {
        var settings = await GetNotificationSettingsAsync();
        if (!settings.NewBooking.Email)
        {
            _logger.LogInformation("New booking email notifications are disabled");
            return;
        }

        if (!booking.WithDriver || booking.Driver == null || booking.Driver.DriverProfile == null)
        {
            return; // No driver assigned or not a with-driver booking
        }

        var driverName = $"{booking.Driver.FirstName} {booking.Driver.LastName}".Trim();
        var customerName = $"{booking.Renter?.FirstName} {booking.Renter?.LastName}".Trim();

        // Build placeholders for template
        var placeholders = new Dictionary<string, string>
        {
            { "driver_name", driverName },
            { "booking_reference", booking.BookingReference },
            { "customer_name", customerName },
            { "customer_phone", booking.Renter?.Phone ?? "" },
            { "pickup_date", booking.PickupDateTime.ToString("MMM dd, yyyy") },
            { "pickup_time", booking.PickupDateTime.ToString("HH:mm") },
            { "pickup_location", booking.PickupLocationJson ?? "TBD" },
            { "return_date", booking.ReturnDateTime.ToString("MMM dd, yyyy") },
            { "return_time", booking.ReturnDateTime.ToString("HH:mm") },
            { "trip_duration", (booking.ReturnDateTime.Date - booking.PickupDateTime.Date).Days.ToString() },
            { "vehicle_make", booking.Vehicle?.Make ?? "" },
            { "vehicle_model", booking.Vehicle?.Model ?? "" },
            { "vehicle_plate", booking.Vehicle?.PlateNumber ?? "" },
            { "support_phone", await GetSupportPhoneAsync(false) }
        };

        try
        {
            // Render HTML email from template
            var htmlMessage = await _emailTemplateService.RenderTemplateAsync("booking_confirmation_driver", placeholders);
            var subject = await _emailTemplateService.RenderSubjectAsync("booking_confirmation_driver", placeholders);

            // Send WhatsApp (simplified text version)
            if (!string.IsNullOrEmpty(booking.Driver.Phone))
            {
                var whatsappMsg = $@"🚗 NEW BOOKING ASSIGNMENT

Hi {driverName},

Ref: {booking.BookingReference}
Status: Confirmed

CUSTOMER
Name: {customerName}
Phone: {booking.Renter?.Phone}

VEHICLE
{booking.Vehicle?.Make} {booking.Vehicle?.Model}
Plate: {booking.Vehicle?.PlateNumber}

TRIP DETAILS
📅 Pickup: {booking.PickupDateTime:MMM dd, yyyy @ HH:mm}
📍 Location: {booking.PickupLocationJson ?? "TBD"}
Duration: {(booking.ReturnDateTime.Date - booking.PickupDateTime.Date).Days} days

✅ Please contact customer 1 day before pickup.";
                await _whatsAppSender.SendBookingNotificationAsync(booking.Driver.Phone, whatsappMsg);
            }

            // Send Email (HTML from template)
            if (!string.IsNullOrEmpty(booking.Driver.Email))
            {
                await _emailService.SendEmailAsync(booking.Driver.Email, subject, htmlMessage);
            }

            _logger.LogInformation("Sent booking confirmation to driver {DriverId} for booking {BookingRef}", 
                booking.DriverId, booking.BookingReference);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send driver notification using template for booking {BookingRef}", booking.BookingReference);
            throw;
        }
    }

    public async Task SendBookingConfirmationToOwnerAsync(Booking booking)
    {
        var settings = await GetNotificationSettingsAsync();
        if (!settings.NewBooking.Email)
        {
            _logger.LogInformation("New booking email notifications are disabled");
            return;
        }

        // Reload booking with related entities to ensure they're available in this DbContext
        var bookingWithEntities = await _db.Bookings
            .Include(b => b.Renter)
            .Include(b => b.Vehicle)
            .FirstOrDefaultAsync(b => b.Id == booking.Id);

        if (bookingWithEntities == null)
        {
            _logger.LogWarning("Cannot send owner notification: Booking not found");
            return;
        }

        // Use the reloaded booking
        booking = bookingWithEntities;

        // Load owner from database
        var owner = await _db.Users.FirstOrDefaultAsync(u => u.Id == booking.OwnerId);
        if (owner == null)
        {
            _logger.LogWarning("Cannot send owner notification: Owner not found for booking {BookingRef}", booking.BookingReference);
            return;
        }

        var ownerName = $"{owner.FirstName} {owner.LastName}".Trim();
        var customerName = booking.Renter != null 
            ? $"{booking.Renter.FirstName} {booking.Renter.LastName}".Trim() 
            : booking.GuestFirstName + " " + booking.GuestLastName;
        var customerPhone = booking.Renter?.Phone ?? booking.GuestPhone;
        var customerEmail = booking.Renter?.Email ?? booking.GuestEmail;
        
        // Format location from JSON to user-friendly text
        var pickupLocation = FormatLocationFromJson(booking.PickupLocationJson);
        var returnLocation = FormatLocationFromJson(booking.ReturnLocationJson);

        // Build placeholders for template
        var placeholders = new Dictionary<string, string>
        {
            { "owner_name", ownerName },
            { "booking_reference", booking.BookingReference },
            { "customer_name", customerName },
            { "customer_phone", customerPhone ?? "" },
            { "customer_email", customerEmail ?? "" },
            { "pickup_date", booking.PickupDateTime.ToString("MMM dd, yyyy") },
            { "pickup_time", booking.PickupDateTime.ToString("HH:mm") },
            { "pickup_location", pickupLocation },
            { "return_date", booking.ReturnDateTime.ToString("MMM dd, yyyy") },
            { "return_time", booking.ReturnDateTime.ToString("HH:mm") },
            { "return_location", returnLocation },
            { "trip_duration", (booking.ReturnDateTime.Date - booking.PickupDateTime.Date).Days.ToString() },
            { "vehicle_make", booking.Vehicle?.Make ?? "" },
            { "vehicle_model", booking.Vehicle?.Model ?? "" },
            { "vehicle_plate", booking.Vehicle?.PlateNumber ?? "" },
            { "vehicle_type", booking.WithDriver ? "With Driver" : "Self-Drive" },
            { "currency", booking.Currency },
            { "rental_amount", booking.RentalAmount.ToString("F2") },
            { "driver_amount", (booking.DriverAmount ?? 0).ToString("F2") },
            { "platform_fee", (booking.PlatformFee ?? 0).ToString("F2") },
            { "owner_total", (booking.RentalAmount + (booking.DriverAmount ?? 0)).ToString("F2") },
            { "owner_net_earnings", ((booking.RentalAmount + (booking.DriverAmount ?? 0)) - (booking.PlatformFee ?? 0)).ToString("F2") },
            { "support_email", "support@ryverental.com" }
        };

        try
        {
            // Render HTML email from template
            var htmlMessage = await _emailTemplateService.RenderTemplateAsync("booking_confirmation_owner", placeholders);
            var subject = await _emailTemplateService.RenderSubjectAsync("booking_confirmation_owner", placeholders);

            // Send WhatsApp (simplified text version)
            if (!string.IsNullOrWhiteSpace(owner.Phone))
            {
                var whatsappMsg = $@"🎉 NEW BOOKING RECEIVED

Hi {ownerName},

You have a new booking!

Ref: {booking.BookingReference}
Status: Pending Payment

CUSTOMER
Name: {customerName}
Phone: {customerPhone}
Email: {customerEmail}

VEHICLE
{booking.Vehicle?.Make} {booking.Vehicle?.Model}
Plate: {booking.Vehicle?.PlateNumber}

TRIP DETAILS
📅 Pickup: {booking.PickupDateTime:MMM dd, yyyy @ HH:mm}
📍 Location: {booking.PickupLocationJson ?? "TBD"}
Duration: {(booking.ReturnDateTime.Date - booking.PickupDateTime.Date).Days} days

💰 EARNINGS
Rental: {booking.Currency} {booking.RentalAmount:F2}
{(booking.WithDriver ? $"Driver Fee: {booking.Currency} {booking.DriverAmount:F2}\n" : "")}Total: {booking.Currency} {booking.TotalAmount:F2}

✅ Booking will be confirmed once payment is received.";
                await _whatsAppSender.SendBookingNotificationAsync(owner.Phone, whatsappMsg);
            }

            // Send Email (HTML from template)
            if (!string.IsNullOrWhiteSpace(owner.Email))
            {
                await _emailService.SendEmailAsync(owner.Email, subject, htmlMessage);
            }

            _logger.LogInformation("Sent new booking notification to owner {OwnerId} for booking {BookingRef}", 
                owner.Id, booking.BookingReference);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send owner notification using template for booking {BookingRef}", booking.BookingReference);
            throw;
        }
    }

    public async Task<bool> SendOwnerNotificationAsync(User owner, string subject, string message)
    {
        if (owner == null)
        {
            _logger.LogWarning("Cannot send owner notification: owner is null");
            return false;
        }

        var sent = false;

        if (!string.IsNullOrWhiteSpace(owner.Phone))
        {
            try
            {
                await _whatsAppSender.SendBookingNotificationAsync(owner.Phone, message);
                sent = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send WhatsApp to owner {OwnerId}", owner.Id);
            }
        }

        if (!string.IsNullOrWhiteSpace(owner.Email))
        {
            try
            {
                await _emailService.SendBookingConfirmationAsync(owner.Email, message);
                sent = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send Email to owner {OwnerId}", owner.Id);
            }
        }

        if (!sent)
            _logger.LogWarning("No contact method available or all sends failed for owner {OwnerId}", owner.Id);

        // Persist an in-app dashboard notification so owner sees this in their dashboard
        try
        {
            var n = new Notification
            {
                UserId = owner.Id,
                Title = subject ?? "Notification",
                Body = message ?? string.Empty,
                Read = false,
                CreatedAt = DateTime.UtcNow
            };
            _db.Notifications.Add(n);
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist in-app notification for owner {OwnerId}", owner.Id);
        }

        return sent;
    }
        
        private async Task EnsureNotificationJobsTableExistsAsync()
        {
            var sql = @"
CREATE TABLE IF NOT EXISTS ""NotificationJobs"" (
  ""Id"" uuid PRIMARY KEY,
  ""BookingId"" uuid NULL,
  ""TargetUserId"" uuid NULL,
  ""TargetEmail"" varchar(256) NULL,
  ""TargetPhone"" varchar(32) NULL,
  ""ChannelsJson"" text NOT NULL,
  ""Subject"" varchar(256) NOT NULL,
  ""Message"" text NOT NULL,
  ""TemplateName"" varchar(128) NULL,
  ""MetadataJson"" text NULL,
  ""ScheduledAt"" timestamp with time zone NULL,
  ""SendImmediately"" boolean NOT NULL DEFAULT false,
  ""Status"" varchar(32) NOT NULL,
  ""Attempts"" integer NOT NULL DEFAULT 0,
  ""LastAttemptAt"" timestamp with time zone NULL,
  ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT now(),
  ""UpdatedAt"" timestamp with time zone NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS ""IX_NotificationJobs_Status"" ON ""NotificationJobs"" (""Status"");
CREATE INDEX IF NOT EXISTS ""IX_NotificationJobs_ScheduledAt"" ON ""NotificationJobs"" (""ScheduledAt"");
";
            try
            {
                await _db.Database.ExecuteSqlRawAsync(sql);
                _logger.LogInformation("Ensured NotificationJobs table exists");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create NotificationJobs table on the fly");
                throw;
            }
        }
        public async Task<Guid> CreateNotificationJobAsync(Models.NotificationJob job)
        {
            if (job == null) throw new ArgumentNullException(nameof(job));
            job.Status = "pending";
            job.CreatedAt = DateTime.UtcNow;
            job.UpdatedAt = DateTime.UtcNow;
            _db.NotificationJobs.Add(job);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Created notification job {JobId} for booking {BookingId}", job.Id, job.BookingId);
            return job.Id;
        }

        public async Task<int> ProcessPendingJobsAsync(int limit = 50)
        {
            var now = DateTime.UtcNow;
            var jobs = new List<Models.NotificationJob>();
            try
            {
                jobs = await _db.NotificationJobs
                    .Where(j => j.Status == "pending" && (j.SendImmediately || (j.ScheduledAt.HasValue && j.ScheduledAt <= now)))
                    .OrderBy(j => j.ScheduledAt)
                    .Take(limit)
                    .ToListAsync();
            }
            catch (PostgresException ex) when (ex.SqlState == "42P01")
            {
                // Table does not exist; create it on the fly and retry once
                _logger.LogWarning("NotificationJobs table missing - creating it now: {Message}", ex.Message);
                await EnsureNotificationJobsTableExistsAsync();
                jobs = await _db.NotificationJobs
                    .Where(j => j.Status == "pending" && (j.SendImmediately || (j.ScheduledAt.HasValue && j.ScheduledAt <= now)))
                    .OrderBy(j => j.ScheduledAt)
                    .Take(limit)
                    .ToListAsync();
            }

            var processed = 0;

            foreach (var job in jobs)
            {
                job.Status = "queued";
                job.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                var successAny = false;
                var channels = new List<string>();
                try
                {
                    channels = JsonSerializer.Deserialize<List<string>>(job.ChannelsJson) ?? new List<string>();
                }
                catch
                {
                    _logger.LogWarning("Invalid ChannelsJson for job {JobId}", job.Id);
                }

                User? user = null;
                Booking? booking = null;
                if (job.TargetUserId.HasValue)
                {
                    user = await _db.Users.FirstOrDefaultAsync(u => u.Id == job.TargetUserId.Value);
                }
                if (job.BookingId.HasValue)
                {
                    booking = await _db.Bookings.Include(b => b.Renter).Include(b => b.Vehicle).FirstOrDefaultAsync(b => b.Id == job.BookingId.Value);
                }
                
                // Handle owner account approval (doesn't require booking)
                if (job.TemplateName == "owner_account_approved")
                {
                    if (!string.IsNullOrWhiteSpace(job.TargetEmail))
                    {
                        try
                        {
                            var ownerName = "Owner";
                            var email = job.TargetEmail;
                            var loginUrl = "https://ryverental.info/login";
                            var dashboardUrl = "https://ryverental.info/owner/dashboard";
                            
                            // Parse metadata if available
                            if (!string.IsNullOrWhiteSpace(job.MetadataJson))
                            {
                                using var metaDoc = JsonDocument.Parse(job.MetadataJson);
                                if (metaDoc.RootElement.TryGetProperty("ownerName", out var nameEl))
                                    ownerName = nameEl.GetString() ?? ownerName;
                                if (metaDoc.RootElement.TryGetProperty("loginUrl", out var loginEl))
                                    loginUrl = loginEl.GetString() ?? loginUrl;
                                if (metaDoc.RootElement.TryGetProperty("dashboardUrl", out var dashEl))
                                    dashboardUrl = dashEl.GetString() ?? dashboardUrl;
                            }
                            
                            var placeholders = new Dictionary<string, string>
                            {
                                { "ownerName", ownerName },
                                { "email", email },
                                { "loginUrl", loginUrl },
                                { "dashboardUrl", dashboardUrl }
                            };
                            
                            var htmlMessage = await _emailTemplateService.RenderTemplateAsync("owner_account_approved", placeholders);
                            var subject = await _emailTemplateService.RenderSubjectAsync("owner_account_approved", placeholders);
                            
                            await _emailService.SendEmailAsync(email, subject, htmlMessage);
                            
                            _logger.LogInformation("Sent owner approval email to {Email}", email);
                            
                            // Mark job as sent
                            job.Attempts += 1;
                            job.LastAttemptAt = DateTime.UtcNow;
                            job.Status = "sent";
                            job.UpdatedAt = DateTime.UtcNow;
                            await _db.SaveChangesAsync();
                            processed++;
                            continue; // Skip to next job
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to send owner approval email for job {JobId}", job.Id);
                            // Don't re-throw, let it fall through to mark as failed below
                        }
                    }
                }
                
                // Handle booking-related notification types with templates
                if (job.TemplateName == "pickup_reminder" || job.TemplateName == "return_reminder" || job.TemplateName == "deposit_refund_processed")
                {
                    // Extract booking ID from metadata
                    if (!string.IsNullOrWhiteSpace(job.MetadataJson))
                    {
                        try
                        {
                            using var doc = JsonDocument.Parse(job.MetadataJson);
                            if (doc.RootElement.TryGetProperty("bookingId", out var bookingIdEl))
                            {
                                var bookingId = bookingIdEl.GetGuid();
                                booking = await _db.Bookings
                                    .Include(b => b.Renter)
                                    .Include(b => b.Vehicle)
                                    .Include(b => b.Driver)
                                    .FirstOrDefaultAsync(b => b.Id == bookingId);
                                
                                if (booking != null)
                                {
                                    if (job.TemplateName == "pickup_reminder")
                                    {
                                        await SendPickupReminderAsync(booking);
                                    }
                                    else if (job.TemplateName == "return_reminder")
                                    {
                                        await SendReturnReminderAsync(booking);
                                    }
                                    else if (job.TemplateName == "deposit_refund_processed")
                                    {
                                        // Send deposit refund notification with template
                                        var email = booking.Renter?.Email ?? booking.GuestEmail;
                                        if (!string.IsNullOrWhiteSpace(email))
                                        {
                                            var amount = "0.00";
                                            var currency = booking.Currency;
                                            var bookingReference = booking.BookingReference;
                                            
                                            // Parse metadata for actual refund details
                                            try
                                            {
                                                using var metaDoc = JsonDocument.Parse(job.MetadataJson);
                                                if (metaDoc.RootElement.TryGetProperty("amount", out var amountEl))
                                                    amount = amountEl.GetDecimal().ToString("F2");
                                                if (metaDoc.RootElement.TryGetProperty("currency", out var currencyEl))
                                                    currency = currencyEl.GetString() ?? currency;
                                                if (metaDoc.RootElement.TryGetProperty("bookingReference", out var refEl))
                                                    bookingReference = refEl.GetString() ?? bookingReference;
                                            }
                                            catch { }
                                            
                                            var placeholders = new Dictionary<string, string>
                                            {
                                                { "customer_name", booking.Renter?.FirstName ?? "Valued Customer" },
                                                { "amount", amount },
                                                { "currency", currency },
                                                { "booking_reference", bookingReference },
                                                { "vehicle_name", $"{booking.Vehicle?.Make} {booking.Vehicle?.Model}" },
                                                { "booking_id", booking.Id.ToString() }
                                            };
                                            
                                            var htmlMessage = await _emailTemplateService.RenderTemplateAsync("deposit_refund_processed", placeholders);
                                            var subject = await _emailTemplateService.RenderSubjectAsync("deposit_refund_processed", placeholders);
                                            
                                            await _emailService.SendEmailAsync(email, subject, htmlMessage);
                                            
                                            // Send WhatsApp notification too
                                            var phone = booking.Renter?.Phone ?? booking.GuestPhone;
                                            if (!string.IsNullOrWhiteSpace(phone))
                                            {
                                                var whatsappMessage = $"💰 Deposit Refund Processed\n\nYour refund of {currency} {amount} for booking {bookingReference} has been processed successfully.\n\nThe funds should reflect in your mobile money account within 5-10 minutes.\n\nThank you for choosing Ryve Rental!";
                                                await _whatsAppSender.SendBookingNotificationAsync(phone, whatsappMessage);
                                            }
                                        }
                                    }
                                    
                                    // Mark job as sent
                                    job.Attempts += 1;
                                    job.LastAttemptAt = DateTime.UtcNow;
                                    job.Status = "sent";
                                    job.UpdatedAt = DateTime.UtcNow;
                                    await _db.SaveChangesAsync();
                                    processed++;
                                    continue; // Skip to next job
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to process template notification job {JobId}", job.Id);
                        }
                    }
                }

                try
                {
                    foreach (var ch in channels)
                    {
                        switch (ch.ToLowerInvariant())
                        {
                            case "inapp":
                                if (job.TargetUserId.HasValue && user != null)
                                {
                                    var n = new Notification
                                    {
                                        UserId = user.Id,
                                        Title = job.Subject ?? "Notification",
                                        Body = job.Message ?? string.Empty,
                                        Read = false,
                                        CreatedAt = DateTime.UtcNow
                                    };
                                    _db.Notifications.Add(n);
                                    await _db.SaveChangesAsync();
                                    successAny = true;
                                }
                                break;
                            case "email":
                                var email = user?.Email ?? job.TargetEmail ?? (booking?.Renter?.Email ?? booking?.GuestEmail);
                                if (!string.IsNullOrWhiteSpace(email))
                                {
                                    await _emailService.SendEmailAsync(email, job.Subject ?? "Notification", job.Message ?? string.Empty);
                                    successAny = true;
                                }
                                break;
                            case "whatsapp":
                                var phone = user?.Phone ?? job.TargetPhone ?? (booking?.Renter?.Phone ?? booking?.GuestPhone);
                                if (!string.IsNullOrWhiteSpace(phone))
                                {
                                    await _whatsAppSender.SendBookingNotificationAsync(phone, job.Message ?? job.Subject ?? string.Empty);
                                    successAny = true;
                                }
                                break;
                            case "sms":
                                var smsPhone = user?.Phone ?? job.TargetPhone ?? (booking?.Renter?.Phone ?? booking?.GuestPhone);
                                if (!string.IsNullOrWhiteSpace(smsPhone))
                                {
                                    _logger.LogInformation("SMS requested for job {JobId} to {Phone} (SMS provider not implemented)", job.Id, smsPhone);
                                    successAny = true; // treat as success for now
                                }
                                break;
                            case "push":
                                _logger.LogInformation("Push requested for job {JobId} (push provider not implemented)", job.Id);
                                successAny = true; // placeholder
                                break;
                            default:
                                _logger.LogWarning("Unknown channel '{Channel}' for job {JobId}", ch, job.Id);
                                break;
                        }
                    }

                    job.Attempts += 1;
                    job.LastAttemptAt = DateTime.UtcNow;
                    job.Status = successAny ? "sent" : "failed";
                    job.UpdatedAt = DateTime.UtcNow;
                    await _db.SaveChangesAsync();
                    processed++;
                }
                catch (Exception ex)
                {
                    job.Attempts += 1;
                    job.LastAttemptAt = DateTime.UtcNow;
                    job.Status = "failed";
                    job.UpdatedAt = DateTime.UtcNow;
                    await _db.SaveChangesAsync();
                    _logger.LogError(ex, "Failed to process notification job {JobId}", job.Id);
                }
            }

            return processed;
        }

    public async Task SendBookingConfirmedNotificationAsync(Booking booking)
    {
        var settings = await GetNotificationSettingsAsync();
        if (!settings.BookingConfirmed.Email)
        {
            _logger.LogInformation("Booking confirmed email notifications are disabled");
            return;
        }

        if (booking.Renter == null)
            return;

        // Format pickup location from JSON
        var pickupLocation = "TBD";
        if (!string.IsNullOrWhiteSpace(booking.PickupLocationJson))
        {
            try
            {
                var locationObj = JsonSerializer.Deserialize<JsonElement>(booking.PickupLocationJson);
                if (locationObj.TryGetProperty("cityName", out var cityName))
                {
                    pickupLocation = cityName.GetString() ?? "TBD";
                }
                else if (locationObj.TryGetProperty("address", out var address))
                {
                    pickupLocation = address.GetString() ?? "TBD";
                }
            }
            catch
            {
                pickupLocation = booking.PickupLocationJson;
            }
        }

        // Calculate trip duration in days
        var tripDuration = (int)Math.Ceiling((booking.ReturnDateTime - booking.PickupDateTime).TotalDays);

        // Get owner and inspection information
        var owner = await _db.Users
            .Include(u => u.OwnerProfile)
            .FirstOrDefaultAsync(u => u.Id == booking.OwnerId);
        
        var ownerName = owner != null ? $"{owner.FirstName} {owner.LastName}".Trim() : "Vehicle Owner";
        var ownerPhone = owner?.OwnerProfile?.BusinessPhone ?? owner?.Phone ?? "Contact support";
        var ownerAddress = owner?.OwnerProfile?.BusinessAddress ?? pickupLocation;
        var ownerGpsAddress = owner?.OwnerProfile?.GpsAddress ?? "";
        var pickupInstructions = owner?.OwnerProfile?.PickupInstructions ?? "Contact owner for pickup details";

        // Generate inspection link
        var inspectionLink = "";
        if (booking.PickupInspection != null && !string.IsNullOrWhiteSpace(booking.PickupInspection.MagicLinkToken))
        {
            inspectionLink = $"https://api.ryverental.com/inspect/{booking.PickupInspection.MagicLinkToken}";
        }

        // Generate QR code for booking inspection
        var qrCodeBase64 = "";
        if (!string.IsNullOrWhiteSpace(inspectionLink))
        {
            try
            {
                using var qrGenerator = new QRCoder.QRCodeGenerator();
                using var qrCodeData = qrGenerator.CreateQrCode(inspectionLink, QRCoder.QRCodeGenerator.ECCLevel.Q);
                using var qrCode = new QRCoder.PngByteQRCode(qrCodeData);
                var qrCodeBytes = qrCode.GetGraphic(20);
                qrCodeBase64 = $"data:image/png;base64,{Convert.ToBase64String(qrCodeBytes)}";
            }
            catch (Exception qrEx)
            {
                _logger.LogWarning(qrEx, "Failed to generate QR code for booking {BookingRef}", booking.BookingReference);
            }
        }

        var placeholders = new Dictionary<string, string>
        {
            { "customer_name", $"{booking.Renter.FirstName} {booking.Renter.LastName}".Trim() },
            { "booking_reference", booking.BookingReference },
            { "pickup_date", booking.PickupDateTime.ToString("MMM dd, yyyy") },
            { "pickup_time", booking.PickupDateTime.ToString("HH:mm") },
            { "pickup_location", pickupLocation },
            { "return_date", booking.ReturnDateTime.ToString("MMM dd, yyyy") },
            { "return_time", booking.ReturnDateTime.ToString("HH:mm") },
            { "trip_duration", tripDuration.ToString() },
            { "vehicle_make", booking.Vehicle?.Make ?? "" },
            { "vehicle_model", booking.Vehicle?.Model ?? "" },
            { "vehicle_plate", booking.Vehicle?.PlateNumber ?? "" },
            { "vehicle_type", booking.WithDriver ? "With Professional Driver" : "Self-Drive" },
            { "currency", booking.Currency },
            { "rental_amount", booking.RentalAmount.ToString("F2") },
            { "driver_amount", (booking.DriverAmount ?? 0m).ToString("F2") },
            { "protection_amount", (booking.ProtectionAmount ?? 0m).ToString("F2") },
            { "platform_fee", (booking.PlatformFee ?? 0m).ToString("F2") },
            { "deposit_amount", booking.DepositAmount.ToString("F2") },
            { "promo_discount", (booking.PromoDiscountAmount ?? 0m).ToString("F2") },
            { "promo_display", (booking.PromoDiscountAmount.HasValue && booking.PromoDiscountAmount.Value > 0) ? "table-row" : "none" },
            { "total_amount", booking.TotalAmount.ToString("F2") },
            { "owner_name", ownerName },
            { "owner_phone", ownerPhone },
            { "owner_address", ownerAddress },
            { "owner_gps_address", ownerGpsAddress },
            { "pickup_instructions", pickupInstructions },
            { "inspection_link", inspectionLink },
            { "qr_code", qrCodeBase64 },
            { "support_email", "support@ryverental.com" }
        };

        // Check if this is a partner booking
        var isPartnerBooking = booking.PaymentChannel == "partner" && booking.IntegrationPartnerId.HasValue;
        string partnerName = "";
        if (isPartnerBooking)
        {
            var partner = await _db.IntegrationPartners.FindAsync(booking.IntegrationPartnerId);
            partnerName = partner?.Name ?? "our partner";
            placeholders["partner_name"] = partnerName;
            placeholders["payment_note"] = $"Payment processed via {partnerName}";
        }

        try
        {
            // Use different template for partner bookings
            var templateName = isPartnerBooking ? "booking_confirmed_partner" : "booking_confirmed";
            var htmlMessage = await _emailTemplateService.RenderTemplateAsync(templateName, placeholders);
            var subject = await _emailTemplateService.RenderSubjectAsync(templateName, placeholders);

            if (!string.IsNullOrEmpty(booking.Renter.Phone))
            {
                var whatsappMsg = $@"✅ BOOKING CONFIRMED

Your booking has been confirmed by the owner!

Ref: {booking.BookingReference}
Vehicle: {booking.Vehicle?.Make} {booking.Vehicle?.Model}

📅 Pickup: {booking.PickupDateTime:MMM dd, yyyy @ HH:mm}
📅 Return: {booking.ReturnDateTime:MMM dd, yyyy @ HH:mm}

💰 Total: {booking.Currency} {booking.TotalAmount:F2}

Get ready for your trip!";
                await _whatsAppSender.SendBookingNotificationAsync(booking.Renter.Phone, whatsappMsg);
            }

            if (!string.IsNullOrEmpty(booking.Renter.Email))
            {
                var attachments = new List<EmailAttachment>();
                
                // Get signed rental agreement if exists
                var rentalAgreement = await _db.RentalAgreementAcceptances
                    .FirstOrDefaultAsync(a => a.BookingId == booking.Id && a.RenterId == booking.RenterId);

                if (rentalAgreement != null)
                {
                    // Generate rental agreement PDF attachment using QuestPDF
                    var agreementBytes = await _receiptTemplateService.GenerateRentalAgreementPdfAsync(booking, rentalAgreement);
                    attachments.Add(new EmailAttachment($"rental-agreement-{booking.BookingReference}.pdf", agreementBytes, "application/pdf"));
                }
                
                // Always attach receipt PDF
                try
                {
                    var receiptBytes = await _receiptTemplateService.GenerateReceiptPdfAsync(booking);
                    attachments.Add(new EmailAttachment($"receipt-{booking.BookingReference}.pdf", receiptBytes, "application/pdf"));
                }
                catch (Exception receiptEx)
                {
                    _logger.LogWarning(receiptEx, "Failed to generate receipt PDF for booking {BookingRef}", booking.BookingReference);
                }
                
                if (attachments.Count > 0)
                {
                    await _emailService.SendEmailWithAttachmentsAsync(booking.Renter.Email, subject, htmlMessage, attachments);
                }
                else
                {
                    await _emailService.SendEmailAsync(booking.Renter.Email, subject, htmlMessage);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send booking confirmed notification using template for booking {BookingRef}", booking.BookingReference);
            throw;
        }
    }

    private string GenerateRentalAgreementDocument(Booking booking, RentalAgreementAcceptance acceptance)
    {
        var sb = new System.Text.StringBuilder();
        
        sb.AppendLine("═══════════════════════════════════════════════");
        sb.AppendLine("                VEHICLE RENTAL AGREEMENT");
        sb.AppendLine("                     RYVEPOOL");
        sb.AppendLine("═══════════════════════════════════════════════");
        sb.AppendLine();
        sb.AppendLine($"Agreement Date: {acceptance.AcceptedAt:yyyy-MM-dd HH:mm} UTC");
        sb.AppendLine($"Booking Reference: {booking.BookingReference}");
        sb.AppendLine($"Template Version: {acceptance.TemplateCode} v{acceptance.TemplateVersion}");
        sb.AppendLine();
        sb.AppendLine("───────────────────────────────────────────────");
        sb.AppendLine("RENTER INFORMATION");
        sb.AppendLine("───────────────────────────────────────────────");
        sb.AppendLine($"Name: {booking.Renter?.FirstName} {booking.Renter?.LastName}");
        sb.AppendLine($"Email: {booking.Renter?.Email}");
        sb.AppendLine($"Phone: {booking.Renter?.Phone}");
        sb.AppendLine();
        sb.AppendLine("───────────────────────────────────────────────");
        sb.AppendLine("VEHICLE INFORMATION");
        sb.AppendLine("───────────────────────────────────────────────");
        sb.AppendLine($"Vehicle: {booking.Vehicle?.Make} {booking.Vehicle?.Model} {booking.Vehicle?.Year}");
        sb.AppendLine($"Plate Number: {booking.Vehicle?.PlateNumber}");
        sb.AppendLine();
        sb.AppendLine("───────────────────────────────────────────────");
        sb.AppendLine("RENTAL PERIOD");
        sb.AppendLine("───────────────────────────────────────────────");
        sb.AppendLine($"Pickup: {booking.PickupDateTime:yyyy-MM-dd HH:mm}");
        sb.AppendLine($"Return: {booking.ReturnDateTime:yyyy-MM-dd HH:mm}");
        sb.AppendLine();
        sb.AppendLine("───────────────────────────────────────────────");
        sb.AppendLine("AGREEMENT TERMS");
        sb.AppendLine("───────────────────────────────────────────────");
        sb.AppendLine();
        sb.AppendLine(acceptance.AgreementSnapshot ?? "[Agreement content]");
        sb.AppendLine();
        sb.AppendLine("───────────────────────────────────────────────");
        sb.AppendLine("ACCEPTANCE CONFIRMATIONS");
        sb.AppendLine("───────────────────────────────────────────────");
        sb.AppendLine($"✓ No Smoking Policy: {(acceptance.AcceptedNoSmoking ? "ACCEPTED" : "NOT ACCEPTED")}");
        sb.AppendLine($"✓ Fines & Tickets Responsibility: {(acceptance.AcceptedFinesAndTickets ? "ACCEPTED" : "NOT ACCEPTED")}");
        sb.AppendLine($"✓ Accident Procedure: {(acceptance.AcceptedAccidentProcedure ? "ACCEPTED" : "NOT ACCEPTED")}");
        sb.AppendLine();
        sb.AppendLine("───────────────────────────────────────────────");
        sb.AppendLine("DIGITAL SIGNATURE");
        sb.AppendLine("───────────────────────────────────────────────");
        sb.AppendLine($"Signed By: {booking.Renter?.FirstName} {booking.Renter?.LastName}");
        sb.AppendLine($"Date & Time: {acceptance.AcceptedAt:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"IP Address: {acceptance.IpAddress}");
        sb.AppendLine();
        sb.AppendLine("═══════════════════════════════════════════════");
        sb.AppendLine("This is a legally binding digital agreement.");
        sb.AppendLine("Acceptance was recorded electronically with audit trail.");
        sb.AppendLine("═══════════════════════════════════════════════");
        
        return sb.ToString();
    }

    public async Task SendPaymentReceivedNotificationAsync(Booking booking, decimal amount)
    {
        var settings = await GetNotificationSettingsAsync();
        if (!settings.PaymentReceived.Email)
        {
            _logger.LogInformation("Payment received email notifications are disabled");
            return;
        }

        if (booking.Renter == null)
            return;

        var placeholders = new Dictionary<string, string>
        {
            { "customer_name", $"{booking.Renter.FirstName} {booking.Renter.LastName}".Trim() },
            { "booking_reference", booking.BookingReference },
            { "payment_amount", amount.ToString("F2") },
            { "currency", booking.Currency },
            { "vehicle_make", booking.Vehicle?.Make ?? "" },
            { "vehicle_model", booking.Vehicle?.Model ?? "" },
            { "pickup_date", booking.PickupDateTime.ToString("MMM dd, yyyy") },
            { "transaction_id", booking.BookingReference },
            { "support_email", "support@ryverental.com" }
        };

        try
        {
            var htmlMessage = await _emailTemplateService.RenderTemplateAsync("payment_received", placeholders);
            var subject = await _emailTemplateService.RenderSubjectAsync("payment_received", placeholders);

            if (!string.IsNullOrEmpty(booking.Renter.Phone))
            {
                var whatsappMsg = $@"💰 PAYMENT RECEIVED

Your payment has been successfully processed!

Ref: {booking.BookingReference}
Amount: {booking.Currency} {amount:F2}

Vehicle: {booking.Vehicle?.Make} {booking.Vehicle?.Model}
📅 Pickup: {booking.PickupDateTime:MMM dd, yyyy}

Thank you for your payment!";
                await _whatsAppSender.SendBookingNotificationAsync(booking.Renter.Phone, whatsappMsg);
            }

            if (!string.IsNullOrEmpty(booking.Renter.Email))
            {
                await _emailService.SendEmailAsync(booking.Renter.Email, subject, htmlMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send payment received notification using template for booking {BookingRef}", booking.BookingReference);
            throw;
        }
    }

    public Task SendReceiptAsync(Booking booking, string email)
    {
        if (string.IsNullOrEmpty(email))
        {
            _logger.LogWarning("Cannot send receipt: Email is empty");
            return Task.CompletedTask;
        }

        // TODO: Generate and send actual receipt PDF
        _logger.LogInformation("Would send receipt to {Email} for booking {BookingRef}", 
            email, booking.BookingReference);
        return Task.CompletedTask;
    }

    public async Task SendPickupReminderAsync(Booking booking)
    {
        if (booking.Renter?.Phone == null && string.IsNullOrEmpty(booking.Renter?.Email))
            return;

        var placeholders = new Dictionary<string, string>
        {
            { "customer_name", $"{booking.Renter?.FirstName} {booking.Renter?.LastName}".Trim() },
            { "booking_reference", booking.BookingReference },
            { "pickup_date", booking.PickupDateTime.ToString("MMM dd, yyyy") },
            { "pickup_time", booking.PickupDateTime.ToString("HH:mm") },
            { "pickup_location", booking.PickupLocationJson ?? "TBD" },
            { "vehicle_make", booking.Vehicle?.Make ?? "" },
            { "vehicle_model", booking.Vehicle?.Model ?? "" },
            { "driver_name", booking.WithDriver ? $"{booking.Driver?.FirstName} {booking.Driver?.LastName}".Trim() : "N/A" },
            { "support_phone", await GetSupportPhoneAsync(false) },
            { "support_email", await GetSupportEmailAsync() }
        };

        try
        {
            var htmlMessage = await _emailTemplateService.RenderTemplateAsync("trip_pickup_reminder", placeholders);
            var subject = await _emailTemplateService.RenderSubjectAsync("trip_pickup_reminder", placeholders);

            if (!string.IsNullOrEmpty(booking.Renter?.Phone))
            {
                var whatsappMsg = $@"⏰ PICKUP REMINDER

Your rental starts tomorrow!

Ref: {booking.BookingReference}
Vehicle: {booking.Vehicle?.Make} {booking.Vehicle?.Model}

📅 Pickup: {booking.PickupDateTime:MMM dd, yyyy @ HH:mm}
📍 Location: {booking.PickupLocationJson ?? "TBD"}

{(booking.WithDriver ? $"Driver: {booking.Driver?.FirstName} {booking.Driver?.LastName}" : "")}

See you soon!";
                await _whatsAppSender.SendBookingNotificationAsync(booking.Renter.Phone, whatsappMsg);
            }

            if (!string.IsNullOrEmpty(booking.Renter?.Email))
            {
                await _emailService.SendEmailAsync(booking.Renter.Email, subject, htmlMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send pickup reminder using template for booking {BookingRef}", booking.BookingReference);
            throw;
        }
    }

    public async Task SendReturnReminderAsync(Booking booking)
    {
        if (booking.Renter?.Phone == null && string.IsNullOrEmpty(booking.Renter?.Email))
            return;

        var placeholders = new Dictionary<string, string>
        {
            { "customer_name", $"{booking.Renter?.FirstName} {booking.Renter?.LastName}".Trim() },
            { "booking_reference", booking.BookingReference },
            { "return_date", booking.ReturnDateTime.ToString("MMM dd, yyyy") },
            { "return_time", booking.ReturnDateTime.ToString("HH:mm") },
            { "return_location", booking.ReturnLocationJson ?? "TBD" },
            { "vehicle_make", booking.Vehicle?.Make ?? "" },
            { "vehicle_model", booking.Vehicle?.Model ?? "" },
            { "support_phone", await GetSupportPhoneAsync(false) },
            { "support_email", await GetSupportEmailAsync() }
        };

        try
        {
            var htmlMessage = await _emailTemplateService.RenderTemplateAsync("trip_return_reminder", placeholders);
            var subject = await _emailTemplateService.RenderSubjectAsync("trip_return_reminder", placeholders);

            if (!string.IsNullOrEmpty(booking.Renter?.Phone))
            {
                var whatsappMsg = $@"⏰ RETURN REMINDER

Your rental ends tomorrow!

Ref: {booking.BookingReference}
Vehicle: {booking.Vehicle?.Make} {booking.Vehicle?.Model}

📅 Return: {booking.ReturnDateTime:MMM dd, yyyy @ HH:mm}
📍 Location: {booking.ReturnLocationJson ?? "TBD"}

Please ensure vehicle is returned on time with same fuel level.

Thank you!";
                await _whatsAppSender.SendBookingNotificationAsync(booking.Renter.Phone, whatsappMsg);
            }

            if (!string.IsNullOrEmpty(booking.Renter?.Email))
            {
                await _emailService.SendEmailAsync(booking.Renter.Email, subject, htmlMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send return reminder using template for booking {BookingRef}", booking.BookingReference);
            throw;
        }
    }

    public async Task SendPayoutRequestNotificationAsync(User owner, decimal amount)
    {
        var settings = await GetNotificationSettingsAsync();
        if (!settings.PayoutRequest.Email)
        {
            _logger.LogInformation("Payout request email notifications are disabled");
            return;
        }

        var placeholders = new Dictionary<string, string>
        {
            { "owner_name", $"{owner.FirstName} {owner.LastName}".Trim() },
            { "owner_email", owner.Email ?? "" },
            { "payout_amount", amount.ToString("F2") },
            { "currency", "GHS" },
            { "request_date", DateTime.UtcNow.ToString("MMM dd, yyyy") },
            { "dashboard_url", "https://admin.ryverental.com/payouts" }
        };

        try
        {
            var htmlMessage = await _emailTemplateService.RenderTemplateAsync("payout_request", placeholders);
            var subject = await _emailTemplateService.RenderSubjectAsync("payout_request", placeholders);

            _logger.LogInformation("Payout request notification: Owner {OwnerId}, Amount {Amount}", owner.Id, amount);
            
            // TODO: Send to admin email list from configuration
            // await _emailService.SendEmailAsync("admin@ryverental.com", subject, htmlMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send payout request notification using template for owner {OwnerId}", owner.Id);
            throw;
        }
    }

    public async Task SendNewReviewNotificationAsync(User owner, string reviewText, int rating)
    {
        var settings = await GetNotificationSettingsAsync();
        if (!settings.NewReview.Email)
        {
            _logger.LogInformation("New review email notifications are disabled");
            return;
        }

        var stars = new string('⭐', rating);
        var placeholders = new Dictionary<string, string>
        {
            { "owner_name", $"{owner.FirstName} {owner.LastName}".Trim() },
            { "rating", rating.ToString() },
            { "rating_stars", stars },
            { "review_text", reviewText },
            { "review_date", DateTime.UtcNow.ToString("MMM dd, yyyy") },
            { "dashboard_url", "https://ryverental.com/dashboard/reviews" }
        };

        try
        {
            var htmlMessage = await _emailTemplateService.RenderTemplateAsync("review_received", placeholders);
            var subject = await _emailTemplateService.RenderSubjectAsync("review_received", placeholders);

            if (!string.IsNullOrEmpty(owner.Phone))
            {
                var whatsappMsg = $@"⭐ NEW REVIEW

You have received a new review!

Rating: {stars} ({rating}/5)

Review: {reviewText}

Check your dashboard for details.";
                await _whatsAppSender.SendBookingNotificationAsync(owner.Phone, whatsappMsg);
            }

            if (!string.IsNullOrEmpty(owner.Email))
            {
                await _emailService.SendEmailAsync(owner.Email, subject, htmlMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send review notification using template for owner {OwnerId}", owner.Id);
            throw;
        }
    }

    public async Task SendReportFiledNotificationAsync(string reportType, string description)
    {
        var settings = await GetNotificationSettingsAsync();
        if (!settings.ReportFiled.Email)
        {
            _logger.LogInformation("Report filed email notifications are disabled");
            return;
        }

        var placeholders = new Dictionary<string, string>
        {
            { "report_type", reportType },
            { "report_description", description },
            { "report_date", DateTime.UtcNow.ToString("MMM dd, yyyy HH:mm") },
            { "dashboard_url", "https://admin.ryverental.com/reports" }
        };

        try
        {
            var htmlMessage = await _emailTemplateService.RenderTemplateAsync("report_filed", placeholders);
            var subject = await _emailTemplateService.RenderSubjectAsync("report_filed", placeholders);

            _logger.LogInformation("Report filed notification: Type {ReportType}", reportType);
            
            // TODO: Send to admin email list from configuration
            // await _emailService.SendEmailAsync("admin@ryverental.com", subject, htmlMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send report filed notification using template");
            throw;
        }
    }

    /// <summary>
    /// Generates a QR code as a base64-encoded PNG data URI for embedding in emails
    /// </summary>
    /// <param name="bookingReference">The booking reference to encode</param>
    /// <param name="pickupUrl">Optional pickup URL to include in QR code</param>
    /// <returns>Base64 data URI string (data:image/png;base64,...)</returns>
    private string GenerateQRCodeBase64(string bookingReference, string pickupUrl)
    {
        try
        {
            // Create QR code data - deep link to owner dashboard
            var qrData = $"https://dashboard.ryverental.com/bookings/{bookingReference}";

            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(qrData, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);
            
            // Generate PNG as byte array
            var qrCodeBytes = qrCode.GetGraphic(20); // 20 pixels per module
            
            // Convert to base64 data URI
            var base64String = Convert.ToBase64String(qrCodeBytes);
            return $"data:image/png;base64,{base64String}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate QR code for booking {BookingRef}", bookingReference);
            
            // Return a placeholder image data URI (1x1 transparent PNG)
            return "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==";
        }
    }

    public async Task SendVehicleReturnedNotificationAsync(Booking booking)
    {
        var settings = await GetNotificationSettingsAsync();
        if (!settings.NewBooking.Email)
        {
            _logger.LogInformation("Email notifications are disabled");
            return;
        }

        // Reload booking with related entities
        var bookingWithEntities = await _db.Bookings
            .Include(b => b.Renter)
            .Include(b => b.Vehicle)
            .Include(b => b.ReturnInspection)
            .FirstOrDefaultAsync(b => b.Id == booking.Id);

        if (bookingWithEntities == null)
        {
            _logger.LogWarning("Cannot send vehicle returned notification: Booking not found");
            return;
        }

        booking = bookingWithEntities;

        try
        {
            // Notify Renter
            if (booking.Renter != null && !string.IsNullOrWhiteSpace(booking.Renter.Email))
            {
                var renterName = $"{booking.Renter.FirstName} {booking.Renter.LastName}".Trim();
                var placeholders = new Dictionary<string, string>
                {
                    { "customer_name", renterName },
                    { "booking_reference", booking.BookingReference },
                    { "vehicle_make", booking.Vehicle?.Make ?? "" },
                    { "vehicle_model", booking.Vehicle?.Model ?? "" },
                    { "return_date", booking.PostTripRecordedAt?.ToString("MMM dd, yyyy") ?? DateTime.UtcNow.ToString("MMM dd, yyyy") },
                    { "return_time", booking.PostTripRecordedAt?.ToString("HH:mm") ?? DateTime.UtcNow.ToString("HH:mm") },
                    { "support_email", "support@ryverental.com" }
                };

                var htmlMessage = await _emailTemplateService.RenderTemplateAsync("vehicle_returned_customer", placeholders);
                var subject = await _emailTemplateService.RenderSubjectAsync("vehicle_returned_customer", placeholders);
                await _emailService.SendEmailAsync(booking.Renter.Email, subject, htmlMessage);
            }

            // Notify Owner
            var owner = await _db.Users.FirstOrDefaultAsync(u => u.Id == booking.OwnerId);
            if (owner != null && !string.IsNullOrWhiteSpace(owner.Email))
            {
                var ownerName = $"{owner.FirstName} {owner.LastName}".Trim();
                var customerName = booking.Renter != null 
                    ? $"{booking.Renter.FirstName} {booking.Renter.LastName}".Trim() 
                    : booking.GuestFirstName + " " + booking.GuestLastName;

                var placeholders = new Dictionary<string, string>
                {
                    { "owner_name", ownerName },
                    { "booking_reference", booking.BookingReference },
                    { "customer_name", customerName },
                    { "vehicle_make", booking.Vehicle?.Make ?? "" },
                    { "vehicle_model", booking.Vehicle?.Model ?? "" },
                    { "vehicle_plate", booking.Vehicle?.PlateNumber ?? "" },
                    { "return_date", booking.PostTripRecordedAt?.ToString("MMM dd, yyyy") ?? DateTime.UtcNow.ToString("MMM dd, yyyy") },
                    { "return_time", booking.PostTripRecordedAt?.ToString("HH:mm") ?? DateTime.UtcNow.ToString("HH:mm") },
                    { "support_email", "support@ryverental.com" }
                };

                var htmlMessage = await _emailTemplateService.RenderTemplateAsync("vehicle_returned_owner", placeholders);
                var subject = await _emailTemplateService.RenderSubjectAsync("vehicle_returned_owner", placeholders);
                await _emailService.SendEmailAsync(owner.Email, subject, htmlMessage);
            }

            _logger.LogInformation("Sent vehicle returned notifications for booking {BookingRef}", booking.BookingReference);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending vehicle returned notifications for {BookingRef}", booking.BookingReference);
        }
    }

    public async Task SendBookingCompletedNotificationAsync(Booking booking)
    {
        var settings = await GetNotificationSettingsAsync();
        if (!settings.NewBooking.Email)
        {
            _logger.LogInformation("Email notifications are disabled");
            return;
        }

        // Reload booking with related entities
        var bookingWithEntities = await _db.Bookings
            .Include(b => b.Renter)
            .Include(b => b.Vehicle)
            .Include(b => b.ReturnInspection)
            .FirstOrDefaultAsync(b => b.Id == booking.Id);

        if (bookingWithEntities == null)
        {
            _logger.LogWarning("Cannot send booking completed notification: Booking not found");
            return;
        }

        booking = bookingWithEntities;

        // Calculate trip duration (use ceiling to round up partial days)
        var tripDuration = booking.PostTripRecordedAt.HasValue && booking.ActualPickupDateTime.HasValue
            ? (int)Math.Ceiling((booking.PostTripRecordedAt.Value - booking.ActualPickupDateTime.Value).TotalDays)
            : (int)Math.Ceiling((booking.ReturnDateTime - booking.PickupDateTime).TotalDays);

        var distanceTraveled = booking.PostTripOdometer.HasValue && booking.PreTripOdometer.HasValue
            ? booking.PostTripOdometer.Value - booking.PreTripOdometer.Value
            : 0;

        try
        {
            // Notify Renter
            if (booking.Renter != null && !string.IsNullOrWhiteSpace(booking.Renter.Email))
            {
                var renterName = $"{booking.Renter.FirstName} {booking.Renter.LastName}".Trim();
                var placeholders = new Dictionary<string, string>
                {
                    { "customer_name", renterName },
                    { "booking_reference", booking.BookingReference },
                    { "vehicle_make", booking.Vehicle?.Make ?? "" },
                    { "vehicle_model", booking.Vehicle?.Model ?? "" },
                    { "trip_duration", tripDuration.ToString() },
                    { "distance_traveled", distanceTraveled.ToString() },
                    { "currency", booking.Currency },
                    { "total_amount", booking.TotalAmount.ToString("F2") },
                    { "deposit_amount", booking.DepositAmount.ToString("F2") },
                    { "support_email", await GetSupportEmailAsync() }
                };

                var htmlMessage = await _emailTemplateService.RenderTemplateAsync("booking_completed_customer", placeholders);
                var subject = await _emailTemplateService.RenderSubjectAsync("booking_completed_customer", placeholders);
                
                // Generate and attach receipt PDF
                try
                {
                    var receiptBytes = await _receiptTemplateService.GenerateReceiptPdfAsync(booking);
                    
                    var attachments = new List<EmailAttachment>
                    {
                        new EmailAttachment($"receipt-{booking.BookingReference}.pdf", receiptBytes, "application/pdf")
                    };
                    
                    await _emailService.SendEmailWithAttachmentsAsync(booking.Renter.Email, subject, htmlMessage, attachments);
                }
                catch (Exception receiptEx)
                {
                    _logger.LogError(receiptEx, "Failed to generate receipt attachment for booking {BookingRef}, sending email without attachment", booking.BookingReference);
                    await _emailService.SendEmailAsync(booking.Renter.Email, subject, htmlMessage);
                }

                // Send WhatsApp notification
                if (!string.IsNullOrWhiteSpace(booking.Renter.Phone))
                {
                    var whatsappMsg = $@"✅ TRIP COMPLETED

Hi {renterName},

Your rental has been completed!

Ref: {booking.BookingReference}
Vehicle: {booking.Vehicle?.Make} {booking.Vehicle?.Model}
Duration: {tripDuration} days
Distance: {distanceTraveled} km

💰 Total Paid: {booking.Currency} {booking.TotalAmount:F2}
🔄 Deposit Refund: {booking.Currency} {booking.DepositAmount:F2} (Processing)

Thank you for choosing RyveRental!

Questions? Contact support@ryverental.com";
                    await _whatsAppSender.SendBookingNotificationAsync(booking.Renter.Phone, whatsappMsg);
                }
            }

            // Notify Owner
            var owner = await _db.Users.FirstOrDefaultAsync(u => u.Id == booking.OwnerId);
            if (owner != null && !string.IsNullOrWhiteSpace(owner.Email))
            {
                var ownerName = $"{owner.FirstName} {owner.LastName}".Trim();
                var customerName = booking.Renter != null 
                    ? $"{booking.Renter.FirstName} {booking.Renter.LastName}".Trim() 
                    : booking.GuestFirstName + " " + booking.GuestLastName;

                // Calculate mileage charges from booking charges
                var mileageCharges = await _db.BookingCharges
                    .Include(bc => bc.ChargeType)
                    .Where(bc => bc.BookingId == booking.Id && 
                                bc.ChargeType != null && 
                                bc.ChargeType.Code == "mileage_overage" &&
                                bc.Status == "approved")
                    .ToListAsync();
                
                var mileageEarnings = mileageCharges.Sum(mc => mc.Amount);
                
                // Calculate base rental (excluding mileage)
                var baseRental = booking.RentalAmount - mileageEarnings;
                
                // Driver earnings (if owner provides driver service)
                var driverEarnings = booking.DriverAmount ?? 0m;
                
                // Calculate owner's net payment (rental + mileage + driver - service fee)
                var totalEarnings = booking.RentalAmount + driverEarnings; // rental includes base + mileage
                var platformFee = booking.PlatformFee ?? 0m;
                var ownerNetPayment = totalEarnings - platformFee;

                var placeholders = new Dictionary<string, string>
                {
                    { "owner_name", ownerName },
                    { "booking_reference", booking.BookingReference },
                    { "customer_name", customerName },
                    { "vehicle_make", booking.Vehicle?.Make ?? "" },
                    { "vehicle_model", booking.Vehicle?.Model ?? "" },
                    { "vehicle_plate", booking.Vehicle?.PlateNumber ?? "" },
                    { "trip_duration", tripDuration.ToString() },
                    { "distance_traveled", distanceTraveled.ToString() },
                    { "currency", booking.Currency },
                    { "base_rental", baseRental.ToString("F2") },
                    { "mileage_earnings", mileageEarnings.ToString("F2") },
                    { "driver_earnings", driverEarnings.ToString("F2") },
                    { "rental_amount", booking.RentalAmount.ToString("F2") },
                    { "total_earnings", totalEarnings.ToString("F2") },
                    { "platform_fee", platformFee.ToString("F2") },
                    { "owner_net_payment", ownerNetPayment.ToString("F2") },
                    { "mileage_display", mileageEarnings > 0 ? "table-row" : "none" },
                    { "driver_display", driverEarnings > 0 ? "table-row" : "none" },
                    { "support_email", "support@ryverental.com" }
                };

                var htmlMessage = await _emailTemplateService.RenderTemplateAsync("booking_completed_owner", placeholders);
                var subject = await _emailTemplateService.RenderSubjectAsync("booking_completed_owner", placeholders);
                await _emailService.SendEmailAsync(owner.Email, subject, htmlMessage);

                // Send WhatsApp notification
                if (!string.IsNullOrWhiteSpace(owner.Phone))
                {
                    var whatsappMsg = $@"✅ RENTAL COMPLETED

Hi {ownerName},

Rental booking completed!

Ref: {booking.BookingReference}
Customer: {customerName}
Vehicle: {booking.Vehicle?.Make} {booking.Vehicle?.Model}
Duration: {tripDuration} days
Distance: {distanceTraveled} km

💰 Your Earnings: {booking.Currency} {ownerNetPayment:F2} (after service fee)

Payout will be processed according to your payment schedule.

Questions? Contact support@ryverental.com";
                    await _whatsAppSender.SendBookingNotificationAsync(owner.Phone, whatsappMsg);
                }
            }

            _logger.LogInformation("Sent booking completed notifications for booking {BookingRef}", booking.BookingReference);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending booking completed notifications for {BookingRef}", booking.BookingReference);
        }
    }
    
    private async Task<string> GetSupportPhoneAsync(bool forWhatsApp = false)
    {
        try
        {
            var configKey = forWhatsApp ? "support_phone_whatsapp" : "support_phone_email";
            var config = await _db.AppConfigs.FirstOrDefaultAsync(c => c.ConfigKey == configKey);
            
            if (config != null && !string.IsNullOrWhiteSpace(config.ConfigValue))
                return config.ConfigValue;
            
            // Fallback to default
            return forWhatsApp ? "+233535944564" : "+233 53 594 4564";
        }
        catch
        {
            return forWhatsApp ? "+233535944564" : "+233 53 594 4564";
        }
    }
    
    private async Task<string> GetSupportEmailAsync()
    {
        try
        {
            var config = await _db.AppConfigs.FirstOrDefaultAsync(c => c.ConfigKey == "support_email");
            return config?.ConfigValue ?? "support@ryverental.com";
        }
        catch
        {
            return "support@ryverental.com";
        }
    }

    // IntegrationPartner application notification methods
    public async Task SendIntegrationPartnerApplicationReceivedAsync(Models.IntegrationPartner partner)
    {
        if (partner == null || string.IsNullOrWhiteSpace(partner.Email))
        {
            _logger.LogWarning("Cannot send IntegrationPartner application received email: missing partner or email");
            return;
        }

        var placeholders = new Dictionary<string, string>
        {
            { "contact_person", partner.ContactPerson ?? partner.Name ?? "" },
            { "business_name", partner.Name ?? "" },
            { "application_reference", partner.ApplicationReference ?? "" },
            { "submitted_at", partner.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss") },
            { "review_time", "2-3 business days" },
            { "support_email", await GetSupportEmailAsync() }
        };

        try
        {
            var htmlMessage = await _emailTemplateService.RenderTemplateAsync("integration_partner_application_received", placeholders);
            var subject = await _emailTemplateService.RenderSubjectAsync("integration_partner_application_received", placeholders);

            await _emailService.SendEmailAsync(partner.Email, subject, htmlMessage);
            _logger.LogInformation("Sent IntegrationPartner application received email to {Email} (Ref: {Ref})", partner.Email, partner.ApplicationReference);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send IntegrationPartner application received email to {Email}", partner.Email);
        }
    }

    public async Task SendIntegrationPartnerApplicationApprovedAsync(Models.IntegrationPartner partner, string apiKey)
    {
        if (partner == null || string.IsNullOrWhiteSpace(partner.Email))
        {
            _logger.LogWarning("Cannot send IntegrationPartner approved email: missing partner or email");
            return;
        }

        var expires = partner.ApiKeyExpiresAt.HasValue ? partner.ApiKeyExpiresAt.Value.ToString("yyyy-MM-dd HH:mm:ss 'UTC'") : "Never";

        var placeholders = new Dictionary<string, string>
        {
            { "contact_person", partner.ContactPerson ?? partner.Name ?? "" },
            { "business_name", partner.Name ?? "" },
            { "application_reference", partner.ApplicationReference ?? "" },
            { "api_key", apiKey ?? "" },
            { "api_key_expires_at", expires },
            { "docs_url", "https://docs.ryverental.info" },
            { "support_email", await GetSupportEmailAsync() }
        };

        try
        {
            var htmlMessage = await _emailTemplateService.RenderTemplateAsync("integration_partner_application_approved", placeholders);
            var subject = await _emailTemplateService.RenderSubjectAsync("integration_partner_application_approved", placeholders);

            await _emailService.SendEmailAsync(partner.Email, subject, htmlMessage);
            _logger.LogInformation("Sent IntegrationPartner approval email to {Email} (Ref: {Ref})", partner.Email, partner.ApplicationReference);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send IntegrationPartner approval email to {Email}", partner.Email);
        }
    }

    public async Task SendIntegrationPartnerApplicationRejectedAsync(string email, string applicationReference, string reason)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            _logger.LogWarning("Cannot send IntegrationPartner rejection email: missing email");
            return;
        }

        var placeholders = new Dictionary<string, string>
        {
            { "application_reference", applicationReference ?? "" },
            { "rejection_reason", reason ?? "" },
            { "support_email", await GetSupportEmailAsync() }
        };

        try
        {
            var htmlMessage = await _emailTemplateService.RenderTemplateAsync("integration_partner_application_rejected", placeholders);
            var subject = await _emailTemplateService.RenderSubjectAsync("integration_partner_application_rejected", placeholders);

            await _emailService.SendEmailAsync(email, subject, htmlMessage);
            _logger.LogInformation("Sent IntegrationPartner rejection email to {Email} (Ref: {Ref})", email, applicationReference);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send IntegrationPartner rejection email to {Email}", email);
        }
    }
    
    private string FormatLocationFromJson(string? locationJson)
    {
        if (string.IsNullOrWhiteSpace(locationJson))
            return "To Be Determined";
        
        try
        {
            using var doc = JsonDocument.Parse(locationJson);
            var root = doc.RootElement;
            
            // Check location type
            if (root.TryGetProperty("type", out var typeElement))
            {
                var locationType = typeElement.GetString();
                
                if (locationType == "city" && root.TryGetProperty("cityName", out var cityName))
                {
                    return cityName.GetString() ?? "City Location";
                }
                else if (locationType == "airport" && root.TryGetProperty("airportName", out var airportName))
                {
                    return airportName.GetString() ?? "Airport";
                }
                else if (locationType == "custom" && root.TryGetProperty("address", out var address))
                {
                    return address.GetString() ?? "Custom Location";
                }
            }
            
            // Fallback: try common properties
            if (root.TryGetProperty("cityName", out var fallbackCity))
                return fallbackCity.GetString() ?? locationJson;
            if (root.TryGetProperty("address", out var fallbackAddress))
                return fallbackAddress.GetString() ?? locationJson;
            
            return locationJson; // Return as-is if structure is unexpected
        }
        catch
        {
            return locationJson; // Return as-is if parsing fails
        }
    }
}

