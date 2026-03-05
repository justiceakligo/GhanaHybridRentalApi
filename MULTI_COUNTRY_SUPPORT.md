# Multi-Country Support Implementation

## Overview
The API now supports multiple countries with a single codebase and database. Countries can be specified in the API route or will default to Ghana (GH) for backward compatibility.

## Route Structure

### Backward Compatible (Ghana Default)
```
/api/v1/bookings              → Ghana (GH)
/api/v1/vehicles              → Ghana (GH)
/api/v1/settings/public       → Ghana (GH)
```

### Country-Specific Routes
```
/api/v1/gh/bookings           → Ghana (GH)
/api/v1/ng/bookings           → Nigeria (NG)
/api/v1/ke/bookings           → Kenya (KE)
/api/v1/za/bookings           → South Africa (ZA)
```

## Architecture

### 1. Country Context Service
- **Interface**: `ICountryContext`
- **Implementation**: `CountryContext` (scoped service)
- Provides country-specific settings for each request:
  - Country code (GH, NG, KE, etc.)
  - Currency (GHS, NGN, KES, etc.)
  - Payment providers
  - Timezone
  - Configuration

### 2. Country Middleware
- **Location**: `Middleware/CountryMiddleware.cs`
- Extracts country code from route path
- Stores in `HttpContext.Items["CountryCode"]`
- Defaults to "GH" if no country specified

### 3. Country Model
- **Location**: `Models/Country.cs`
- **Database Table**: `Countries`
- Fields:
  - `Code`: ISO 3166-1 alpha-2 (GH, NG, KE)
  - `CurrencyCode`: ISO 4217 (GHS, NGN, KES)
  - `IsActive`: Enable/disable country operations
  - `IsDefault`: Mark default country for backward compatibility
  - `PaymentProvidersJson`: Enabled payment providers
  - `ConfigJson`: Country-specific settings

## Database Changes

### New Tables
- `Countries`: Stores country configuration

### Updated Tables
- `Cities`: Added `CountryId` foreign key to link cities to countries

### Migration Script
Run: `add-multi-country-support.sql`

This script:
- Creates `Countries` table
- Seeds Ghana as default country
- Seeds additional countries (inactive)
- Links existing cities to Ghana
- Creates necessary indexes

## Usage in Services

### Inject ICountryContext
```csharp
public class MyService
{
    private readonly ICountryContext _countryContext;
    
    public MyService(ICountryContext countryContext)
    {
        _countryContext = countryContext;
    }
    
    public async Task DoSomething()
    {
        var countryCode = _countryContext.CountryCode;
        var currency = _countryContext.CurrencyCode;
        
        // Filter data by country
        var vehicles = await db.Vehicles
            .Include(v => v.City)
            .Where(v => v.City.Country.Code == countryCode)
            .ToListAsync();
    }
}
```

### Filter Queries by Country
```csharp
// In endpoints or services
var vehicles = await db.Vehicles
    .Include(v => v.City)
    .ThenInclude(c => c.Country)
    .Where(v => v.City.Country != null && 
                v.City.Country.Code == _countryContext.CountryCode)
    .ToListAsync();
```

## Configuration

### Country-Specific Settings
Use `AppConfig` table with country-specific keys:
```
ConfigKey: "GH:Payment:Paystack:SecretKey"
ConfigKey: "NG:Payment:Paystack:SecretKey"
ConfigKey: "KE:Payment:MPesa:ShortCode"
```

Or use the `ConfigJson` field in the `Countries` table for structured settings.

### Payment Providers by Country
- **Ghana (GH)**: Paystack, Stripe
- **Nigeria (NG)**: Paystack, Flutterwave
- **Kenya (KE)**: M-Pesa, Flutterwave
- **South Africa (ZA)**: Paystack, Stripe

## Enabling New Countries

### Step 1: Activate Country
```sql
UPDATE "Countries" SET "IsActive" = true WHERE "Code" = 'NG';
```

### Step 2: Add Cities
```sql
INSERT INTO "Cities" ("Name", "Region", "CountryId", "IsActive")
SELECT 'Lagos', 'Lagos State', "Id", true
FROM "Countries" WHERE "Code" = 'NG';
```

### Step 3: Configure Payment Providers
Add country-specific payment configuration to `AppConfig`:
```sql
INSERT INTO "AppConfigs" ("ConfigKey", "ConfigValue")
VALUES ('NG:Payment:Paystack:SecretKey', 'sk_live_...');
```

### Step 4: Add Vehicles
Vehicles are automatically filtered by country through their city relationship.

## Testing

### Test Country Routes
```bash
# Ghana (default - no country prefix)
curl http://localhost:5000/api/v1/settings/public

# Ghana (explicit)
curl http://localhost:5000/api/v1/gh/settings/public

# Nigeria
curl http://localhost:5000/api/v1/ng/settings/public

# Kenya
curl http://localhost:5000/api/v1/ke/settings/public
```

### Verify Country Context
```bash
# Check which country is being used
curl http://localhost:5000/api/v1/gh/settings/public | jq '.currency'
# Should return: "GHS"

curl http://localhost:5000/api/v1/ng/settings/public | jq '.currency'
# Should return: "NGN"
```

## Phase Implementation

### Phase 1: Core Infrastructure ✅
- Country model and database
- Country context service
- Country middleware
- Backward compatible routing

### Phase 2: Country-Specific Features (Next)
- Payment provider selection by country
- Email templates per country
- Pricing rules per country
- Tax calculations per country

### Phase 3: Advanced Features (Future)
- Multi-currency display
- Language localization
- Country-specific legal compliance
- Regional marketing campaigns

## Backward Compatibility

**All existing routes continue to work without changes:**
- Routes without country prefix default to Ghana (GH)
- Existing mobile/web apps work without updates
- Database queries default to Ghana if no country specified
- All existing data remains accessible

## Migration Strategy

### Option 1: Gradual Rollout (Recommended)
1. Deploy multi-country code with Ghana as default
2. Test with Nigeria using `/api/v1/ng/` routes
3. Monitor and fix any country-specific issues
4. Activate additional countries one by one

### Option 2: Big Bang (All Countries at Once)
1. Activate all countries in database
2. Add cities for each country
3. Configure payment providers
4. Launch simultaneously

## Country Management Endpoints

Consider adding admin endpoints:
```
GET    /api/v1/admin/countries          - List all countries
POST   /api/v1/admin/countries          - Create country
PUT    /api/v1/admin/countries/{code}   - Update country
DELETE /api/v1/admin/countries/{code}   - Deactivate country
```

## Known Limitations

1. **Single Database**: All countries share one database. For data sovereignty requirements, consider database sharding by country.
2. **Currency Conversion**: No automatic currency conversion. Each country operates in its own currency.
3. **Legal Compliance**: Country-specific legal requirements must be manually configured.
4. **Localization**: Language support requires additional implementation.

## Support

For questions or issues with multi-country support, refer to this document or check the code in:
- `Services/ICountryContext.cs`
- `Services/CountryContext.cs`
- `Middleware/CountryMiddleware.cs`
- `Models/Country.cs`
