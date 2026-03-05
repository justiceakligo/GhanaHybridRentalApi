# 🛠️ Admin Country Management - Quick Reference

## Quick Setup (Automated)
```powershell
# Onboard Nigeria with all defaults
.\onboard-country.ps1 -CountryCode NG -AdminToken "your-admin-token"

# Onboard Kenya
.\onboard-country.ps1 -CountryCode KE -AdminToken "your-admin-token"
```

## Core Admin Endpoints

### 📋 Country CRUD
```http
GET    /api/v1/admin/countries              # List all
GET    /api/v1/admin/countries/{code}       # Get one
POST   /api/v1/admin/countries              # Create
PUT    /api/v1/admin/countries/{code}       # Update
DELETE /api/v1/admin/countries/{code}       # Deactivate
POST   /api/v1/admin/countries/{code}/activate
```

### 💳 Payment Configuration
```http
PUT  /api/v1/admin/countries/{code}/payment-providers
POST /api/v1/admin/countries/{code}/app-config
GET  /api/v1/admin/countries/{code}/app-config
```

### 🏙️ City Management
```http
POST /api/v1/admin/countries/{code}/cities/bulk
GET  /api/v1/admin/countries/{code}/cities
```

### ⚙️ Configuration
```http
PUT /api/v1/admin/countries/{code}/config
```

### 📊 Monitoring
```http
GET  /api/v1/admin/countries/{code}/onboarding-status
POST /api/v1/admin/countries/{code}/test
GET  /api/v1/admin/countries/{code}/stats
POST /api/v1/admin/countries/{code}/onboarding/complete
```

## Example: Create Nigeria

### 1. Create Country
```bash
curl -X POST https://api.ryverental.com/api/v1/admin/countries \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "code": "NG",
    "name": "Nigeria",
    "currencyCode": "NGN",
    "currencySymbol": "₦",
    "phoneCode": "+234",
    "timezone": "Africa/Lagos",
    "defaultLanguage": "en-NG",
    "isActive": false,
    "paymentProviders": ["paystack", "flutterwave"]
  }'
```

### 2. Add Cities
```bash
curl -X POST https://api.ryverental.com/api/v1/admin/countries/NG/cities/bulk \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "cities": [
      {"name": "Lagos", "region": "Lagos State", "displayOrder": 1},
      {"name": "Abuja", "region": "FCT", "displayOrder": 2}
    ]
  }'
```

### 3. Configure Payment
```bash
curl -X POST https://api.ryverental.com/api/v1/admin/countries/NG/app-config \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "settings": {
      "Payment:Paystack:SecretKey": "sk_live_...",
      "Payment:Paystack:PublicKey": "pk_live_..."
    }
  }'
```

### 4. Check Status
```bash
curl https://api.ryverental.com/api/v1/admin/countries/NG/onboarding-status \
  -H "Authorization: Bearer $TOKEN"
```

### 5. Activate
```bash
curl -X POST https://api.ryverental.com/api/v1/admin/countries/NG/onboarding/complete \
  -H "Authorization: Bearer $TOKEN"
```

## PowerShell Examples

### Create Country
```powershell
$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

$body = @{
    code = "NG"
    name = "Nigeria"
    currencyCode = "NGN"
    currencySymbol = "₦"
    phoneCode = "+234"
    timezone = "Africa/Lagos"
    defaultLanguage = "en-NG"
    isActive = $false
    paymentProviders = @("paystack", "flutterwave")
} | ConvertTo-Json

Invoke-RestMethod -Uri "$baseUrl/api/v1/admin/countries" `
    -Headers $headers -Method Post -Body $body
```

### Bulk Add Cities
```powershell
$body = @{
    cities = @(
        @{ name = "Lagos"; region = "Lagos State"; displayOrder = 1 },
        @{ name = "Abuja"; region = "FCT"; displayOrder = 2 }
    )
} | ConvertTo-Json -Depth 3

Invoke-RestMethod -Uri "$baseUrl/api/v1/admin/countries/NG/cities/bulk" `
    -Headers $headers -Method Post -Body $body
```

### Check Status
```powershell
$status = Invoke-RestMethod -Uri "$baseUrl/api/v1/admin/countries/NG/onboarding-status" `
    -Headers $headers

Write-Host "Progress: $($status.progress.percentage)%"
Write-Host "Ready: $($status.isReady)"
```

### Test Configuration
```powershell
$test = Invoke-RestMethod -Uri "$baseUrl/api/v1/admin/countries/NG/test" `
    -Headers $headers -Method Post

Write-Host "Status: $($test.overallStatus)"
$test.tests | ForEach-Object {
    Write-Host "$($_.test): $(if ($_.passed) { 'PASSED' } else { 'FAILED' })"
}
```

## Configuration Examples

### Nigeria
```json
{
  "code": "NG",
  "currencyCode": "NGN",
  "currencySymbol": "₦",
  "paymentProviders": ["paystack", "flutterwave"],
  "config": {
    "taxRate": 0.075,
    "supportEmail": "support@ryverental.ng"
  }
}
```

### Kenya
```json
{
  "code": "KE",
  "currencyCode": "KES",
  "currencySymbol": "KSh",
  "paymentProviders": ["mpesa", "flutterwave"],
  "config": {
    "taxRate": 0.16,
    "supportEmail": "support@ryverental.ke"
  }
}
```

### South Africa
```json
{
  "code": "ZA",
  "currencyCode": "ZAR",
  "currencySymbol": "R",
  "paymentProviders": ["paystack", "stripe"],
  "config": {
    "taxRate": 0.15,
    "supportEmail": "support@ryverental.co.za"
  }
}
```

## Onboarding Checklist

- [ ] Create country (inactive)
- [ ] Set payment providers
- [ ] Add cities
- [ ] Configure payment credentials
- [ ] Set country config
- [ ] Test configuration
- [ ] Check onboarding status
- [ ] Activate country

## Monitoring Commands

### Get Stats
```bash
curl https://api.ryverental.com/api/v1/admin/countries/NG/stats \
  -H "Authorization: Bearer $TOKEN"
```

### List Cities
```bash
curl https://api.ryverental.com/api/v1/admin/countries/NG/cities \
  -H "Authorization: Bearer $TOKEN"
```

### Get Payment Config
```bash
curl https://api.ryverental.com/api/v1/admin/countries/NG/app-config \
  -H "Authorization: Bearer $TOKEN"
```

## Common Tasks

### Update Payment Providers
```bash
curl -X PUT https://api.ryverental.com/api/v1/admin/countries/NG/payment-providers \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"providers": ["paystack", "flutterwave", "stripe"]}'
```

### Update Country Config
```bash
curl -X PUT https://api.ryverental.com/api/v1/admin/countries/NG/config \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"config": {"taxRate": 0.1, "supportEmail": "new@email.com"}}'
```

### Deactivate Country
```bash
curl -X DELETE https://api.ryverental.com/api/v1/admin/countries/NG \
  -H "Authorization: Bearer $TOKEN"
```

### Reactivate Country
```bash
curl -X POST https://api.ryverental.com/api/v1/admin/countries/NG/activate \
  -H "Authorization: Bearer $TOKEN"
```

## Response Examples

### Onboarding Status
```json
{
  "country": "Nigeria",
  "countryCode": "NG",
  "isReady": true,
  "progress": {
    "completed": 5,
    "total": 5,
    "percentage": 100
  },
  "checklist": [
    {"step": "Country Created", "completed": true},
    {"step": "Payment Providers Set", "completed": true},
    {"step": "Cities Added", "completed": true}
  ]
}
```

### Test Results
```json
{
  "overallStatus": "PASSED",
  "testsRun": 5,
  "testsPassed": 5,
  "isReady": true,
  "tests": [
    {"test": "Basic Information", "passed": true},
    {"test": "Payment Providers", "passed": true},
    {"test": "Cities", "passed": true}
  ]
}
```

### Stats
```json
{
  "country": "Nigeria",
  "stats": {
    "cities": {"total": 6, "active": 6},
    "vehicles": {"total": 45, "active": 38},
    "bookings": {"total": 127},
    "owners": {"total": 15}
  }
}
```

## Error Codes
- `400` - Invalid input or missing requirements
- `401` - Unauthorized (invalid token)
- `403` - Forbidden (not admin)
- `404` - Country not found
- `409` - Conflict (country already exists)

## Tips

1. **Always create inactive** - Set `isActive: false` initially
2. **Test before activating** - Use test endpoint
3. **Check status** - Verify all requirements met
4. **Secure credentials** - Never commit payment keys
5. **Monitor stats** - Check stats after activation

## Support
- Full Guide: `COUNTRY_ONBOARDING_ADMIN_GUIDE.md`
- Automated Script: `onboard-country.ps1`
- Architecture: `MULTI_COUNTRY_SUPPORT.md`
