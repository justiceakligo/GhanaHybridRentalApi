using System.Text.Json;
using GhanaHybridRentalApi.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GhanaHybridRentalApi.Endpoints;

public static class SettingsEndpoints
{
    public static void MapSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        // Public settings - no authentication required
        app.MapGet("/api/v1/settings/public", GetPublicSettingsAsync);
        
        // Price calculator - no authentication required
        app.MapPost("/api/v1/bookings/calculate-price", CalculateBookingPriceAsync);
    }

    private static async Task<IResult> GetPublicSettingsAsync(AppDbContext db)
    {
        // Get platform fee percentage
        decimal platformFeePercentage = 15.0m; // Default
        var platformFeeSetting = await db.GlobalSettings
            .FirstOrDefaultAsync(s => s.Key == "PlatformFeePercentage");
        
        if (platformFeeSetting != null)
        {
            try
            {
                platformFeePercentage = JsonSerializer.Deserialize<decimal>(platformFeeSetting.ValueJson);
            }
            catch
            {
                // Use default if parsing fails
            }
        }

        // Get deposit percentage (if you have it)
        decimal defaultDepositPercentage = 20.0m; // Default 20% of rental
        var depositSetting = await db.GlobalSettings
            .FirstOrDefaultAsync(s => s.Key == "DefaultDepositPercentage");
        
        if (depositSetting != null)
        {
            try
            {
                defaultDepositPercentage = JsonSerializer.Deserialize<decimal>(depositSetting.ValueJson);
            }
            catch
            {
                // Use default
            }
        }

        return Results.Ok(new
        {
            success = true,
            platformFeePercentage,
            defaultDepositPercentage,
            currency = "GHS",
            taxRate = 0.0m, // VAT/tax if applicable
            minimumBookingDays = 1,
            cancellationPolicyUrl = "/api/v1/refund-policies",
            supportEmail = "support@ryverental.com",
            supportPhone = "+233-XXX-XXXXX"
        });
    }

    private static async Task<IResult> CalculateBookingPriceAsync(
        [FromBody] PriceCalculationRequest request,
        AppDbContext db)
    {
        // Validate request
        if (request.VehicleId == Guid.Empty)
            return Results.BadRequest(new { error = "Vehicle ID is required" });

        if (request.PickupDate >= request.ReturnDate)
            return Results.BadRequest(new { error = "Return date must be after pickup date" });

        // Get vehicle
        var vehicle = await db.Vehicles
            .Include(v => v.Category)
            .Where(v => v.DeletedAt == null) // Exclude soft-deleted vehicles
            .FirstOrDefaultAsync(v => v.Id == request.VehicleId);

        if (vehicle == null)
            return Results.NotFound(new { error = "Vehicle not found" });

        if (vehicle.Status != "active")
            return Results.BadRequest(new { error = "Vehicle is not available for booking" });

        // Calculate rental days
        var rentalDays = Math.Max(1, (int)Math.Ceiling((request.ReturnDate - request.PickupDate).TotalDays));

        // Get daily rate
        var dailyRate = vehicle.DailyRate ?? vehicle.Category?.DefaultDailyRate ?? 0m;
        if (dailyRate <= 0)
            return Results.BadRequest(new { error = "Vehicle does not have a valid daily rate" });

        // Calculate rental amount
        var rentalAmount = dailyRate * rentalDays;

        // Get platform fee percentage
        decimal platformFeePercentage = 15.0m; // Default
        var platformFeeSetting = await db.GlobalSettings
            .FirstOrDefaultAsync(s => s.Key == "PlatformFeePercentage");
        
        if (platformFeeSetting != null)
        {
            try
            {
                platformFeePercentage = JsonSerializer.Deserialize<decimal>(platformFeeSetting.ValueJson);
            }
            catch
            {
                // Use default
            }
        }

        // Calculate platform fee
        var platformFee = rentalAmount * (platformFeePercentage / 100m);

        // Calculate deposit (use category default or 20% of rental)
        var depositAmount = vehicle.Category?.DefaultDepositAmount ?? (rentalAmount * 0.20m);

        // Driver fee (if requested)
        decimal driverAmount = 0m;
        if (request.WithDriver)
        {
            // Assume driver costs 30 GHS per day (you can make this configurable)
            var driverDailyRate = 30m;
            var driverRateSetting = await db.GlobalSettings
                .FirstOrDefaultAsync(s => s.Key == "DriverDailyRate");
            
            if (driverRateSetting != null)
            {
                try
                {
                    driverDailyRate = JsonSerializer.Deserialize<decimal>(driverRateSetting.ValueJson);
                }
                catch
                {
                    // Use default
                }
            }

            driverAmount = driverDailyRate * rentalDays;
        }

        // Calculate totals
        var subtotal = rentalAmount + driverAmount;
        var totalBeforeDeposit = subtotal + platformFee;
        var grandTotal = totalBeforeDeposit + depositAmount;

        return Results.Ok(new
        {
            success = true,
            vehicleId = vehicle.Id,
            vehicleName = $"{vehicle.Year} {vehicle.Make} {vehicle.Model}",
            rentalPeriod = new
            {
                pickupDate = request.PickupDate,
                returnDate = request.ReturnDate,
                days = rentalDays
            },
            pricing = new
            {
                dailyRate,
                rentalDays,
                rentalAmount,
                
                driverRequested = request.WithDriver,
                driverAmount,
                
                subtotal,
                
                platformFee,
                platformFeePercentage,
                
                totalBeforeDeposit,
                
                depositAmount,
                depositDescription = "Refundable security deposit",
                
                grandTotal,
                currency = "GHS"
            },
            breakdown = new[]
            {
                new { label = "Vehicle Rental", amount = rentalAmount, description = $"{rentalDays} day(s) @ GHS {dailyRate}/day" },
                request.WithDriver ? new { label = "Driver Service", amount = driverAmount, description = $"{rentalDays} day(s) @ GHS {driverAmount/rentalDays}/day" } : null,
                new { label = $"Service Fee ({platformFeePercentage}%)", amount = platformFee, description = "Platform service charge" },
                new { label = "Security Deposit", amount = depositAmount, description = "Refundable after rental" }
            }.Where(x => x != null).ToArray(),
            mileageInfo = vehicle.MileageChargingEnabled ? new
            {
                includedKilometers = vehicle.IncludedKilometers,
                pricePerExtraKm = vehicle.PricePerExtraKm,
                description = $"{vehicle.IncludedKilometers} km included. Extra km charged at GHS {vehicle.PricePerExtraKm}/km"
            } : null,
            notes = new[]
            {
                "Prices shown are estimates and may vary",
                "Security deposit is fully refundable (minus any damages/overage charges)",
                vehicle.MileageChargingEnabled ? $"Mileage overage charges apply beyond {vehicle.IncludedKilometers} km" : null,
                "Payment required at booking confirmation"
            }.Where(x => x != null).ToArray()
        });
    }
}

public record PriceCalculationRequest(
    Guid VehicleId,
    DateTime PickupDate,
    DateTime ReturnDate,
    bool WithDriver = false
);
