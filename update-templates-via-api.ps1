########################################
# Update Email Templates via API
########################################
# Uses the POST /api/v1/admin/email-templates endpoint
# to update booking_confirmation_customer and booking_confirmed templates
########################################

$ErrorActionPreference = "Stop"
$API_URL = "http://48.200.18.172"

Write-Host "========================================" -ForegroundColor Green
Write-Host "Email Template Update via API" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""

# Load admin token
if (!(Test-Path "admin_token.txt")) {
    Write-Host "ERROR: admin_token.txt not found" -ForegroundColor Red
    Write-Host "Please login first to get the token" -ForegroundColor Yellow
    exit 1
}

$token = Get-Content "admin_token.txt" -Raw
Write-Host "Admin token loaded" -ForegroundColor Green
Write-Host ""

# Read the HTML templates from the fixed SQL file
Write-Host "[1/3] Reading email templates from update-email-breakdown-fixed.sql..." -ForegroundColor Cyan
$sqlContent = Get-Content "update-email-breakdown-fixed.sql" -Raw

# Extract booking_confirmation_customer template
$template1Start = $sqlContent.IndexOf("SET `"BodyTemplate`" = '") + 23
$template1End = $sqlContent.IndexOf("WHERE `"TemplateName`" = 'booking_reserved'")
$template1Html = $sqlContent.Substring($template1Start, $template1End - $template1Start).Trim().TrimEnd("'")

# Extract booking_confirmed template
$template2Marker = "-- Update booking_confirmed template"
$template2Start = $sqlContent.IndexOf($template2Marker)
$template2BodyStart = $sqlContent.IndexOf("SET `"BodyTemplate`" = '", $template2Start) + 23
$template2End = $sqlContent.IndexOf("WHERE `"TemplateName`" = 'booking_confirmed'")
$template2Html = $sqlContent.Substring($template2BodyStart, $template2End - $template2BodyStart).Trim().TrimEnd("'")

Write-Host "  Template 1 size: $($template1Html.Length) characters" -ForegroundColor Yellow
Write-Host "  Template 2 size: $($template2Html.Length) characters" -ForegroundColor Yellow
Write-Host ""

# Prepare headers
$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

# Update Template 1: booking_confirmation_customer
Write-Host "[2/3] Updating booking_confirmation_customer template..." -ForegroundColor Cyan
$template1Body = @{
    templateName = "booking_confirmation_customer"
    subject = "Booking Reserved - {{booking_reference}}"
    bodyTemplate = $template1Html
    description = "Email sent when a booking is reserved (before payment)"
    category = "booking"
    isActive = $true
    isHtml = $true
}

try {
    $jsonBody1 = $template1Body | ConvertTo-Json -Depth 10 -Compress
    $response1 = Invoke-RestMethod -Uri "$API_URL/api/v1/admin/email-templates" -Method POST -Headers $headers -Body $jsonBody1 -ContentType "application/json; charset=utf-8"
    Write-Host "  SUCCESS: booking_confirmation_customer updated" -ForegroundColor Green
} catch {
    Write-Host "  ERROR updating booking_confirmation_customer:" -ForegroundColor Red
    Write-Host "  $($_.Exception.Message)" -ForegroundColor Yellow
    if ($_.ErrorDetails.Message) {
        Write-Host "  Details: $($_.ErrorDetails.Message)" -ForegroundColor Yellow
    }
    exit 1
}

Write-Host ""

# Update Template 2: booking_confirmed
Write-Host "[3/3] Updating booking_confirmed template..." -ForegroundColor Cyan
$template2Body = @{
    templateName = "booking_confirmed"
    subject = "Payment Confirmed - {{booking_reference}}"
    bodyTemplate = $template2Html
    description = "Email sent when booking payment is confirmed"
    category = "booking"
    isActive = $true
    isHtml = $true
}

try {
    $jsonBody2 = $template2Body | ConvertTo-Json -Depth 10 -Compress
    $response2 = Invoke-RestMethod -Uri "$API_URL/api/v1/admin/email-templates" -Method POST -Headers $headers -Body $jsonBody2 -ContentType "application/json; charset=utf-8"
    Write-Host "  SUCCESS: booking_confirmed updated" -ForegroundColor Green
} catch {
    Write-Host "  ERROR updating booking_confirmed:" -ForegroundColor Red
    Write-Host "  $($_.Exception.Message)" -ForegroundColor Yellow
    if ($_.ErrorDetails.Message) {
        Write-Host "  Details: $($_.ErrorDetails.Message)" -ForegroundColor Yellow
    }
    exit 1
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "Email Templates Updated Successfully!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Updated templates:" -ForegroundColor Yellow
Write-Host "  - booking_confirmation_customer" -ForegroundColor White
Write-Host "  - booking_confirmed" -ForegroundColor White
Write-Host ""
Write-Host "Both templates now include:" -ForegroundColor Yellow
Write-Host "  - Complete pricing breakdown" -ForegroundColor White
Write-Host "  - Protection Plan display" -ForegroundColor White
Write-Host "  - Platform Fee (15% of rental + driver)" -ForegroundColor White
Write-Host "  - Security Deposit (refundable)" -ForegroundColor White
Write-Host "  - Promo Discount (conditional)" -ForegroundColor White
Write-Host ""
Write-Host "Next: Create a test booking to verify the new email format!" -ForegroundColor Cyan
Write-Host ""
