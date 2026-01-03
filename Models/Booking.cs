using System.ComponentModel.DataAnnotations;

namespace GhanaHybridRentalApi.Models;

public class Booking
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid RenterId { get; set; }
    public Guid VehicleId { get; set; }
    public Guid OwnerId { get; set; }
    // Guest contact info when booking created without an authenticated user
    [MaxLength(32)]
    public string? GuestPhone { get; set; }
    [MaxLength(256)]
    public string? GuestEmail { get; set; }
    [MaxLength(128)]
    public string? GuestFirstName { get; set; }
    [MaxLength(128)]
    public string? GuestLastName { get; set; }
    public Guid? GuestUserId { get; set; } // If we link booking to an existing user

    [MaxLength(32)]
    public string Status { get; set; } = "pending_payment"; // pending_payment, confirmed, ongoing, completed, cancelled, no_show

    public DateTime PickupDateTime { get; set; }
    public DateTime ReturnDateTime { get; set; }

    public string? PickupLocationJson { get; set; }
    public string? ReturnLocationJson { get; set; }

    public bool WithDriver { get; set; }
    public Guid? DriverId { get; set; }
    public decimal? DriverAmount { get; set; } // Cost of driver service if WithDriver = true

    [MaxLength(32)]
    public string BookingReference { get; set; } = string.Empty; // e.g., "RV-2025-001234"

    [MaxLength(8)]
    public string Currency { get; set; } = "GHS";

    public decimal RentalAmount { get; set; }
    public decimal DepositAmount { get; set; }
    public decimal? PlatformFee { get; set; }
    public string? FeesJson { get; set; }
    public decimal TotalAmount { get; set; }

    [MaxLength(32)]
    public string PaymentStatus { get; set; } = "unpaid"; // unpaid, paid, refunded, partial_refund

    [MaxLength(16)]
    public string PaymentMethod { get; set; } = "momo"; // momo, card

    public Guid? InsurancePlanId { get; set; }
    public decimal? InsuranceAmount { get; set; }
    public bool InsuranceAccepted { get; set; }
    public Guid? ProtectionPlanId { get; set; }
    public decimal? ProtectionAmount { get; set; }
    public string? ProtectionSnapshotJson { get; set; }

    // Promo code fields
    public Guid? PromoCodeId { get; set; }
    public decimal? PromoDiscountAmount { get; set; }

    public Guid? PickupInspectionId { get; set; }
    public Guid? ReturnInspectionId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Trip start/completion data (Owner quick check-in/out)
    public int? PreTripOdometer { get; set; }
    public double? PreTripFuelLevel { get; set; }
    public string? PreTripNotes { get; set; }
    public string? PreTripPhotosJson { get; set; }
    public DateTime? PreTripRecordedAt { get; set; }
    public Guid? PreTripRecordedBy { get; set; }
    
    public int? PostTripOdometer { get; set; }
    public double? PostTripFuelLevel { get; set; }
    public string? PostTripNotes { get; set; }
    public string? PostTripPhotosJson { get; set; }
    public DateTime? PostTripRecordedAt { get; set; }
    public Guid? PostTripRecordedBy { get; set; }
    
    public DateTime? ActualPickupDateTime { get; set; }

    // Navigation properties
    public User? Renter { get; set; }
    public User? Driver { get; set; }
    public Vehicle? Vehicle { get; set; }
    public Inspection? PickupInspection { get; set; }
    public Inspection? ReturnInspection { get; set; }
    public InsurancePlan? InsurancePlan { get; set; }
    public ProtectionPlan? ProtectionPlan { get; set; }
    public PaymentTransaction? PaymentTransaction { get; set; }
    public PromoCode? PromoCode { get; set; }
}
