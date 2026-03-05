# 🌍 Multi-Country Support - Quick Start

## What Changed?

Your API now supports multiple countries with a **single codebase**! 

- ✅ **Ghana (GH)** is default - all existing routes work without changes
- ✅ **Nigeria, Kenya, South Africa, Tanzania** ready to activate
- ✅ **Backward compatible** - no breaking changes
- ✅ **Easy routing** - `/api/v1/{country}/...` or just `/api/v1/...` (defaults to Ghana)

## 🚀 Quick Start

### 1. Run Database Migration
```bash
# Apply the migration to create Countries table and seed data
psql -h your-host -U your-user -d your-database -f add-multi-country-support.sql
```

### 2. Build and Run
```bash
dotnet build
dotnet run
```

### 3. Test It
```powershell
# Run the automated test
.\test-multi-country.ps1

# Or test manually
curl http://localhost:5000/api/v1/countries
curl http://localhost:5000/api/v1/country/current
curl http://localhost:5000/api/v1/settings/public
```

## 📍 Route Structure

### Works as Before (Ghana)
```
GET /api/v1/cities              → Ghana cities
GET /api/v1/vehicles            → Ghana vehicles
GET /api/v1/bookings            → Ghana bookings
GET /api/v1/settings/public     → Currency: GHS
```

### New Country-Specific Routes
```
GET /api/v1/gh/cities           → Ghana cities (explicit)
GET /api/v1/ng/cities           → Nigeria cities
GET /api/v1/ke/cities           → Kenya cities
GET /api/v1/za/cities           → South Africa cities
```

## 🎯 Key Features

1. **Automatic Country Detection**
   - From URL: `/api/v1/ng/...` → Nigeria
   - Default: `/api/v1/...` → Ghana

2. **Country-Specific Data**
   - Currency (GHS, NGN, KES, etc.)
   - Payment providers per country
   - Cities filtered by country
   - Vehicles filtered by country

3. **Easy Extension**
   ```sql
   -- Activate a new country
   UPDATE "Countries" SET "IsActive" = true WHERE "Code" = 'NG';
   
   -- Add cities
   INSERT INTO "Cities" ("Name", "CountryId", ...)
   SELECT 'Lagos', "Id", ... FROM "Countries" WHERE "Code" = 'NG';
   ```

## 📚 Documentation

- **[IMPLEMENTATION_COMPLETE.md](IMPLEMENTATION_COMPLETE.md)** - What was implemented
- **[MULTI_COUNTRY_SUPPORT.md](MULTI_COUNTRY_SUPPORT.md)** - Architecture overview
- **[COUNTRY_CONTEXT_GUIDE.md](COUNTRY_CONTEXT_GUIDE.md)** - Developer guide
- **[MULTI_COUNTRY_DEPLOYMENT.md](MULTI_COUNTRY_DEPLOYMENT.md)** - Deployment guide

## 🔧 For Developers

### Using Country Context in Code
```csharp
// Inject ICountryContext in any endpoint
app.MapGet("/api/v1/my-endpoint", async (
    AppDbContext db,
    ICountryContext countryContext) =>
{
    // Get country info
    var country = countryContext.CountryCode;      // "GH", "NG", "KE"
    var currency = countryContext.CurrencyCode;    // "GHS", "NGN", "KES"
    var symbol = countryContext.CurrencySymbol;    // "₵", "₦", "KSh"
    
    // Filter data by country
    var cities = await db.Cities
        .Include(c => c.Country)
        .Where(c => c.Country.Code == country)
        .ToListAsync();
    
    return Results.Ok(new { country, currency, cities });
});
```

## 🌟 API Endpoints

### Public Endpoints
```
GET  /api/v1/countries              List active countries
GET  /api/v1/country/current        Get current country context
GET  /api/v1/cities                 Get cities (filtered by country)
GET  /api/v1/settings/public        Get settings (includes currency)
```

### Admin Endpoints
```
GET    /api/v1/admin/countries           List all countries
POST   /api/v1/admin/countries           Create country
PUT    /api/v1/admin/countries/{code}    Update country
DELETE /api/v1/admin/countries/{code}    Deactivate country
POST   /api/v1/admin/countries/{code}/activate

GET    /api/v1/admin/cities              List all cities
POST   /api/v1/admin/cities              Create city
PUT    /api/v1/admin/cities/{id}         Update city
DELETE /api/v1/admin/cities/{id}         Delete city
```

## 🗄️ Database

### New Table: Countries
Stores country configuration including currency, payment providers, timezone, etc.

### Updated: Cities
Added `CountryId` foreign key linking cities to countries.

### Seeded Countries
- 🇬🇭 Ghana (GH) - **Active & Default**
- 🇳🇬 Nigeria (NG) - Inactive (ready to activate)
- 🇰🇪 Kenya (KE) - Inactive (ready to activate)
- 🇿🇦 South Africa (ZA) - Inactive (ready to activate)
- 🇹🇿 Tanzania (TZ) - Inactive (ready to activate)

## ✅ Testing Checklist

After deployment, verify:
- [ ] Existing routes work (no country prefix)
- [ ] Ghana routes work (/api/v1/gh/...)
- [ ] Currency is "GHS" for Ghana
- [ ] Cities are returned
- [ ] Settings endpoint returns country info
- [ ] No errors in logs
- [ ] Existing mobile/web apps work

## 🎮 Example Usage

### From Frontend
```javascript
// Default (Ghana)
const settings = await fetch('/api/v1/settings/public').then(r => r.json());
console.log(settings.currency); // "GHS"

// Nigeria
const settingsNG = await fetch('/api/v1/ng/settings/public').then(r => r.json());
console.log(settingsNG.currency); // "NGN"

// Get available countries
const countries = await fetch('/api/v1/countries').then(r => r.json());
```

### From Mobile
```dart
// Default (Ghana) - existing code works!
final response = await http.get('/api/v1/vehicles');

// Or explicit country
final response = await http.get('/api/v1/gh/vehicles');
```

## 🆘 Troubleshooting

### Issue: Countries table doesn't exist
**Solution:** Run the migration script
```bash
psql -f add-multi-country-support.sql
```

### Issue: Currency is still "GHS" for Nigeria
**Solution:** Ensure Nigeria is activated
```sql
UPDATE "Countries" SET "IsActive" = true WHERE "Code" = 'NG';
```

### Issue: No cities returned
**Solution:** Add cities for the country or link existing cities
```sql
-- Link existing cities to Ghana
UPDATE "Cities" 
SET "CountryId" = (SELECT "Id" FROM "Countries" WHERE "Code" = 'GH')
WHERE "CountryCode" = 'GH';
```

## 📞 Support

- Check logs for errors
- Run test script: `.\test-multi-country.ps1`
- Review documentation in this folder
- Check the code for examples

## 🎉 You're Done!

Your API now supports multiple countries. Start with Ghana (default), then gradually activate other countries as needed.

**No changes required for existing apps - they'll continue using Ghana by default!**
