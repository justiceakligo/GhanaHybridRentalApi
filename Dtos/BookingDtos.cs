using GhanaHybridRentalApi.Models;

namespace GhanaHybridRentalApi.Dtos;

public record CreateBookingRequest(
    Guid VehicleId,
    DateTime PickupDateTime,
    DateTime ReturnDateTime,
    bool WithDriver,
    Guid? DriverId, // Optional: Specify preferred driver
    Guid? InsurancePlanId,
    Guid? ProtectionPlanId,
    object? PickupLocation,
    object? ReturnLocation,
    string PaymentMethod,
    string? PromoCode = null // Optional promo code
);

// Extended guest booking request to support booking without authentication
public record CreateBookingRequestGuest(
    Guid VehicleId,
    DateTime PickupDateTime,
    DateTime ReturnDateTime,
    bool WithDriver,
    Guid? DriverId, // Optional: Specify preferred driver
    Guid? InsurancePlanId,
    Guid? ProtectionPlanId,
    object? PickupLocation,
    object? ReturnLocation,
    string PaymentMethod,
    string? GuestPhone,
    string? GuestEmail,
    string? GuestFirstName,
    string? GuestLastName,
    string? GuestDriverLicenseNumber,
    DateTime? GuestDriverLicenseExpiryDate,
    string? GuestDriverLicensePhotoUrl,
    string? PromoCode = null // Optional promo code
);

public record BookingResponse(Booking Booking)
{
    public Guid Id => Booking.Id;
    public string BookingReference => Booking.BookingReference;
    public Guid RenterId => Booking.RenterId;
    public Guid VehicleId => Booking.VehicleId;
    public Guid OwnerId => Booking.OwnerId;
    public string Status => Booking.Status;
    public DateTime PickupDateTime => Booking.PickupDateTime;
    public DateTime ReturnDateTime => Booking.ReturnDateTime;
    public bool WithDriver => Booking.WithDriver;
    public Guid? DriverId => Booking.DriverId;
    public decimal? DriverAmount => Booking.DriverAmount;
    public decimal RentalAmount => Booking.RentalAmount;
    public decimal DepositAmount => Booking.DepositAmount;
    public decimal? PlatformFee => Booking.PlatformFee;
    public decimal TotalAmount => Booking.TotalAmount;
    public string PaymentStatus => Booking.PaymentStatus;
    public string PaymentMethod => Booking.PaymentMethod;
    public Guid? InsurancePlanId => Booking.InsurancePlanId;
    public decimal? InsuranceAmount => Booking.InsuranceAmount;
    public Guid? ProtectionPlanId => Booking.ProtectionPlanId;
    public decimal? ProtectionAmount => Booking.ProtectionAmount;
    public string? ProtectionSnapshotJson => Booking.ProtectionSnapshotJson;
    public Guid? PromoCodeId => Booking.PromoCodeId;
    public decimal? PromoDiscountAmount => Booking.PromoDiscountAmount;
    public DateTime CreatedAt => Booking.CreatedAt;
    public DateTime? UpdatedAt => Booking.UpdatedAt;
    public DateTime? PaymentDate => Booking.PaymentTransaction?.CompletedAt;
    
    // Vehicle information with parsed photos
    public object? Vehicle => Booking.Vehicle != null ? new
    {
        id = Booking.Vehicle.Id,
        make = Booking.Vehicle.Make,
        model = Booking.Vehicle.Model,
        year = Booking.Vehicle.Year,
        plateNumber = Booking.Vehicle.PlateNumber,
        transmission = Booking.Vehicle.Transmission,
        fuelType = Booking.Vehicle.FuelType,
        seatingCapacity = Booking.Vehicle.SeatingCapacity,
        hasAC = Booking.Vehicle.HasAC,
        // Parse photos from JSON and ensure HTTPS
        photos = ParseAndAbsolutizePhotos(Booking.Vehicle.PhotosJson),
        images = ParseAndAbsolutizePhotos(Booking.Vehicle.PhotosJson), // Alias for compatibility
        category = Booking.Vehicle.Category != null ? new
        {
            id = Booking.Vehicle.Category.Id,
            name = Booking.Vehicle.Category.Name
        } : null
    } : null;
    
    // Protection plan details - use snapshot if available, otherwise from relation
    public object? ProtectionPlan
    {
        get
        {
            // Try to parse snapshot first
            if (!string.IsNullOrWhiteSpace(Booking.ProtectionSnapshotJson))
            {
                try
                {
                    var snapshot = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(Booking.ProtectionSnapshotJson);
                    if (snapshot != null)
                    {
                        return new
                        {
                            id = Booking.ProtectionPlanId,
                            code = snapshot.ContainsKey("Code") ? snapshot["Code"]?.ToString() : null,
                            name = snapshot.ContainsKey("Name") ? snapshot["Name"]?.ToString() : null,
                            description = snapshot.ContainsKey("Description") ? snapshot["Description"]?.ToString() : null,
                            dailyPrice = snapshot.ContainsKey("DailyPrice") ? ParseDecimal(snapshot["DailyPrice"]) : null,
                            fixedPrice = snapshot.ContainsKey("FixedPrice") ? ParseDecimal(snapshot["FixedPrice"]) : null,
                            deductible = snapshot.ContainsKey("Deductible") ? ParseDecimal(snapshot["Deductible"]) : null,
                            includesMinorDamageWaiver = snapshot.ContainsKey("IncludesMinorDamageWaiver") ? ParseBool(snapshot["IncludesMinorDamageWaiver"]) : null,
                            minorWaiverCap = snapshot.ContainsKey("MinorWaiverCap") ? ParseDecimal(snapshot["MinorWaiverCap"]) : null
                        };
                    }
                }
                catch { }
            }
            
            // Fallback to relation
            if (Booking.ProtectionPlan != null)
            {
                return new
                {
                    id = Booking.ProtectionPlan.Id,
                    code = Booking.ProtectionPlan.Code,
                    name = Booking.ProtectionPlan.Name,
                    description = Booking.ProtectionPlan.Description,
                    dailyPrice = Booking.ProtectionPlan.DailyPrice,
                    deductible = Booking.ProtectionPlan.Deductible,
                    includesMinorDamageWaiver = Booking.ProtectionPlan.IncludesMinorDamageWaiver,
                    minorWaiverCap = Booking.ProtectionPlan.MinorWaiverCap
                };
            }
            
            return null;
        }
    }
    
    private static decimal? ParseDecimal(object? value)
    {
        if (value == null) return null;
        if (value is System.Text.Json.JsonElement je && je.ValueKind == System.Text.Json.JsonValueKind.Number)
            return je.GetDecimal();
        if (decimal.TryParse(value.ToString(), out var result))
            return result;
        return null;
    }
    
    private static bool? ParseBool(object? value)
    {
        if (value == null) return null;
        if (value is System.Text.Json.JsonElement je && je.ValueKind == System.Text.Json.JsonValueKind.True)
            return true;
        if (value is System.Text.Json.JsonElement je2 && je2.ValueKind == System.Text.Json.JsonValueKind.False)
            return false;
        if (bool.TryParse(value.ToString(), out var result))
            return result;
        return null;
    }
    
    private static string[]? ParseAndAbsolutizePhotos(string? photosJson)
    {
        if (string.IsNullOrWhiteSpace(photosJson))
            return null;
            
        try
        {
            var photos = System.Text.Json.JsonSerializer.Deserialize<string[]>(photosJson);
            if (photos == null || photos.Length == 0)
                return null;
                
            // Ensure all URLs use HTTPS
            return photos.Select(url => EnsureHttps(url)).ToArray();
        }
        catch
        {
            return null;
        }
    }
    
    private static string EnsureHttps(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return url;
            
        if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            return "https://" + url.Substring(7);
            
        if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            return "https://" + url;
            
        return url;
    }
    public object? MileageAllowance => Booking.Vehicle != null ? new
    {
        includedKilometers = Booking.Vehicle.IncludedKilometers,
        pricePerExtraKm = Booking.Vehicle.PricePerExtraKm,
        enabled = Booking.Vehicle.MileageChargingEnabled,
        note = $"Includes {Booking.Vehicle.IncludedKilometers}km. Extra km charged at {Booking.Vehicle.PricePerExtraKm:F2}/km"
    } : null;
    
    // Owner details (vehicle owner)
    public object? Owner => Booking.Vehicle?.Owner != null ? new
    {
        id = Booking.Vehicle.Owner.Id,
        firstName = Booking.Vehicle.Owner.FirstName,
        lastName = Booking.Vehicle.Owner.LastName,
        email = Booking.Vehicle.Owner.Email,
        phone = Booking.Vehicle.Owner.Phone,
        // Owner profile details if available
        companyName = Booking.Vehicle.Owner.OwnerProfile?.CompanyName
    } : null;

    // Renter info (owner-facing): returns authenticated renter if available or guest contact info
    public object? Renter => Booking.Renter != null ? new
    {
        id = Booking.Renter.Id,
        firstName = Booking.Renter.FirstName,
        lastName = Booking.Renter.LastName,
        phone = Booking.Renter.Phone,
        email = Booking.Renter.Email,
        // Renter profile documents and license info (if available)
        driverLicenseNumber = Booking.Renter?.RenterProfile?.DriverLicenseNumber,
        driverLicenseExpiryDate = Booking.Renter?.RenterProfile?.DriverLicenseExpiryDate,
        driverLicensePhotoUrl = Booking.Renter?.RenterProfile?.DriverLicensePhotoUrl,
        nationalIdNumber = Booking.Renter?.RenterProfile?.NationalIdNumber,
        nationalIdPhotoUrl = Booking.Renter?.RenterProfile?.NationalIdPhotoUrl,
        documents = string.IsNullOrWhiteSpace(Booking.Renter?.RenterProfile?.DocumentsJson) ? null : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string,string>>(Booking.Renter!.RenterProfile!.DocumentsJson!)
    } : (Booking.GuestEmail != null || Booking.GuestPhone != null || Booking.GuestFirstName != null ? new
    {
        guestFirstName = Booking.GuestFirstName,
        guestLastName = Booking.GuestLastName,
        guestEmail = Booking.GuestEmail,
        guestPhone = Booking.GuestPhone,
        driverLicenseNumber = Booking.Renter?.RenterProfile?.DriverLicenseNumber,
        driverLicenseExpiryDate = Booking.Renter?.RenterProfile?.DriverLicenseExpiryDate,
        driverLicensePhotoUrl = Booking.Renter?.RenterProfile?.DriverLicensePhotoUrl
    } : null);

    // Driver info (when booking includes a driver)
    public object? Driver => Booking.Driver?.DriverProfile != null ? new
    {
        id = Booking.Driver.Id,
        firstName = Booking.Driver.FirstName,
        lastName = Booking.Driver.LastName,
        phoneNumber = Booking.Driver.Phone,
        email = Booking.Driver.Email,
        licenseNumber = Booking.Driver.DriverProfile.LicenseNumber,
        licenseExpiryDate = Booking.Driver.DriverProfile.LicenseExpiryDate,
        profilePhotoUrl = Booking.Driver.DriverProfile.PhotoUrl,
        averageRating = Booking.Driver.DriverProfile.AverageRating,
        totalRides = Booking.Driver.DriverProfile.TotalTrips,
        available = Booking.Driver.DriverProfile.Available,
        verificationStatus = Booking.Driver.DriverProfile.VerificationStatus,
        driverType = Booking.Driver.DriverProfile.DriverType
    } : null;
}

public record UpdateBookingStatusRequest(string Status);

