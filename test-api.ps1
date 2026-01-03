# API Testing Script for v1.194
# Base URL
$baseUrl = "http://4.149.69.32"
$fqdn = "http://ghanarentalapi.westus2.azurecontainer.io"

Write-Host "`n=== Testing v1.194 Deployment ===`n" -ForegroundColor Cyan

# Test 1: Health Check
Write-Host "1. Testing Health Endpoint..." -ForegroundColor Yellow
try {
    $health = Invoke-RestMethod -Uri "$baseUrl/health" -Method Get -TimeoutSec 5
    Write-Host "   ✓ Health check passed" -ForegroundColor Green
    Write-Host "   Response: $health" -ForegroundColor Gray
} catch {
    Write-Host "   ✗ Health check failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "   Trying FQDN..." -ForegroundColor Yellow
    try {
        $health = Invoke-RestMethod -Uri "$fqdn/health" -Method Get -TimeoutSec 5
        Write-Host "   ✓ Health check passed (FQDN)" -ForegroundColor Green
        $baseUrl = $fqdn
    } catch {
        Write-Host "   ✗ FQDN also failed" -ForegroundColor Red
    }
}

# Test 2: Swagger UI
Write-Host "`n2. Testing Swagger Endpoint..." -ForegroundColor Yellow
try {
    $swagger = Invoke-WebRequest -Uri "$baseUrl/swagger" -Method Get -TimeoutSec 5 -UseBasicParsing
    Write-Host "   ✓ Swagger UI accessible (Status: $($swagger.StatusCode))" -ForegroundColor Green
} catch {
    Write-Host "   ✗ Swagger not accessible: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 3: Admin Documents Endpoint (requires auth)
Write-Host "`n3. Testing Documents Endpoint (requires admin token)..." -ForegroundColor Yellow
$token = Get-Content -Path "admin_token.txt" -ErrorAction SilentlyContinue
if ($token) {
    try {
        $headers = @{ "Authorization" = "Bearer $token" }
        $docs = Invoke-RestMethod -Uri "$baseUrl/api/v1/admin/documents" -Method Get -Headers $headers -TimeoutSec 5
        Write-Host "   ✓ Documents endpoint accessible" -ForegroundColor Green
        Write-Host "   Total Documents: $($docs.total)" -ForegroundColor Gray
    } catch {
        Write-Host "   ✗ Documents endpoint failed: $($_.Exception.Message)" -ForegroundColor Red
    }
} else {
    Write-Host "   ⚠ No admin token found - skipping authenticated tests" -ForegroundColor Yellow
}

# Test 4: Admin Renters Endpoint
Write-Host "`n4. Testing Renters Endpoint (requires admin token)..." -ForegroundColor Yellow
if ($token) {
    try {
        $headers = @{ "Authorization" = "Bearer $token" }
        $renters = Invoke-RestMethod -Uri "$baseUrl/api/v1/admin/renters?page=1&pageSize=10" -Method Get -Headers $headers -TimeoutSec 5
        Write-Host "   ✓ Renters endpoint accessible" -ForegroundColor Green
        Write-Host "   Total Renters: $($renters.total)" -ForegroundColor Gray
    } catch {
        Write-Host "   ✗ Renters endpoint failed: $($_.Exception.Message)" -ForegroundColor Red
    }
} else {
    Write-Host "   ⚠ No admin token - skipping" -ForegroundColor Yellow
}

# Test 5: Scheduled Payouts Endpoint
Write-Host "`n5. Testing Scheduled Payouts Endpoint (requires admin token)..." -ForegroundColor Yellow
if ($token) {
    try {
        $headers = @{ "Authorization" = "Bearer $token" }
        $payouts = Invoke-RestMethod -Uri "$baseUrl/api/v1/admin/payouts/scheduled/due" -Method Get -Headers $headers -TimeoutSec 5
        Write-Host "   ✓ Scheduled payouts endpoint accessible" -ForegroundColor Green
        Write-Host "   Total Due: $($payouts.totalDue), Amount: GHS $($payouts.totalAmount)" -ForegroundColor Gray
    } catch {
        Write-Host "   ✗ Scheduled payouts failed: $($_.Exception.Message)" -ForegroundColor Red
    }
} else {
    Write-Host "   ⚠ No admin token - skipping" -ForegroundColor Yellow
}

Write-Host "`n=== Test Summary ===`n" -ForegroundColor Cyan
Write-Host "API Base URL: $baseUrl" -ForegroundColor White
Write-Host "Container IP: 4.149.69.32" -ForegroundColor White
Write-Host "Container FQDN: ghanarentalapi.westus2.azurecontainer.io" -ForegroundColor White
Write-Host ""
Write-Host "To get admin token, run:" -ForegroundColor Gray
Write-Host '  POST /api/v1/auth/login with admin credentials' -ForegroundColor Gray
Write-Host ""
