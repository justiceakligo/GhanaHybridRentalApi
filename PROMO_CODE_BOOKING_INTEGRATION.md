# Promo Code Integration Guide for BookingEndpoints.cs

## Changes Required in CreateBookingAsync Method

### 1. Add IPromoCodeService to method parameters (around line 457):

```csharp
private static async Task<IResult> CreateBookingAsync(
    ClaimsPrincipal principal,
    [FromBody] JsonElement body,
    AppDbContext db,
    IAppConfigService configService,
    IPromoCodeService promoCodeService, // ADD THIS
    HttpContext context)
```

### 2. Extract promo code from request (after line 619, where rPaymentMethod is extracted):

```csharp
string? rPromoCode;

if (authRequest != null)
{
    rPromoCode = authRequest.PromoCode;
}
else
{
    rPromoCode = guestRequest!.PromoCode;
}
```

### 3. Apply promo code discount (after line 734, after totalAmount calculation):

```csharp
// Apply promo code if provided
Guid? promoCodeId = null;
decimal promoDiscountAmount = 0m;

if (!string.IsNullOrWhiteSpace(rPromoCode))
{
    try
    {
        var categoryId = vehicle.CategoryId;
        var cityId = vehicle.CityId;
        
        var validationRequest = new ValidatePromoCodeDto(
            rPromoCode,
            totalAmount,
            categoryId,
            cityId,
            (int)totalDays
        );

        var validation = await promoCodeService.ValidatePromoCodeAsync(rPromoCode, renterId, validationRequest);

        if (validation.IsValid)
        {
            promoDiscountAmount = validation.DiscountAmount;
            totalAmount = validation.FinalAmount;
            promoCodeId = validation.PromoCode!.Id;
        }
        else
        {
            // Return error if promo code is invalid
            return Results.BadRequest(new { error = $"Promo code error: {validation.ErrorMessage}" });
        }
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = $"Failed to apply promo code: {ex.Message}" });
    }
}
```

### 4. Add promo code fields to booking creation (around line 749, in booking object creation):

```csharp
var booking = new Booking
{
    RenterId = renterId,
    VehicleId = vehicle.Id,
    OwnerId = vehicle.OwnerId,
    // ... existing fields ...
    TotalAmount = totalAmount,
    PromoCodeId = promoCodeId,  // ADD THIS
    PromoDiscountAmount = promoDiscountAmount,  // ADD THIS
    PaymentMethod = rPaymentMethod ?? "mobile_money",
    PaymentStatus = "pending",
    Status = "pending_payment",
    // ... rest of fields ...
};
```

### 5. Record promo code usage after booking is saved (after line 810, after db.SaveChangesAsync()):

```csharp
// Record promo code usage if applied
if (promoCodeId.HasValue && !string.IsNullOrWhiteSpace(rPromoCode))
{
    try
    {
        await promoCodeService.ApplyPromoCodeAsync(
            rPromoCode,
            renterId,
            booking.Id,
            totalAmount + promoDiscountAmount, // Original amount before discount
            "renter"
        );
    }
    catch (Exception ex)
    {
        // Log error but don't fail the booking
        Console.WriteLine($"Failed to record promo code usage: {ex.Message}");
    }
}
```

## Summary of Changes:
- Added IPromoCodeService dependency
- Extract promoCode from request
- Validate promo code and calculate discount
- Store promoCodeId and promoDiscountAmount in booking
- Record usage in PromoCodeUsage table
- Return discount details in response
