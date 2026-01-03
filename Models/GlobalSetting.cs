using System.ComponentModel.DataAnnotations;

namespace GhanaHybridRentalApi.Models;

public class GlobalSetting
{
    [Key]
    [MaxLength(128)]
    public string Key { get; set; } = string.Empty;

    public string ValueJson { get; set; } = "{}";
}

// Mileage charging settings (stored as GlobalSetting with key "MileageCharging")
public class MileageChargingSettings
{
    public bool MileageChargingEnabled { get; set; } = false;
    public int MinimumIncludedKilometers { get; set; } = 100;
    public decimal MinPricePerExtraKm { get; set; } = 0.30m;
    public decimal MaxPricePerExtraKm { get; set; } = 1.00m;
    public decimal DefaultPricePerExtraKm { get; set; } = 0.50m;
    public decimal TamperingPenaltyAmount { get; set; } = 500.00m;
    public decimal MissingMileagePenaltyAmount { get; set; } = 200.00m;
}

// Notification settings (stored as GlobalSetting with key "NotificationSettings")
public class NotificationSettings
{
    public NotificationTypeSettings NewBooking { get; set; } = new();
    public NotificationTypeSettings BookingConfirmed { get; set; } = new();
    public NotificationTypeSettings PaymentReceived { get; set; } = new();
    public NotificationTypeSettings PayoutRequest { get; set; } = new();
    public NotificationTypeSettings NewReview { get; set; } = new();
    public NotificationTypeSettings ReportFiled { get; set; } = new();
}

public class NotificationTypeSettings
{
    public bool Email { get; set; } = true;
    public bool Push { get; set; } = false; // Reserved for future push notification support
}
