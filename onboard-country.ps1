# Onboard a New Country - Automated Script
# Usage: .\onboard-country.ps1 -CountryCode NG -AdminToken "your-token"

param(
    [Parameter(Mandatory=$true)]
    [string]$CountryCode,
    
    [Parameter(Mandatory=$true)]
    [string]$AdminToken,
    
    [string]$BaseUrl = "https://api.ryverental.com"
    # [string]$BaseUrl = "http://localhost:5000"
)

$headers = @{
    "Authorization" = "Bearer $AdminToken"
    "Content-Type" = "application/json"
}

Write-Host "=== Country Onboarding Script ===" -ForegroundColor Cyan
Write-Host "Country Code: $CountryCode" -ForegroundColor Yellow
Write-Host "Base URL: $BaseUrl" -ForegroundColor Yellow
Write-Host ""

# Country configurations
$countries = @{
    "NG" = @{
        name = "Nigeria"
        currencyCode = "NGN"
        currencySymbol = "₦"
        phoneCode = "+234"
        timezone = "Africa/Lagos"
        defaultLanguage = "en-NG"
        paymentProviders = @("paystack", "flutterwave")
        cities = @(
            @{ name = "Lagos"; region = "Lagos State"; displayOrder = 1; defaultDeliveryFee = 500 },
            @{ name = "Abuja"; region = "FCT"; displayOrder = 2; defaultDeliveryFee = 600 },
            @{ name = "Port Harcourt"; region = "Rivers State"; displayOrder = 3; defaultDeliveryFee = 450 },
            @{ name = "Kano"; region = "Kano State"; displayOrder = 4; defaultDeliveryFee = 400 },
            @{ name = "Ibadan"; region = "Oyo State"; displayOrder = 5; defaultDeliveryFee = 400 },
            @{ name = "Kaduna"; region = "Kaduna State"; displayOrder = 6; defaultDeliveryFee = 450 }
        )
        config = @{
            taxRate = 0.075
            platformFeePercentage = 15
            supportEmail = "support@ryverental.ng"
            supportPhone = "+234-800-RYVE-RENT"
        }
    }
    "KE" = @{
        name = "Kenya"
        currencyCode = "KES"
        currencySymbol = "KSh"
        phoneCode = "+254"
        timezone = "Africa/Nairobi"
        defaultLanguage = "en-KE"
        paymentProviders = @("mpesa", "flutterwave")
        cities = @(
            @{ name = "Nairobi"; region = "Nairobi County"; displayOrder = 1; defaultDeliveryFee = 500 },
            @{ name = "Mombasa"; region = "Mombasa County"; displayOrder = 2; defaultDeliveryFee = 600 },
            @{ name = "Kisumu"; region = "Kisumu County"; displayOrder = 3; defaultDeliveryFee = 450 },
            @{ name = "Nakuru"; region = "Nakuru County"; displayOrder = 4; defaultDeliveryFee = 400 },
            @{ name = "Eldoret"; region = "Uasin Gishu County"; displayOrder = 5; defaultDeliveryFee = 450 }
        )
        config = @{
            taxRate = 0.16
            platformFeePercentage = 15
            supportEmail = "support@ryverental.ke"
            supportPhone = "+254-800-RYVE-RENT"
        }
    }
    "ZA" = @{
        name = "South Africa"
        currencyCode = "ZAR"
        currencySymbol = "R"
        phoneCode = "+27"
        timezone = "Africa/Johannesburg"
        defaultLanguage = "en-ZA"
        paymentProviders = @("paystack", "stripe")
        cities = @(
            @{ name = "Johannesburg"; region = "Gauteng"; displayOrder = 1; defaultDeliveryFee = 150 },
            @{ name = "Cape Town"; region = "Western Cape"; displayOrder = 2; defaultDeliveryFee = 200 },
            @{ name = "Durban"; region = "KwaZulu-Natal"; displayOrder = 3; defaultDeliveryFee = 180 },
            @{ name = "Pretoria"; region = "Gauteng"; displayOrder = 4; defaultDeliveryFee = 150 },
            @{ name = "Port Elizabeth"; region = "Eastern Cape"; displayOrder = 5; defaultDeliveryFee = 170 }
        )
        config = @{
            taxRate = 0.15
            platformFeePercentage = 15
            supportEmail = "support@ryverental.co.za"
            supportPhone = "+27-800-RYVE-RENT"
        }
    }
    "TZ" = @{
        name = "Tanzania"
        currencyCode = "TZS"
        currencySymbol = "TSh"
        phoneCode = "+255"
        timezone = "Africa/Dar_es_Salaam"
        defaultLanguage = "sw-TZ"
        paymentProviders = @("flutterwave", "mpesa")
        cities = @(
            @{ name = "Dar es Salaam"; region = "Dar es Salaam"; displayOrder = 1; defaultDeliveryFee = 10000 },
            @{ name = "Dodoma"; region = "Dodoma"; displayOrder = 2; defaultDeliveryFee = 12000 },
            @{ name = "Mwanza"; region = "Mwanza"; displayOrder = 3; defaultDeliveryFee = 11000 },
            @{ name = "Arusha"; region = "Arusha"; displayOrder = 4; defaultDeliveryFee = 11000 }
        )
        config = @{
            taxRate = 0.18
            platformFeePercentage = 15
            supportEmail = "support@ryverental.tz"
            supportPhone = "+255-800-RYVE-RENT"
        }
    }
}

if (-not $countries.ContainsKey($CountryCode.ToUpper())) {
    Write-Host "Error: Country code $CountryCode not found in configuration" -ForegroundColor Red
    Write-Host "Supported countries: NG, KE, ZA, TZ" -ForegroundColor Yellow
    exit 1
}

$countryData = $countries[$CountryCode.ToUpper()]

# Step 1: Check if country already exists
Write-Host "Step 1: Checking if country exists..." -ForegroundColor Yellow
try {
    $existing = Invoke-RestMethod -Uri "$BaseUrl/api/v1/admin/countries/$CountryCode" -Headers $headers -Method Get -ErrorAction SilentlyContinue
    Write-Host "✓ Country already exists. Updating..." -ForegroundColor Green
    $countryExists = $true
} catch {
    Write-Host "✓ Country doesn't exist. Will create..." -ForegroundColor Green
    $countryExists = $false
}
Write-Host ""

# Step 2: Create or update country
if (-not $countryExists) {
    Write-Host "Step 2: Creating country..." -ForegroundColor Yellow
    $createBody = @{
        code = $CountryCode.ToUpper()
        name = $countryData.name
        currencyCode = $countryData.currencyCode
        currencySymbol = $countryData.currencySymbol
        phoneCode = $countryData.phoneCode
        timezone = $countryData.timezone
        defaultLanguage = $countryData.defaultLanguage
        isActive = $false
        paymentProviders = $countryData.paymentProviders
    } | ConvertTo-Json

    try {
        $result = Invoke-RestMethod -Uri "$BaseUrl/api/v1/admin/countries" -Headers $headers -Method Post -Body $createBody
        Write-Host "✓ Country created successfully" -ForegroundColor Green
        $result | ConvertTo-Json -Depth 3
    } catch {
        Write-Host "✗ Failed to create country: $_" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "Step 2: Updating country..." -ForegroundColor Yellow
    $updateBody = @{
        name = $countryData.name
        currencyCode = $countryData.currencyCode
        currencySymbol = $countryData.currencySymbol
        phoneCode = $countryData.phoneCode
        timezone = $countryData.timezone
        defaultLanguage = $countryData.defaultLanguage
        paymentProviders = $countryData.paymentProviders
    } | ConvertTo-Json

    try {
        $result = Invoke-RestMethod -Uri "$BaseUrl/api/v1/admin/countries/$CountryCode" -Headers $headers -Method Put -Body $updateBody
        Write-Host "✓ Country updated successfully" -ForegroundColor Green
    } catch {
        Write-Host "✗ Failed to update country: $_" -ForegroundColor Red
    }
}
Write-Host ""

# Step 3: Add cities
Write-Host "Step 3: Adding cities..." -ForegroundColor Yellow
$citiesBody = @{
    cities = $countryData.cities
} | ConvertTo-Json -Depth 3

try {
    $result = Invoke-RestMethod -Uri "$BaseUrl/api/v1/admin/countries/$CountryCode/cities/bulk" -Headers $headers -Method Post -Body $citiesBody
    Write-Host "✓ Cities added successfully" -ForegroundColor Green
    Write-Host "  Created: $($result.created) cities" -ForegroundColor Cyan
    if ($result.errors) {
        Write-Host "  Warnings: $($result.errors -join ', ')" -ForegroundColor Yellow
    }
} catch {
    Write-Host "✗ Failed to add cities: $_" -ForegroundColor Red
}
Write-Host ""

# Step 4: Configure country settings
Write-Host "Step 4: Configuring country settings..." -ForegroundColor Yellow
$configBody = @{
    config = $countryData.config
} | ConvertTo-Json -Depth 3

try {
    $result = Invoke-RestMethod -Uri "$BaseUrl/api/v1/admin/countries/$CountryCode/config" -Headers $headers -Method Put -Body $configBody
    Write-Host "✓ Country configuration updated" -ForegroundColor Green
} catch {
    Write-Host "✗ Failed to update configuration: $_" -ForegroundColor Red
}
Write-Host ""

# Step 5: Check onboarding status
Write-Host "Step 5: Checking onboarding status..." -ForegroundColor Yellow
try {
    $status = Invoke-RestMethod -Uri "$BaseUrl/api/v1/admin/countries/$CountryCode/onboarding-status" -Headers $headers -Method Get
    Write-Host "✓ Onboarding status retrieved" -ForegroundColor Green
    Write-Host "  Progress: $($status.progress.completed)/$($status.progress.total) ($($status.progress.percentage)%)" -ForegroundColor Cyan
    Write-Host "  Ready: $($status.isReady)" -ForegroundColor $(if ($status.isReady) { "Green" } else { "Yellow" })
    
    if ($status.recommendations) {
        Write-Host "  Recommendations:" -ForegroundColor Yellow
        foreach ($rec in $status.recommendations) {
            Write-Host "    - $rec" -ForegroundColor Yellow
        }
    }
    
    Write-Host ""
    Write-Host "  Checklist:" -ForegroundColor Cyan
    foreach ($item in $status.checklist) {
        $icon = if ($item.completed) { "✓" } else { "✗" }
        $color = if ($item.completed) { "Green" } else { "Red" }
        Write-Host "    $icon $($item.step)" -ForegroundColor $color
    }
} catch {
    Write-Host "✗ Failed to get onboarding status: $_" -ForegroundColor Red
}
Write-Host ""

# Step 6: Test configuration
Write-Host "Step 6: Testing configuration..." -ForegroundColor Yellow
try {
    $testResult = Invoke-RestMethod -Uri "$BaseUrl/api/v1/admin/countries/$CountryCode/test" -Headers $headers -Method Post
    Write-Host "✓ Configuration tests completed" -ForegroundColor Green
    Write-Host "  Overall Status: $($testResult.overallStatus)" -ForegroundColor $(if ($testResult.overallStatus -eq "PASSED") { "Green" } else { "Yellow" })
    Write-Host "  Tests Passed: $($testResult.testsPassed)/$($testResult.testsRun)" -ForegroundColor Cyan
    
    Write-Host ""
    Write-Host "  Test Results:" -ForegroundColor Cyan
    foreach ($test in $testResult.tests) {
        $icon = if ($test.passed) { "✓" } else { "✗" }
        $color = if ($test.passed) { "Green" } else { "Yellow" }
        $optional = if ($test.optional) { " (optional)" } else { "" }
        Write-Host "    $icon $($test.test)$optional" -ForegroundColor $color
    }
} catch {
    Write-Host "✗ Failed to test configuration: $_" -ForegroundColor Red
}
Write-Host ""

# Step 7: Prompt for payment configuration
Write-Host "Step 7: Payment Configuration" -ForegroundColor Yellow
Write-Host "Payment configuration requires manual setup with credentials." -ForegroundColor Cyan
Write-Host ""
Write-Host "Payment Providers: $($countryData.paymentProviders -join ', ')" -ForegroundColor Cyan
Write-Host ""
Write-Host "To configure payment gateways, use:" -ForegroundColor White
Write-Host "POST $BaseUrl/api/v1/admin/countries/$CountryCode/app-config" -ForegroundColor Gray
Write-Host ""
Write-Host "Example body:" -ForegroundColor White
Write-Host @"
{
  "settings": {
    "Payment:Paystack:SecretKey": "sk_live_...",
    "Payment:Paystack:PublicKey": "pk_live_...",
    "Payment:Flutterwave:SecretKey": "FLWSECK-...",
    "Payment:Flutterwave:PublicKey": "FLWPUBK-..."
  }
}
"@ -ForegroundColor Gray
Write-Host ""

$configurePayment = Read-Host "Do you want to configure payment now? (y/n)"
if ($configurePayment -eq 'y') {
    Write-Host "Enter payment configuration (paste JSON and press Enter twice):" -ForegroundColor Yellow
    $paymentJson = @()
    do {
        $line = Read-Host
        if ($line) { $paymentJson += $line }
    } while ($line)
    
    $paymentBody = $paymentJson -join "`n"
    try {
        $result = Invoke-RestMethod -Uri "$BaseUrl/api/v1/admin/countries/$CountryCode/app-config" -Headers $headers -Method Post -Body $paymentBody
        Write-Host "✓ Payment configuration updated" -ForegroundColor Green
    } catch {
        Write-Host "✗ Failed to update payment configuration: $_" -ForegroundColor Red
    }
}
Write-Host ""

# Step 8: Final status check
Write-Host "Step 8: Final status check..." -ForegroundColor Yellow
try {
    $finalStatus = Invoke-RestMethod -Uri "$BaseUrl/api/v1/admin/countries/$CountryCode/onboarding-status" -Headers $headers -Method Get
    
    if ($finalStatus.isReady) {
        Write-Host "✓ Country is ready for activation!" -ForegroundColor Green
        Write-Host ""
        
        $activate = Read-Host "Do you want to activate $($countryData.name) now? (y/n)"
        if ($activate -eq 'y') {
            try {
                $result = Invoke-RestMethod -Uri "$BaseUrl/api/v1/admin/countries/$CountryCode/onboarding/complete" -Headers $headers -Method Post
                Write-Host "✓ $($countryData.name) is now ACTIVE!" -ForegroundColor Green
                Write-Host ""
                Write-Host "Test the country:" -ForegroundColor Cyan
                Write-Host "  GET $BaseUrl/api/v1/$CountryCode/cities" -ForegroundColor Gray
                Write-Host "  GET $BaseUrl/api/v1/$CountryCode/country/current" -ForegroundColor Gray
            } catch {
                Write-Host "✗ Failed to activate country: $_" -ForegroundColor Red
            }
        } else {
            Write-Host "Country not activated. You can activate later with:" -ForegroundColor Yellow
            Write-Host "POST $BaseUrl/api/v1/admin/countries/$CountryCode/onboarding/complete" -ForegroundColor Gray
        }
    } else {
        Write-Host "✗ Country is not ready for activation" -ForegroundColor Red
        Write-Host "Complete these steps first:" -ForegroundColor Yellow
        foreach ($rec in $finalStatus.recommendations) {
            Write-Host "  - $rec" -ForegroundColor Yellow
        }
    }
} catch {
    Write-Host "✗ Failed to get final status: $_" -ForegroundColor Red
}
Write-Host ""

Write-Host "=== Onboarding Complete ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Summary:" -ForegroundColor Yellow
Write-Host "- Country: $($countryData.name) ($CountryCode)" -ForegroundColor White
Write-Host "- Currency: $($countryData.currencyCode) ($($countryData.currencySymbol))" -ForegroundColor White
Write-Host "- Cities: $($countryData.cities.Count)" -ForegroundColor White
Write-Host "- Payment Providers: $($countryData.paymentProviders -join ', ')" -ForegroundColor White
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "1. Configure payment gateway credentials (if not done)" -ForegroundColor White
Write-Host "2. Test the country endpoints" -ForegroundColor White
Write-Host "3. Add vehicles for the country" -ForegroundColor White
Write-Host "4. Monitor stats: GET $BaseUrl/api/v1/admin/countries/$CountryCode/stats" -ForegroundColor White
