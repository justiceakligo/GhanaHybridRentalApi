using System.Text.Json;
using GhanaHybridRentalApi.Data;
using GhanaHybridRentalApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GhanaHybridRentalApi.Endpoints;

public static class AdminMileageEndpoints
{
    // Admin override: manually add mileage charge to a booking
    public static async Task<IResult> AddMileageChargeOverrideAsync(
        Guid bookingId,
        [FromBody] AddMileageChargeRequest request,
        AppDbContext db)
    {
        var booking = await db.Bookings
            .Include(b => b.Vehicle)
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        if (booking == null)
            return Results.NotFound(new { error = "Booking not found" });

        if (booking.Status != "completed")
            return Results.BadRequest(new { error = "Can only add mileage charges to completed bookings" });

        // Get mileage settings
        var settingRecord = await db.GlobalSettings.FirstOrDefaultAsync(s => s.Key == "MileageCharging");
        if (settingRecord == null)
            return Results.BadRequest(new { error = "Mileage charging settings not configured" });

        var settings = JsonSerializer.Deserialize<MileageChargingSettings>(settingRecord.ValueJson);
        if (settings == null)
            return Results.BadRequest(new { error = "Invalid mileage charging settings" });

        var vehicle = booking.Vehicle;
        if (vehicle == null)
            return Results.BadRequest(new { error = "Vehicle not found" });

        // Calculate charge based on request
        decimal chargeAmount = 0m;
        string chargeType = "mileage_overage";
        string description = "";

        if (request.OverrideType == "overage")
        {
            var overage = request.ActualDrivenKm - (request.OverrideIncludedKm ?? vehicle.IncludedKilometers);
            if (overage <= 0)
                return Results.BadRequest(new { error = "No overage to charge" });

            var ratePerKm = request.OverrideRatePerKm ?? vehicle.PricePerExtraKm;
            chargeAmount = overage * ratePerKm;
            description = $"Manual mileage charge: {request.ActualDrivenKm} km driven, {request.OverrideIncludedKm ?? vehicle.IncludedKilometers} km included, {overage} km overage @ {ratePerKm:F2}/km. Reason: {request.Reason}";
        }
        else if (request.OverrideType == "tampering")
        {
            chargeType = "mileage_tampering";
            chargeAmount = request.FixedAmount ?? settings.TamperingPenaltyAmount;
            description = $"Manual tampering penalty. Reason: {request.Reason}";
        }
        else if (request.OverrideType == "missing")
        {
            chargeType = "mileage_missing";
            chargeAmount = request.FixedAmount ?? settings.MissingMileagePenaltyAmount;
            description = $"Manual missing mileage penalty. Reason: {request.Reason}";
        }
        else
        {
            return Results.BadRequest(new { error = "Invalid override type. Must be 'overage', 'tampering', or 'missing'" });
        }

        // Get or create charge type
        var chargeTypeName = chargeType == "mileage_overage" ? "Mileage Overage" :
                            chargeType == "mileage_tampering" ? "Odometer Tampering" :
                            "Missing Mileage Data";
        var postRentalChargeType = await GetOrCreateChargeTypeAsync(db, chargeType, chargeTypeName, description, chargeAmount);

        // Create charge
        var charge = new BookingCharge
        {
            BookingId = bookingId,
            ChargeTypeId = postRentalChargeType.Id,
            Amount = chargeAmount,
            Currency = "GHS",
            Label = $"Admin override: {chargeTypeName}",
            Notes = description,
            Status = "approved", // Admin-added charges are pre-approved
            CreatedAt = DateTime.UtcNow,
            SettledAt = DateTime.UtcNow,
            EvidencePhotoUrlsJson = "[]"
        };
        db.BookingCharges.Add(charge);

        // Try to deduct from deposit if requested
        if (request.DeductFromDeposit && booking.DepositAmount >= chargeAmount)
        {
            booking.DepositAmount -= chargeAmount;
        }

        await db.SaveChangesAsync();

        return Results.Ok(new
        {
            message = "Mileage charge added successfully",
            charge = new
            {
                charge.Id,
                charge.Amount,
                type = chargeType,
                deductedFromDeposit = request.DeductFromDeposit && booking.DepositAmount >= chargeAmount,
                remainingDeposit = booking.DepositAmount
            }
        });
    }

    // Helper to get or create charge type
    private static async Task<PostRentalChargeType> GetOrCreateChargeTypeAsync(
        AppDbContext db, string code, string name, string description, decimal defaultAmount)
    {
        var chargeType = await db.PostRentalChargeTypes.FirstOrDefaultAsync(ct => ct.Code == code);
        if (chargeType == null)
        {
            chargeType = new PostRentalChargeType
            {
                Code = code,
                Name = name,
                Description = description,
                DefaultAmount = defaultAmount,
                Currency = "GHS",
                RecipientType = "platform",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            db.PostRentalChargeTypes.Add(chargeType);
            await db.SaveChangesAsync();
        }
        return chargeType;
    }
}

public record AddMileageChargeRequest(
    string OverrideType, // "overage", "tampering", or "missing"
    int ActualDrivenKm,
    int? OverrideIncludedKm,
    decimal? OverrideRatePerKm,
    decimal? FixedAmount,
    string Reason,
    bool DeductFromDeposit
);
