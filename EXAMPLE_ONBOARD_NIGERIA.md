# Example: Onboarding Nigeria Step-by-Step

This is a real-world example of onboarding Nigeria to the platform using the admin endpoints.

## Prerequisites
```powershell
$token = "your-admin-jwt-token"
$baseUrl = "https://api.ryverental.com"

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}
```

## Step 1: Create Nigeria

```powershell
Write-Host "Creating Nigeria..." -ForegroundColor Yellow

$createBody = @{
    code = "NG"
    name = "Nigeria"
    currencyCode = "NGN"
    currencySymbol = "₦"
    phoneCode = "+234"
    timezone = "Africa/Lagos"
    defaultLanguage = "en-NG"
    isActive = $false  # Start inactive
    paymentProviders = @("paystack", "flutterwave")
} | ConvertTo-Json

$country = Invoke-RestMethod `
    -Uri "$baseUrl/api/v1/admin/countries" `
    -Headers $headers `
    -Method Post `
    -Body $createBody

Write-Host "✓ Created: $($country.name)" -ForegroundColor Green
```

**Result:**
```json
{
  "id": "123e4567-e89b-12d3-a456-426614174000",
  "code": "NG",
  "name": "Nigeria",
  "currencyCode": "NGN",
  "currencySymbol": "₦",
  "isActive": false
}
```

## Step 2: Add Major Cities

```powershell
Write-Host "Adding cities..." -ForegroundColor Yellow

$citiesBody = @{
    cities = @(
        @{
            name = "Lagos"
            region = "Lagos State"
            displayOrder = 1
            defaultDeliveryFee = 500
            isActive = $true
        },
        @{
            name = "Abuja"
            region = "FCT"
            displayOrder = 2
            defaultDeliveryFee = 600
            isActive = $true
        },
        @{
            name = "Port Harcourt"
            region = "Rivers State"
            displayOrder = 3
            defaultDeliveryFee = 450
            isActive = $true
        },
        @{
            name = "Kano"
            region = "Kano State"
            displayOrder = 4
            defaultDeliveryFee = 400
            isActive = $true
        },
        @{
            name = "Ibadan"
            region = "Oyo State"
            displayOrder = 5
            defaultDeliveryFee = 400
            isActive = $true
        }
    )
} | ConvertTo-Json -Depth 3

$citiesResult = Invoke-RestMethod `
    -Uri "$baseUrl/api/v1/admin/countries/NG/cities/bulk" `
    -Headers $headers `
    -Method Post `
    -Body $citiesBody

Write-Host "✓ Created $($citiesResult.created) cities" -ForegroundColor Green
```

**Result:**
```json
{
  "message": "Created 5 cities for Nigeria",
  "created": 5,
  "cities": [
    {"id": "...", "name": "Lagos", "region": "Lagos State"},
    {"id": "...", "name": "Abuja", "region": "FCT"}
  ]
}
```

## Step 3: Configure Paystack (Nigeria)

```powershell
Write-Host "Configuring Paystack..." -ForegroundColor Yellow

# IMPORTANT: Get these from https://dashboard.paystack.com
$paystackConfig = @{
    settings = @{
        "Payment:Paystack:SecretKey" = "sk_live_your_nigeria_secret_key"
        "Payment:Paystack:PublicKey" = "pk_live_your_nigeria_public_key"
        "Payment:Paystack:CallbackUrl" = "https://api.ryverental.com/api/v1/webhooks/paystack"
    }
} | ConvertTo-Json

$paymentResult = Invoke-RestMethod `
    -Uri "$baseUrl/api/v1/admin/countries/NG/app-config" `
    -Headers $headers `
    -Method Post `
    -Body $paystackConfig

Write-Host "✓ Paystack configured" -ForegroundColor Green
```

## Step 4: Configure Flutterwave (Optional)

```powershell
Write-Host "Configuring Flutterwave..." -ForegroundColor Yellow

$flutterwaveConfig = @{
    settings = @{
        "Payment:Flutterwave:SecretKey" = "FLWSECK-your-secret-key"
        "Payment:Flutterwave:PublicKey" = "FLWPUBK-your-public-key"
        "Payment:Flutterwave:EncryptionKey" = "FLWSECK_TEST-your-encryption-key"
    }
} | ConvertTo-Json

Invoke-RestMethod `
    -Uri "$baseUrl/api/v1/admin/countries/NG/app-config" `
    -Headers $headers `
    -Method Post `
    -Body $flutterwaveConfig

Write-Host "✓ Flutterwave configured" -ForegroundColor Green
```

## Step 5: Set Country Configuration

```powershell
Write-Host "Setting country-specific configuration..." -ForegroundColor Yellow

$countryConfig = @{
    config = @{
        taxRate = 0.075  # 7.5% VAT in Nigeria
        platformFeePercentage = 15
        minBookingAmount = 10000  # NGN 10,000
        supportEmail = "support@ryverental.ng"
        supportPhone = "+234-800-RYVE-RENT"
        emergencyContact = "+234-XXX-XXX-XXXX"
        businessHours = "Mon-Fri 8AM-6PM WAT"
    }
} | ConvertTo-Json -Depth 3

$configResult = Invoke-RestMethod `
    -Uri "$baseUrl/api/v1/admin/countries/NG/config" `
    -Headers $headers `
    -Method Put `
    -Body $countryConfig

Write-Host "✓ Country configuration set" -ForegroundColor Green
```

## Step 6: Check Onboarding Status

```powershell
Write-Host "Checking onboarding status..." -ForegroundColor Yellow

$status = Invoke-RestMethod `
    -Uri "$baseUrl/api/v1/admin/countries/NG/onboarding-status" `
    -Headers $headers `
    -Method Get

Write-Host "Progress: $($status.progress.percentage)%" -ForegroundColor Cyan
Write-Host "Ready: $($status.isReady)" -ForegroundColor $(if ($status.isReady) { "Green" } else { "Red" })

Write-Host "`nChecklist:" -ForegroundColor Cyan
foreach ($item in $status.checklist) {
    $icon = if ($item.completed) { "✓" } else { "✗" }
    $color = if ($item.completed) { "Green" } else { "Red" }
    Write-Host "  $icon $($item.step)" -ForegroundColor $color
}

if ($status.recommendations) {
    Write-Host "`nRecommendations:" -ForegroundColor Yellow
    foreach ($rec in $status.recommendations) {
        Write-Host "  - $rec" -ForegroundColor Yellow
    }
}
```

**Expected Output:**
```
Progress: 100%
Ready: True

Checklist:
  ✓ Country Created
  ✓ Currency Configured
  ✓ Payment Providers Set
  ✓ Payment Configuration
  ✓ Cities Added
  ✗ Vehicles Listed (optional)
  ✗ Country Activated
```

## Step 7: Test Configuration

```powershell
Write-Host "Testing configuration..." -ForegroundColor Yellow

$testResult = Invoke-RestMethod `
    -Uri "$baseUrl/api/v1/admin/countries/NG/test" `
    -Headers $headers `
    -Method Post

Write-Host "Overall Status: $($testResult.overallStatus)" -ForegroundColor $(
    if ($testResult.overallStatus -eq "PASSED") { "Green" } else { "Red" }
)

Write-Host "`nTest Results:" -ForegroundColor Cyan
foreach ($test in $testResult.tests) {
    $icon = if ($test.passed) { "✓" } else { "✗" }
    $color = if ($test.passed) { "Green" } else { "Red" }
    $optional = if ($test.optional) { " (optional)" } else { "" }
    Write-Host "  $icon $($test.test)$optional" -ForegroundColor $color
}

Write-Host "`nReady for activation: $($testResult.isReady)" -ForegroundColor Green
```

**Expected Output:**
```
Overall Status: PASSED

Test Results:
  ✓ Basic Information
  ✓ Payment Providers
  ✓ Cities
  ✓ Payment Configuration
  ✗ Vehicles (optional)

Ready for activation: True
```

## Step 8: Activate Nigeria

```powershell
Write-Host "Activating Nigeria..." -ForegroundColor Yellow

$activateResult = Invoke-RestMethod `
    -Uri "$baseUrl/api/v1/admin/countries/NG/onboarding/complete" `
    -Headers $headers `
    -Method Post

Write-Host "✓ Nigeria is now ACTIVE!" -ForegroundColor Green
Write-Host "Country: $($activateResult.country)" -ForegroundColor Cyan
Write-Host "Code: $($activateResult.countryCode)" -ForegroundColor Cyan
Write-Host "Activated at: $($activateResult.completedAt)" -ForegroundColor Cyan
```

**Result:**
```json
{
  "message": "Nigeria onboarding completed successfully",
  "country": "Nigeria",
  "countryCode": "NG",
  "isActive": true,
  "completedAt": "2026-03-05T14:30:00Z"
}
```

## Step 9: Verify Public Access

```powershell
Write-Host "`nVerifying public access..." -ForegroundColor Yellow

# Test public endpoints (no auth needed)
Write-Host "Testing: GET /api/v1/ng/country/current" -ForegroundColor Cyan
$currentCountry = Invoke-RestMethod -Uri "$baseUrl/api/v1/ng/country/current"
Write-Host "Currency: $($currentCountry.currencyCode)" -ForegroundColor Green

Write-Host "Testing: GET /api/v1/ng/cities" -ForegroundColor Cyan
$cities = Invoke-RestMethod -Uri "$baseUrl/api/v1/ng/cities"
Write-Host "Cities found: $($cities.Count)" -ForegroundColor Green

Write-Host "Testing: GET /api/v1/ng/settings/public" -ForegroundColor Cyan
$settings = Invoke-RestMethod -Uri "$baseUrl/api/v1/ng/settings/public"
Write-Host "Currency: $($settings.currency)" -ForegroundColor Green
Write-Host "Country: $($settings.countryName)" -ForegroundColor Green
```

**Expected Output:**
```
Testing: GET /api/v1/ng/country/current
Currency: NGN

Testing: GET /api/v1/ng/cities
Cities found: 5

Testing: GET /api/v1/ng/settings/public
Currency: NGN
Country: Nigeria
```

## Step 10: Check Final Stats

```powershell
Write-Host "`nChecking final statistics..." -ForegroundColor Yellow

$stats = Invoke-RestMethod `
    -Uri "$baseUrl/api/v1/admin/countries/NG/stats" `
    -Headers $headers `
    -Method Get

Write-Host "Country: $($stats.country)" -ForegroundColor Cyan
Write-Host "Status: $(if ($stats.isActive) { 'ACTIVE' } else { 'INACTIVE' })" -ForegroundColor Green
Write-Host "`nStatistics:" -ForegroundColor Cyan
Write-Host "  Cities: $($stats.stats.cities.total) (Active: $($stats.stats.cities.active))" -ForegroundColor White
Write-Host "  Vehicles: $($stats.stats.vehicles.total) (Active: $($stats.stats.vehicles.active))" -ForegroundColor White
Write-Host "  Bookings: $($stats.stats.bookings.total)" -ForegroundColor White
Write-Host "  Owners: $($stats.stats.owners.total)" -ForegroundColor White
```

## Complete Script

Save all the steps above into a single file `onboard-nigeria-example.ps1` and run:

```powershell
.\onboard-nigeria-example.ps1
```

## What Happens Next?

After activation:

1. **Users can access Nigeria-specific endpoints:**
   - `GET /api/v1/ng/cities` - See cities in Nigeria
   - `GET /api/v1/ng/vehicles` - See vehicles in Nigeria
   - `POST /api/v1/ng/bookings` - Book vehicles in Nigeria

2. **All prices show in NGN (₦)**
   - Bookings are in Nigerian Naira
   - Payments go through Paystack Nigeria account

3. **Owners can list vehicles in Nigerian cities**
   - Select Lagos, Abuja, etc. as vehicle location
   - Set prices in NGN

4. **Automatic filtering by country**
   - Vehicles in Lagos only show on `/api/v1/ng/vehicles`
   - Ghana vehicles stay separate on `/api/v1/gh/vehicles`

## Monitoring

After activation, monitor:

```powershell
# Check stats daily
$stats = Invoke-RestMethod `
    -Uri "$baseUrl/api/v1/admin/countries/NG/stats" `
    -Headers $headers

# Monitor growth
Write-Host "Vehicles: $($stats.stats.vehicles.total)"
Write-Host "Bookings: $($stats.stats.bookings.total)"
Write-Host "Owners: $($stats.stats.owners.total)"
```

## Rollback (If Needed)

If something goes wrong:

```powershell
# Deactivate Nigeria
Invoke-RestMethod `
    -Uri "$baseUrl/api/v1/admin/countries/NG" `
    -Headers $headers `
    -Method Delete

Write-Host "✓ Nigeria deactivated" -ForegroundColor Yellow
```

To reactivate later:

```powershell
# Reactivate Nigeria
Invoke-RestMethod `
    -Uri "$baseUrl/api/v1/admin/countries/NG/activate" `
    -Headers $headers `
    -Method Post

Write-Host "✓ Nigeria reactivated" -ForegroundColor Green
```

## Summary

You've successfully onboarded Nigeria! 🇳🇬

**What was created:**
- ✓ Country: Nigeria (NG)
- ✓ Currency: NGN (₦)
- ✓ Cities: 5 major cities
- ✓ Payment: Paystack + Flutterwave
- ✓ Status: ACTIVE

**Next steps:**
1. Add more cities as needed
2. Onboard vehicle owners in Nigeria
3. Launch marketing campaign
4. Monitor stats and growth

**Repeat for other countries:**
- Kenya (KE): Use `onboard-country.ps1 -CountryCode KE`
- South Africa (ZA): Use `onboard-country.ps1 -CountryCode ZA`
- Tanzania (TZ): Use `onboard-country.ps1 -CountryCode TZ`
