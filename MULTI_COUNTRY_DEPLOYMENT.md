# Multi-Country Deployment Guide

## Pre-Deployment Checklist

### 1. Code Review
- ✅ All country-related models created
- ✅ Country middleware registered
- ✅ Country context service registered
- ✅ Database migration script ready
- ✅ Endpoints updated to use country context
- ✅ Testing scripts prepared

### 2. Database Preparation
Before deploying, ensure you have:
- Database backup
- Migration script: `add-multi-country-support.sql`
- Rollback plan

## Deployment Steps

### Step 1: Deploy Code Changes
```powershell
# Build the application
dotnet build --configuration Release

# Run tests (if you have them)
dotnet test

# Publish
dotnet publish --configuration Release --output ./publish
```

### Step 2: Run Database Migration

#### Option A: Using Azure CLI (Production)
```powershell
# Set your resource group and server details
$resourceGroup = "your-resource-group"
$serverName = "your-server-name"
$databaseName = "your-database-name"

# Run the migration
az postgres flexible-server execute `
    --resource-group $resourceGroup `
    --name $serverName `
    --database-name $databaseName `
    --file-path "./add-multi-country-support.sql"
```

#### Option B: Using psql (Local or Remote)
```bash
psql -h your-host -U your-user -d your-database -f add-multi-country-support.sql
```

#### Option C: Using Azure Data Studio / pgAdmin
1. Open query window
2. Load `add-multi-country-support.sql`
3. Execute the script
4. Verify tables created:
   - `Countries` table exists
   - `Cities` table has `CountryId` column
   - Ghana (GH) is seeded and set as default

### Step 3: Verify Migration
```sql
-- Check Countries table
SELECT * FROM "Countries" ORDER BY "Name";

-- Check Cities are linked to Countries
SELECT c."Name", c."Region", co."Name" as "CountryName", co."Code"
FROM "Cities" c
LEFT JOIN "Countries" co ON c."CountryId" = co."Id"
LIMIT 10;

-- Verify Ghana is default
SELECT * FROM "Countries" WHERE "IsDefault" = true;
```

### Step 4: Deploy Application
```powershell
# Deploy to Azure App Service (example)
az webapp deployment source config-zip `
    --resource-group $resourceGroup `
    --name your-app-name `
    --src ./publish.zip

# Or use your CI/CD pipeline
# The application should restart automatically
```

### Step 5: Verify Deployment
```powershell
# Test default route (Ghana)
$baseUrl = "https://api.ryverental.com"
Invoke-RestMethod -Uri "$baseUrl/api/v1/countries"
Invoke-RestMethod -Uri "$baseUrl/api/v1/country/current"
Invoke-RestMethod -Uri "$baseUrl/api/v1/settings/public"

# Test explicit Ghana route
Invoke-RestMethod -Uri "$baseUrl/api/v1/gh/settings/public"

# Verify currency is returned
$settings = Invoke-RestMethod -Uri "$baseUrl/api/v1/settings/public"
Write-Host "Currency: $($settings.currency)" # Should be "GHS"
Write-Host "Country: $($settings.country)"   # Should be "GH"
```

### Step 6: Test Backward Compatibility
```powershell
# All these should still work (defaulting to Ghana)
Invoke-RestMethod -Uri "$baseUrl/api/v1/cities"
Invoke-RestMethod -Uri "$baseUrl/api/v1/vehicles"
Invoke-RestMethod -Uri "$baseUrl/api/v1/bookings/calculate-total"

# Verify existing mobile/web apps still work
# No changes required on frontend if they use default routes
```

## Post-Deployment Testing

### Test Suite
Run the comprehensive test script:
```powershell
.\test-multi-country.ps1
```

### Manual Testing Checklist
- [ ] Default routes work (no country prefix)
- [ ] Ghana routes work (/api/v1/gh/...)
- [ ] Currency is correct in responses
- [ ] Cities are filtered by country
- [ ] Vehicles are accessible
- [ ] Bookings can be created
- [ ] Settings endpoint returns country info
- [ ] Admin endpoints are accessible
- [ ] Existing apps still work without changes

## Activating Additional Countries

### Nigeria Example
```sql
-- 1. Activate Nigeria
UPDATE "Countries" SET "IsActive" = true WHERE "Code" = 'NG';

-- 2. Add cities
INSERT INTO "Cities" ("Name", "Region", "CountryCode", "CountryId", "IsActive")
SELECT 'Lagos', 'Lagos State', 'NG', "Id", true
FROM "Countries" WHERE "Code" = 'NG';

INSERT INTO "Cities" ("Name", "Region", "CountryCode", "CountryId", "IsActive")
SELECT 'Abuja', 'FCT', 'NG', "Id", true
FROM "Countries" WHERE "Code" = 'NG';

-- 3. Configure payment providers (if different from default)
UPDATE "Countries" 
SET "PaymentProvidersJson" = '["paystack", "flutterwave"]'
WHERE "Code" = 'NG';

-- 4. Add country-specific AppConfig (optional)
INSERT INTO "AppConfigs" ("ConfigKey", "ConfigValue")
VALUES 
    ('NG:Payment:Paystack:SecretKey', 'sk_live_...'),
    ('NG:Payment:Flutterwave:SecretKey', 'FLWSECK-...');
```

### Kenya Example
```sql
-- 1. Activate Kenya
UPDATE "Countries" SET "IsActive" = true WHERE "Code" = 'KE';

-- 2. Add cities
INSERT INTO "Cities" ("Name", "Region", "CountryCode", "CountryId", "IsActive")
SELECT 'Nairobi', 'Nairobi County', 'KE', "Id", true
FROM "Countries" WHERE "Code" = 'KE';

INSERT INTO "Cities" ("Name", "Region", "CountryCode", "CountryId", "IsActive")
SELECT 'Mombasa', 'Mombasa County', 'KE', "Id", true
FROM "Countries" WHERE "Code" = 'KE';

-- 3. Configure payment providers
UPDATE "Countries" 
SET "PaymentProvidersJson" = '["mpesa", "flutterwave"]'
WHERE "Code" = 'KE';

-- 4. Add M-Pesa configuration
INSERT INTO "AppConfigs" ("ConfigKey", "ConfigValue")
VALUES 
    ('KE:Payment:MPesa:ShortCode', '174379'),
    ('KE:Payment:MPesa:ConsumerKey', '...'),
    ('KE:Payment:MPesa:ConsumerSecret', '...');
```

## Rollback Plan

If issues arise, follow this rollback procedure:

### Emergency Rollback (Keep Countries, Revert Code)
```powershell
# Revert to previous deployment
az webapp deployment slot swap `
    --resource-group $resourceGroup `
    --name your-app-name `
    --slot staging

# Or redeploy previous version
git checkout previous-version
dotnet publish --configuration Release
# Deploy previous version
```

### Full Rollback (Remove Countries Table)
```sql
-- WARNING: This removes all country data
-- Only use if absolutely necessary

-- Remove foreign key
ALTER TABLE "Cities" DROP CONSTRAINT IF EXISTS "FK_Cities_Countries_CountryId";

-- Remove column
ALTER TABLE "Cities" DROP COLUMN IF EXISTS "CountryId";

-- Drop table
DROP TABLE IF EXISTS "Countries";
```

### Partial Rollback (Keep Table, Disable Countries)
```sql
-- Keep infrastructure but disable all except Ghana
UPDATE "Countries" SET "IsActive" = false WHERE "Code" != 'GH';

-- Application will continue working with Ghana only
```

## Monitoring and Alerts

### Metrics to Monitor
1. **Request Distribution**
   - Requests to default routes
   - Requests to country-specific routes
   - Country breakdown of requests

2. **Database Performance**
   - Query performance on Cities with Country join
   - Index usage on Countries table

3. **Errors**
   - Country not found errors
   - Invalid country code errors
   - Missing country context errors

### Application Insights Queries (if using Azure)
```kusto
// Country distribution
requests
| where timestamp > ago(24h)
| extend country = extract(@"/api/v1/([a-z]{2})/", 1, url)
| summarize count() by country
| order by count_ desc

// Country-specific errors
exceptions
| where timestamp > ago(24h)
| where message contains "Country"
| summarize count() by message
```

## Support Contacts

- **Database Issues**: DBA team
- **Code Issues**: Development team
- **Infrastructure**: DevOps team
- **Business Logic**: Product team

## Success Criteria

Deployment is successful when:
- ✅ All existing routes work without changes
- ✅ Ghana (GH) is accessible via both default and explicit routes
- ✅ Currency is correctly returned in API responses
- ✅ Cities are filtered by country
- ✅ No errors in application logs
- ✅ Existing mobile/web apps continue working
- ✅ Admin can manage countries via API

## Next Steps After Deployment

1. **Monitor for 24 hours** with Ghana only
2. **Gradually activate other countries** (Nigeria, Kenya)
3. **Add cities for new countries** via admin API
4. **Configure country-specific payment providers**
5. **Update frontend** to show country selector (optional)
6. **Create country-specific email templates**
7. **Set up country-specific pricing rules**
8. **Document country-specific business rules**

## Troubleshooting

### Issue: Country not found error
**Solution**: Ensure Countries table is seeded and Ghana is active
```sql
SELECT * FROM "Countries" WHERE "IsActive" = true;
```

### Issue: Cities not showing
**Solution**: Link cities to countries
```sql
UPDATE "Cities" 
SET "CountryId" = (SELECT "Id" FROM "Countries" WHERE "Code" = 'GH')
WHERE "CountryCode" = 'GH' AND "CountryId" IS NULL;
```

### Issue: Currency still showing as GHS for Nigeria
**Solution**: Verify country context is working
```powershell
Invoke-RestMethod -Uri "$baseUrl/api/v1/ng/country/current"
# Should return NGN as currency
```

### Issue: Existing routes broken
**Solution**: Check middleware is registered correctly
- Verify `UseCountryContext()` is in middleware pipeline
- Verify default country (GH) is set in database
- Check application logs for errors

## Changelog

### Version 1.228 - Multi-Country Support
- Added Countries table
- Added Country context service
- Added Country middleware
- Updated Settings endpoint
- Updated Cities endpoint
- Added Country admin endpoints
- Backward compatible with existing routes
- Default country: Ghana (GH)
