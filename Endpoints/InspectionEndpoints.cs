using System.Security.Claims;
using System.Text.Json;
using System.IO.Compression;
using GhanaHybridRentalApi.Data;
using GhanaHybridRentalApi.Models;
using GhanaHybridRentalApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GhanaHybridRentalApi.Endpoints;

public static class InspectionEndpoints
{
    public static void MapInspectionEndpoints(this IEndpointRouteBuilder app)
    {
        // Magic link access (no auth required - token-based)
        app.MapGet("/inspect/{token}", AccessInspectionAsync);

        app.MapPost("/inspect/{token}/complete", CompleteInspectionAsync);

        app.MapPost("/inspect/{token}/photos", UploadInspectionPhotoAsync);

        // Admin/authenticated inspection viewing
        app.MapGet("/api/v1/inspections/{inspectionId:guid}", GetInspectionAsync)
            .RequireAuthorization();

        // Inspection photos endpoint
        app.MapGet("/api/v1/inspections/{inspectionId:guid}/photos", GetInspectionPhotosAsync)
            .RequireAuthorization();

        // Export inspection photos as ZIP (owners, renters, admins)
        app.MapGet("/api/v1/inspections/{inspectionId:guid}/photos/zip", DownloadInspectionPhotosZipAsync)
            .RequireAuthorization();

        // Token-based access returning structured photos
        app.MapGet("/api/v1/inspections/token/{token}", AccessInspectionByTokenAsync);

        // Admin inspection management
        var adminGroup = app.MapGroup("/api/v1/admin/inspections")
            .WithTags("Inspections - Admin")
            .RequireAuthorization();

        adminGroup.MapGet("/", GetAllInspectionsAsync)
            .WithName("GetAllInspections")
            .WithDescription("Get all inspections with filtering");

        adminGroup.MapGet("/{inspectionId:guid}", GetInspectionDetailsAsync)
            .WithName("GetInspectionDetails")
            .WithDescription("Get detailed inspection information");

        // Admin: view structured photos grouped by area and include booking/renter contact
        adminGroup.MapGet("/{inspectionId:guid}/photos", GetInspectionPhotosAdminAsync)
            .WithName("GetInspectionPhotosAdmin")
            .WithDescription("Get structured inspection photos grouped by area for admins");

        adminGroup.MapDelete("/{inspectionId:guid}", DeleteInspectionAsync)
            .WithName("DeleteInspection")
            .WithDescription("Delete an inspection record");
    }

    private static async Task<IResult> AccessInspectionAsync(
        string token,
        AppDbContext db)
    {
        var inspection = await db.Inspections
            .Include(i => i.Booking)
            .FirstOrDefaultAsync(i => i.MagicLinkToken == token);

        if (inspection is null)
            return Results.NotFound(new { error = "Inspection not found" });

        if (inspection.ExpiresAt.HasValue && inspection.ExpiresAt.Value < DateTime.UtcNow)
            return Results.BadRequest(new { error = "Inspection link has expired" });

        var booking = inspection.Booking;

        return Results.Ok(new
        {
            inspection.Id,
            inspection.BookingId,
            inspection.Type,
            inspection.Notes,
            inspection.ExpiresAt,
            booking = new
            {
                booking?.Id,
                booking?.VehicleId,
                booking?.PickupDateTime,
                booking?.ReturnDateTime
            },
            photos = ParseInspectionPhotos(inspection.PhotosJson ?? string.Empty)
        });
    }

    private static async Task<IResult> CompleteInspectionAsync(
        string token,
        [FromBody] CompleteInspectionRequest request,
        HttpContext context,
        AppDbContext db)
    {
        var inspection = await db.Inspections
            .Include(i => i.Booking)
            .FirstOrDefaultAsync(i => i.MagicLinkToken == token);

        if (inspection is null)
            return Results.NotFound(new { error = "Inspection not found" });

        if (inspection.ExpiresAt.HasValue && inspection.ExpiresAt.Value < DateTime.UtcNow)
            return Results.BadRequest(new { error = "Inspection link has expired" });

        inspection.Notes = request.Notes;
        inspection.CompletedAt = DateTime.UtcNow;

        if (request.PhotosStructured is not null && request.PhotosStructured.Any())
        {
            // Convert structured inputs to DTOs with timestamps
            var list = request.PhotosStructured.Select(p => new InspectionPhotoDto(p.Area ?? string.Empty, p.ImageUrl, p.Timestamp ?? DateTime.UtcNow)).ToList();
            inspection.PhotosJson = JsonSerializer.Serialize(list);
        }
        else if (request.Photos is not null && request.Photos.Any())
        {
            // Legacy: list of URLs
            var list = request.Photos.Select(u => new InspectionPhotoDto(string.Empty, u, DateTime.UtcNow)).ToList();
            inspection.PhotosJson = JsonSerializer.Serialize(list);
        }

        // Update mileage if provided
        if (request.Mileage.HasValue)
        {
            inspection.Mileage = request.Mileage.Value;
        }

        // Update fuel level if provided
        if (!string.IsNullOrWhiteSpace(request.FuelLevel))
        {
            inspection.FuelLevel = request.FuelLevel;
        }

        // Update damage notes if provided
        if (request.DamageNotes is not null)
        {
            inspection.DamageNotesJson = JsonSerializer.Serialize(request.DamageNotes);
        }

        // Update booking status when pickup inspection completes (Hertz/Turo style check-in)
        if (inspection.Type == "pickup" && inspection.Booking != null)
        {
            if (inspection.Booking.Status == "confirmed")
            {
                inspection.Booking.Status = "ongoing";
            }
        }
        // Update booking status when return inspection completes
        else if (inspection.Type == "return" && inspection.Booking != null)
        {
            if (inspection.Booking.Status == "ongoing")
            {
                inspection.Booking.Status = "completed";
                
                // Send booking completed notifications
                try
                {
                    var notificationService = context.RequestServices.GetRequiredService<INotificationService>();
                    await notificationService.SendBookingCompletedNotificationAsync(inspection.Booking);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending booking completed notification: {ex.Message}");
                }
            }

            // Calculate mileage charges if enabled
            if (request.Mileage.HasValue)
            {
                var chargeResult = await CalculateMileageChargeAsync(
                    inspection.Booking,
                    inspection.Id,
                    request.Mileage.Value,
                    db);
                
                if (chargeResult != null)
                {
                    await db.SaveChangesAsync(); // Save charge first
                    return Results.Ok(new
                    {
                        inspection.Id,
                        inspection.CompletedAt,
                        bookingStatus = inspection.Booking?.Status,
                        mileageCharge = chargeResult,
                        message = "Inspection completed successfully with mileage charge applied"
                    });
                }
            }
        }

        await db.SaveChangesAsync();

        return Results.Ok(new
        {
            inspection.Id,
            inspection.CompletedAt,
            bookingStatus = inspection.Booking?.Status,
            message = "Inspection completed successfully",
            photos = ParseInspectionPhotos(inspection.PhotosJson ?? string.Empty)
        });
    }

    // Calculate and apply mileage charges
    private static async Task<object?> CalculateMileageChargeAsync(
        Booking booking,
        Guid returnInspectionId,
        int returnMileage,
        AppDbContext db)
    {
        // Get vehicle and mileage settings
        var vehicle = await db.Vehicles.FindAsync(booking.VehicleId);
        if (vehicle == null) return null;

        var settingRecord = await db.GlobalSettings.FirstOrDefaultAsync(s => s.Key == "MileageCharging");
        if (settingRecord == null) return null;

        var settings = JsonSerializer.Deserialize<MileageChargingSettings>(settingRecord.ValueJson);
        if (settings == null || !settings.MileageChargingEnabled || !vehicle.MileageChargingEnabled)
            return null;

        // Get pickup inspection mileage
        var pickupInspection = await db.Inspections
            .FirstOrDefaultAsync(i => i.BookingId == booking.Id && i.Type == "pickup" && i.Mileage.HasValue);

        if (pickupInspection?.Mileage == null)
        {
            // Missing pickup mileage - get or create charge type
            var missingChargeType = await GetOrCreateChargeTypeAsync(db, "mileage_missing",
                "Missing Mileage Data", "Penalty for missing odometer reading", settings.MissingMileagePenaltyAmount);

            var missingCharge = new BookingCharge
            {
                BookingId = booking.Id,
                ChargeTypeId = missingChargeType.Id,
                Amount = settings.MissingMileagePenaltyAmount,
                Currency = "GHS",
                Label = "Missing pickup mileage data",
                Notes = "Penalty applied due to missing pickup odometer reading",
                Status = "pending_review",
                CreatedAt = DateTime.UtcNow,
                EvidencePhotoUrlsJson = "[]"
            };
            db.BookingCharges.Add(missingCharge);

            // Try to deduct from deposit
            if (booking.DepositAmount >= settings.MissingMileagePenaltyAmount)
            {
                booking.DepositAmount -= settings.MissingMileagePenaltyAmount;
                missingCharge.Status = "approved";
                missingCharge.SettledAt = DateTime.UtcNow;
            }

            return new
            {
                type = "mileage_missing",
                amount = settings.MissingMileagePenaltyAmount,
                deductedFromDeposit = missingCharge.Status == "approved",
                remainingDeposit = booking.DepositAmount,
                message = "Missing pickup mileage data - penalty applied"
            };
        }

        var driven = returnMileage - pickupInspection.Mileage.Value;

        // Check for tampering (odometer rollback)
        if (driven < 0)
        {
            var tamperingChargeType = await GetOrCreateChargeTypeAsync(db, "mileage_tampering",
                "Odometer Tampering", "Penalty for odometer rollback or tampering", settings.TamperingPenaltyAmount);

            var tamperingCharge = new BookingCharge
            {
                BookingId = booking.Id,
                ChargeTypeId = tamperingChargeType.Id,
                Amount = settings.TamperingPenaltyAmount,
                Currency = "GHS",
                Label = "Odometer tampering detected",
                Notes = $"Odometer rollback detected - Return: {returnMileage} km < Pickup: {pickupInspection.Mileage.Value} km",
                Status = "pending_review",
                CreatedAt = DateTime.UtcNow,
                EvidencePhotoUrlsJson = "[]"
            };
            db.BookingCharges.Add(tamperingCharge);

            // Try to deduct from deposit
            if (booking.DepositAmount >= settings.TamperingPenaltyAmount)
            {
                booking.DepositAmount -= settings.TamperingPenaltyAmount;
                tamperingCharge.Status = "approved";
                tamperingCharge.SettledAt = DateTime.UtcNow;
            }

            return new
            {
                type = "mileage_tampering",
                pickupMileage = pickupInspection.Mileage.Value,
                returnMileage,
                amount = settings.TamperingPenaltyAmount,
                deductedFromDeposit = tamperingCharge.Status == "approved",
                remainingDeposit = booking.DepositAmount,
                message = "Odometer tampering detected - penalty applied"
            };
        }

        // Calculate overage if exceeded included kilometers
        if (driven > vehicle.IncludedKilometers)
        {
            var overage = driven - vehicle.IncludedKilometers;
            var overageAmount = overage * vehicle.PricePerExtraKm;

            var overageChargeType = await GetOrCreateChargeTypeAsync(db, "mileage_overage",
                "Mileage Overage", "Charge for kilometers driven beyond included allowance", 0);

            var overageCharge = new BookingCharge
            {
                BookingId = booking.Id,
                ChargeTypeId = overageChargeType.Id,
                Amount = overageAmount,
                Currency = "GHS",
                Label = $"Mileage overage: {overage} km",
                Notes = $"Driven: {driven} km, Included: {vehicle.IncludedKilometers} km, Overage: {overage} km @ {vehicle.PricePerExtraKm:F2}/km",
                Status = "pending_review",
                CreatedAt = DateTime.UtcNow,
                EvidencePhotoUrlsJson = "[]"
            };
            db.BookingCharges.Add(overageCharge);

            // Try to deduct from deposit
            if (booking.DepositAmount >= overageAmount)
            {
                booking.DepositAmount -= overageAmount;
                overageCharge.Status = "approved";
                overageCharge.SettledAt = DateTime.UtcNow;
            }

            return new
            {
                type = "mileage_overage",
                pickupMileage = pickupInspection.Mileage.Value,
                returnMileage,
                driven,
                includedKilometers = vehicle.IncludedKilometers,
                overageKilometers = overage,
                ratePerKm = vehicle.PricePerExtraKm,
                amount = overageAmount,
                deductedFromDeposit = overageCharge.Status == "approved",
                remainingDeposit = booking.DepositAmount,
                message = $"{overage} km overage charged"
            };
        }

        // No overage - within included kilometers
        return new
        {
            type = "no_overage",
            pickupMileage = pickupInspection.Mileage.Value,
            returnMileage,
            driven,
            includedKilometers = vehicle.IncludedKilometers,
            amount = 0m,
            message = "No mileage overage"
        };
    }

    // Helper to get or create mileage charge types
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

    private static async Task<IResult> UploadInspectionPhotoAsync(
        string token,
        [FromBody] UploadPhotoRequest request,
        AppDbContext db)
    {
        var inspection = await db.Inspections
            .FirstOrDefaultAsync(i => i.MagicLinkToken == token);

        if (inspection is null)
            return Results.NotFound(new { error = "Inspection not found" });

        if (inspection.ExpiresAt.HasValue && inspection.ExpiresAt.Value < DateTime.UtcNow)
            return Results.BadRequest(new { error = "Inspection link has expired" });

        var photos = ParseInspectionPhotos(inspection.PhotosJson ?? string.Empty);

        photos.Add(new InspectionPhotoDto(request.Area ?? string.Empty, request.PhotoUrl, DateTime.UtcNow));
        inspection.PhotosJson = JsonSerializer.Serialize(photos);

        await db.SaveChangesAsync();

        return Results.Ok(new
        {
            inspection.Id,
            photoCount = photos.Count,
            message = "Photo uploaded successfully"
        });
    }

    private static async Task<IResult> GetInspectionAsync(
        Guid inspectionId,
        ClaimsPrincipal principal,
        AppDbContext db)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var inspection = await db.Inspections
            .Include(i => i.Booking)
            .FirstOrDefaultAsync(i => i.Id == inspectionId);

        if (inspection is null)
            return Results.NotFound(new { error = "Inspection not found" });

        // Check access rights
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        var hasAccess = user?.Role == "admin" ||
                       inspection.Booking?.RenterId == userId ||
                       inspection.Booking?.OwnerId == userId;

        if (!hasAccess)
            return Results.Forbid();

        return Results.Ok(new
        {
            inspection.Id,
            inspection.BookingId,
            inspection.Type,
            inspection.Notes,
            inspection.Mileage,
            inspection.FuelLevel,
            inspection.CreatedAt,
            inspection.CompletedAt,
            photos = ParseInspectionPhotos(inspection.PhotosJson ?? string.Empty),
            damageNotes = string.IsNullOrWhiteSpace(inspection.DamageNotesJson)
                ? new List<string>()
                : JsonSerializer.Deserialize<List<string>>(inspection.DamageNotesJson)
        });
    }

    // New: Get photos for an inspection grouped by area
    private static async Task<IResult> GetInspectionPhotosAsync(
        Guid inspectionId,
        ClaimsPrincipal principal,
        AppDbContext db)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var inspection = await db.Inspections.Include(i => i.Booking).FirstOrDefaultAsync(i => i.Id == inspectionId);
        if (inspection is null) return Results.NotFound(new { error = "Inspection not found" });

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        var hasAccess = user?.Role == "admin" || inspection.Booking?.RenterId == userId || inspection.Booking?.OwnerId == userId;
        if (!hasAccess) return Results.Forbid();

        var photos = ParseInspectionPhotos(inspection.PhotosJson ?? string.Empty)
            .Where(p => !string.IsNullOrWhiteSpace(p.ImageUrl))
            .ToList();

        // Group by area (filter out empty/invalid URLs)
        var grouped = photos
            .GroupBy(p => string.IsNullOrWhiteSpace(p.Area ?? string.Empty) ? "unspecified" : p.Area!)
            .ToDictionary(g => g.Key, g => g.Select(p => new { ImageUrl = p.ImageUrl ?? string.Empty, Timestamp = p.Timestamp }).ToList());

        return Results.Ok(new { inspectionId = inspection.Id, grouped });
    }

    // Admin: return structured photos grouped by area and include booking/renter contact
    private static async Task<IResult> GetInspectionPhotosAdminAsync(
        Guid inspectionId,
        AppDbContext db,
        HttpContext context)
    {
        var userRole = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (!string.Equals(userRole, "admin", StringComparison.OrdinalIgnoreCase))
            return Results.Forbid();

        var inspection = await db.Inspections
            .Include(i => i.Booking)
                .ThenInclude(b => b!.Renter)
            .Include(i => i.Booking)
                .ThenInclude(b => b!.Vehicle)
            .FirstOrDefaultAsync(i => i.Id == inspectionId);

        if (inspection is null) return Results.NotFound(new { error = "Inspection not found" });

        var photos = ParseInspectionPhotos(inspection.PhotosJson ?? string.Empty)
            .Where(p => !string.IsNullOrWhiteSpace(p.ImageUrl))
            .ToList();

        var grouped = photos
            .GroupBy(p => string.IsNullOrWhiteSpace(p.Area ?? string.Empty) ? "unspecified" : p.Area!)
            .ToDictionary(g => g.Key, g => g.Select(p => new { ImageUrl = p.ImageUrl ?? string.Empty, Timestamp = p.Timestamp }).ToList());

        var b = inspection.Booking;
        var renter = b?.Renter;
        var vehicle = b?.Vehicle;

        object? bookingObj = null;
        if (b != null)
        {
            bookingObj = new
            {
                b!.Id,
                b!.BookingReference,
                renter = renter != null ? new
                {
                    Id = renter!.Id,
                    Name = (((renter!.FirstName ?? "") + " " + (renter!.LastName ?? "")).Trim()),
                    Email = renter!.Email,
                    Phone = renter!.Phone
                } : null,
                vehicle = vehicle != null ? new
                {
                    Id = vehicle!.Id,
                    Make = vehicle!.Make,
                    Model = vehicle!.Model,
                    PlateNumber = vehicle!.PlateNumber
                } : null
            };
        }

        return Results.Ok(new
        {
            inspection.Id,
            inspection.BookingId,
            booking = bookingObj,
            grouped
        });
    }

    // Download inspection photos as ZIP (owners/renters/admins)
    private static async Task<IResult> DownloadInspectionPhotosZipAsync(
        Guid inspectionId,
        ClaimsPrincipal principal,
        AppDbContext db)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var inspection = await db.Inspections.Include(i => i.Booking).FirstOrDefaultAsync(i => i.Id == inspectionId);
        if (inspection is null) return Results.NotFound(new { error = "Inspection not found" });

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        var hasAccess = user?.Role == "admin" || inspection.Booking?.RenterId == userId || inspection.Booking?.OwnerId == userId;
        if (!hasAccess) return Results.Forbid();

        var photos = ParseInspectionPhotos(inspection.PhotosJson ?? string.Empty);
        if (photos == null || !photos.Any())
            return Results.BadRequest(new { error = "No photos available for this inspection" });

        using var http = new System.Net.Http.HttpClient();
        using var ms = new System.IO.MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, true))
        {
            var idx = 1;
#pragma warning disable CS8602
            foreach (var p in photos.Where(p => !string.IsNullOrWhiteSpace(p.ImageUrl)))
            {
                try
                {
                    var imageUrl = p.ImageUrl ?? string.Empty;
                    var bytes = await http.GetByteArrayAsync(imageUrl);
                    var fileName = System.IO.Path.GetFileName(new Uri(imageUrl).LocalPath);
                    if (string.IsNullOrWhiteSpace(fileName)) fileName = $"photo_{idx}.jpg";
                    var area = p.Area ?? string.Empty;
                    var entryName = string.IsNullOrWhiteSpace(area) ? fileName : System.IO.Path.Combine(area, fileName);
                    var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
                    using var entryStream = entry.Open();
                    await entryStream.WriteAsync(bytes, 0, bytes.Length);
                }
                catch
                {
                    // Skip failed downloads but continue
                }
                idx++;
            }
#pragma warning restore CS8602
        }

        ms.Seek(0, System.IO.SeekOrigin.Begin);
        var zipName = inspection.BookingId != Guid.Empty ? $"inspection_{inspection.BookingId}_photos.zip" : $"inspection_{inspection.Id}_photos.zip";
        return Results.File(ms.ToArray(), "application/zip", zipName);
    }
    // New: Access inspection by token and include structured photos
    private static async Task<IResult> AccessInspectionByTokenAsync(string token, AppDbContext db)
    {
        var inspection = await db.Inspections
            .Include(i => i.Booking)
            .FirstOrDefaultAsync(i => i.MagicLinkToken == token);

        if (inspection is null)
            return Results.NotFound(new { error = "Inspection not found" });

        if (inspection.ExpiresAt.HasValue && inspection.ExpiresAt.Value < DateTime.UtcNow)
            return Results.BadRequest(new { error = "Inspection link has expired" });

        var booking = inspection.Booking;

        return Results.Ok(new
        {
            inspection.Id,
            inspection.BookingId,
            inspection.Type,
            inspection.Notes,
            inspection.ExpiresAt,
            booking = new
            {
                booking?.Id,
                booking?.VehicleId,
                booking?.PickupDateTime,
                booking?.ReturnDateTime
            },
            photos = ParseInspectionPhotos(inspection.PhotosJson ?? string.Empty)
        });
    }

    // Helper parse function that supports legacy string arrays and new object arrays
    private static List<InspectionPhotoDto> ParseInspectionPhotos(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new List<InspectionPhotoDto>();

        try
        {
            // Try to parse as array of objects
            var objs = JsonSerializer.Deserialize<List<InspectionPhotoDto>>(json);
            if (objs != null) return objs;
        }
        catch { /* ignore */ }

        try
        {
            var urls = JsonSerializer.Deserialize<List<string>>(json);
            if (urls != null) return urls.Select(u => new InspectionPhotoDto(string.Empty, u, DateTime.UtcNow)).ToList();
        }
        catch { /* ignore */ }

        return new List<InspectionPhotoDto>();
    }

    private record InspectionPhotoDto(string Area, string ImageUrl, DateTime Timestamp);

    private static async Task<IResult> GetAllInspectionsAsync(
        AppDbContext db,
        HttpContext context,
        string? type = null,
        Guid? bookingId = null,
        bool? completed = null,
        int page = 1,
        int pageSize = 50)
    {
        var userRole = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (!string.Equals(userRole, "admin", StringComparison.OrdinalIgnoreCase))
            return Results.Forbid();

        var query = db.Inspections
            .Include(i => i.Booking)
                .ThenInclude(b => b!.Renter)
            .Include(i => i.Booking)
                .ThenInclude(b => b!.Vehicle)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(type))
            query = query.Where(i => i.Type == type);

        if (bookingId.HasValue)
            query = query.Where(i => i.BookingId == bookingId.Value);

        if (completed.HasValue)
            query = query.Where(i => completed.Value ? i.CompletedAt != null : i.CompletedAt == null);

        var total = await query.CountAsync();
        var inspections = await query
            .OrderByDescending(i => i.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(i => new
            {
                i.Id,
                i.BookingId,
                i.Type,
                i.Notes,
                i.Mileage,
                i.FuelLevel,
                i.CreatedAt,
                i.CompletedAt,
                i.ExpiresAt,
                booking = i.Booking != null ? new
                {
                    Id = i.Booking!.Id,
                    BookingReference = i.Booking!.BookingReference,
                    RenterName = i.Booking!.Renter != null ? ((i.Booking.Renter!.FirstName ?? "") + " " + (i.Booking.Renter!.LastName ?? "")).Trim() : null,
                    VehicleName = i.Booking!.Vehicle != null ? i.Booking.Vehicle!.Make + " " + i.Booking.Vehicle!.Model : null
                } : null,
                i.PhotosJson
            })
            .ToListAsync();

        // Calculate photo count after query execution
        var result = inspections.Select(i => new
        {
            i.Id,
            i.BookingId,
            i.Type,
            i.Notes,
            i.Mileage,
            i.FuelLevel,
            i.CreatedAt,
            i.CompletedAt,
            i.ExpiresAt,
            i.booking,
            photoCount = ParseInspectionPhotos(i.PhotosJson ?? string.Empty).Count
        }).ToList();

        return Results.Ok(new { total, page, pageSize, data = result });
    }

    private static async Task<IResult> GetInspectionDetailsAsync(
        Guid inspectionId,
        AppDbContext db,
        HttpContext context)
    {
        var userRole = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (!string.Equals(userRole, "admin", StringComparison.OrdinalIgnoreCase))
            return Results.Forbid();

        var inspection = await db.Inspections
            .Include(i => i.Booking)
                .ThenInclude(b => b!.Renter)
            .Include(i => i.Booking)
                .ThenInclude(b => b!.Vehicle)
            .FirstOrDefaultAsync(i => i.Id == inspectionId);

        if (inspection == null)
            return Results.NotFound(new { error = "Inspection not found" });

        var b = inspection.Booking;
        var renter = b?.Renter;
        var vehicle = b?.Vehicle;

#pragma warning disable CS8602
        object? bookingObj = null;
        if (b != null)
        {
            bookingObj = new
            {
                b!.Id,
                b!.BookingReference,
                b!.PickupDateTime,
                b!.ReturnDateTime,
                b!.OwnerId,
                renter = renter != null ? new
                {
                    renter!.Id,
                    Name = (renter!.FirstName ?? "") + " " + (renter!.LastName ?? ""),
                    renter!.Phone
                } : null,
                vehicle = vehicle != null ? new
                {
                    vehicle!.Id,
                    vehicle!.Make,
                    vehicle!.Model,
                    vehicle!.Year,
                    vehicle!.PlateNumber
                } : null
            };
        }
#pragma warning restore CS8602

        return Results.Ok(new
        {
            inspection.Id,
            inspection.BookingId,
            inspection.Type,
            inspection.Notes,
            inspection.Mileage,
            inspection.FuelLevel,
            inspection.CreatedAt,
            inspection.CompletedAt,
            inspection.ExpiresAt,
            inspection.MagicLinkToken,
            booking = bookingObj,
            photos = ParseInspectionPhotos(inspection.PhotosJson ?? string.Empty),
            damageNotes = string.IsNullOrWhiteSpace(inspection.DamageNotesJson)
                ? new List<string>()
                : JsonSerializer.Deserialize<List<string>>(inspection.DamageNotesJson)
        });
    }

    private static async Task<IResult> DeleteInspectionAsync(
        Guid inspectionId,
        AppDbContext db,
        HttpContext context)
    {
        var userRole = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (!string.Equals(userRole, "admin", StringComparison.OrdinalIgnoreCase))
            return Results.Forbid();

        var inspection = await db.Inspections.FirstOrDefaultAsync(i => i.Id == inspectionId);
        if (inspection == null)
            return Results.NotFound(new { error = "Inspection not found" });

        db.Inspections.Remove(inspection);
        await db.SaveChangesAsync();

        return Results.Ok(new { message = "Inspection deleted successfully" });
    }
}

public record CompleteInspectionRequest(
    string? Notes,
    List<string>? Photos,
    List<InspectionPhotoInput>? PhotosStructured,
    int? Mileage,
    string? FuelLevel,
    List<string>? DamageNotes
);

public record UploadPhotoRequest(string PhotoUrl, string? Area);

public record InspectionPhotoInput(string Area, string ImageUrl, DateTime? Timestamp = null);
