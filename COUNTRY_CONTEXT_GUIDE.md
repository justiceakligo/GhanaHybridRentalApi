# Quick Guide: Using Country Context in Endpoints

## Automatic Country Filtering

The `ICountryContext` service automatically provides country information based on the route:

```csharp
// In your endpoint or service
public static void MapMyEndpoints(this IEndpointRouteBuilder app)
{
    app.MapGet("/api/v1/my-vehicles", async (
        AppDbContext db,
        ICountryContext countryContext) =>
    {
        // Get country-specific data
        var countryCode = countryContext.CountryCode;
        var currency = countryContext.CurrencyCode;
        
        // Filter vehicles by country through cities
        var vehicles = await db.Vehicles
            .Include(v => v.City)
            .ThenInclude(c => c.Country)
            .Where(v => v.City != null && 
                       v.City.Country != null && 
                       v.City.Country.Code == countryCode)
            .ToListAsync();
        
        return Results.Ok(new { 
            country = countryCode,
            currency = currency,
            vehicles = vehicles
        });
    });
}
```

## Common Patterns

### 1. Filter Vehicles by Country
```csharp
var vehicles = await db.Vehicles
    .Include(v => v.City)
    .ThenInclude(c => c.Country)
    .Where(v => v.City != null && 
               v.City.Country != null && 
               v.City.Country.Code == countryContext.CountryCode)
    .ToListAsync();
```

### 2. Filter Bookings by Country
```csharp
var bookings = await db.Bookings
    .Include(b => b.Vehicle)
    .ThenInclude(v => v.City)
    .ThenInclude(c => c.Country)
    .Where(b => b.Vehicle != null &&
               b.Vehicle.City != null &&
               b.Vehicle.City.Country != null &&
               b.Vehicle.City.Country.Code == countryContext.CountryCode)
    .ToListAsync();
```

### 3. Get Cities for Current Country
```csharp
var cities = await db.Cities
    .Include(c => c.Country)
    .Where(c => c.Country != null && 
               c.Country.Code == countryContext.CountryCode &&
               c.IsActive)
    .ToListAsync();
```

### 4. Use Country-Specific Currency
```csharp
var booking = new Booking
{
    Currency = countryContext.CurrencyCode, // GHS, NGN, KES, etc.
    RentalAmount = 500,
    // ... other fields
};
```

### 5. Get Country-Specific Payment Providers
```csharp
var paymentProviders = countryContext.GetEnabledPaymentProviders();
// Returns: ["paystack", "stripe"] for Ghana
// Returns: ["paystack", "flutterwave"] for Nigeria
// Returns: ["mpesa", "flutterwave"] for Kenya
```

### 6. Get Country-Specific Configuration
```csharp
// Store configuration in Countries.ConfigJson as:
// {"taxRate": 0.15, "bookingFee": 10}

var taxRate = countryContext.GetConfig<decimal>("taxRate") ?? 0.0m;
var bookingFee = countryContext.GetConfig<decimal>("bookingFee") ?? 0.0m;
```

## Service Integration

### In Services
```csharp
public class MyService
{
    private readonly AppDbContext _db;
    private readonly ICountryContext _countryContext;
    
    public MyService(AppDbContext db, ICountryContext countryContext)
    {
        _db = db;
        _countryContext = countryContext;
    }
    
    public async Task<List<Vehicle>> GetAvailableVehiclesAsync()
    {
        return await _db.Vehicles
            .Include(v => v.City)
            .ThenInclude(c => c.Country)
            .Where(v => v.City != null &&
                       v.City.Country != null &&
                       v.City.Country.Code == _countryContext.CountryCode &&
                       v.Status == "active")
            .ToListAsync();
    }
}
```

## Email Templates

Use country code to select templates:
```csharp
var templateKey = $"{countryContext.CountryCode}:BookingConfirmation";
// GH:BookingConfirmation for Ghana
// NG:BookingConfirmation for Nigeria

var template = await db.EmailTemplates
    .FirstOrDefaultAsync(t => t.TemplateKey == templateKey)
    ?? await db.EmailTemplates
        .FirstOrDefaultAsync(t => t.TemplateKey == "BookingConfirmation"); // fallback
```

## Price Calculations

```csharp
// Get regional pricing for current country
var regionalPricing = await db.RegionalPricings
    .Where(p => p.IsActive)
    .Include(p => p.Category)
    .FirstOrDefaultAsync(p => 
        p.City == cityName || 
        p.Region == regionName);

var priceMultiplier = regionalPricing?.PriceMultiplier ?? 1.0m;
var finalPrice = basePrice * priceMultiplier;
```

## Testing

### Route Examples
```
Default (Ghana):
GET /api/v1/cities
GET /api/v1/vehicles
GET /api/v1/bookings

Country-Specific:
GET /api/v1/gh/cities      (Ghana)
GET /api/v1/ng/cities      (Nigeria)
GET /api/v1/ke/cities      (Kenya)
GET /api/v1/za/cities      (South Africa)
```

### PowerShell Testing
```powershell
# Test default (Ghana)
Invoke-RestMethod -Uri "http://localhost:5000/api/v1/country/current"

# Test Nigeria
Invoke-RestMethod -Uri "http://localhost:5000/api/v1/ng/country/current"

# Test cities
Invoke-RestMethod -Uri "http://localhost:5000/api/v1/gh/cities"
Invoke-RestMethod -Uri "http://localhost:5000/api/v1/ng/cities"
```

## Migration Checklist

When updating existing endpoints:

1. ✅ Inject `ICountryContext` parameter
2. ✅ Filter queries by `countryContext.CountryCode`
3. ✅ Use `countryContext.CurrencyCode` for currency fields
4. ✅ Check payment providers with `countryContext.GetEnabledPaymentProviders()`
5. ✅ Test both default and country-specific routes
6. ✅ Update documentation

## Common Mistakes to Avoid

❌ **Don't hardcode country:**
```csharp
.Where(v => v.City.CountryCode == "GH")
```

✅ **Use context:**
```csharp
.Where(v => v.City.Country.Code == countryContext.CountryCode)
```

❌ **Don't hardcode currency:**
```csharp
booking.Currency = "GHS";
```

✅ **Use context:**
```csharp
booking.Currency = countryContext.CurrencyCode;
```

❌ **Don't forget to include Country in queries:**
```csharp
.Include(v => v.City) // Missing .ThenInclude(c => c.Country)
```

✅ **Include full navigation:**
```csharp
.Include(v => v.City)
.ThenInclude(c => c.Country)
```

## Performance Tip

For frequently accessed country data, the `CountryContext` caches the country object per request, so multiple calls to `countryContext.CountryCode` or other properties don't hit the database repeatedly.
