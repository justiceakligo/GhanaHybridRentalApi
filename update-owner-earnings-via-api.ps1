# Update owner earnings email template via API

Write-Host "Loading admin token..." -ForegroundColor Cyan
$token = Get-Content -Path "admin_token.txt" -Raw
$token = $token.Trim()

if ([string]::IsNullOrWhiteSpace($token)) {
    Write-Host "ERROR: Admin token not found in admin_token.txt" -ForegroundColor Red
    exit 1
}

Write-Host "Admin token loaded" -ForegroundColor Green

# Read the SQL file to extract the template
$sqlContent = Get-Content -Path "update-owner-earnings-template.sql" -Raw

# Extract the HTML template from between the quotes
$startMarker = 'SET "BodyTemplate" = '''
$endMarker = ''',
    "UpdatedAt"'

$startIndex = $sqlContent.IndexOf($startMarker)
if ($startIndex -ge 0) {
    $startIndex = $startIndex + $startMarker.Length
    $endIndex = $sqlContent.IndexOf($endMarker, $startIndex)
    
    if ($endIndex -gt $startIndex) {
        $templateHtml = $sqlContent.Substring($startIndex, $endIndex - $startIndex)
        Write-Host "Template extracted successfully (Length: $($templateHtml.Length) characters)" -ForegroundColor Green
    } else {
        Write-Host "ERROR: Could not find end marker in SQL file" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "ERROR: Could not find start marker in SQL file" -ForegroundColor Red
    exit 1
}

# Prepare the API request
$apiUrl = "http://172.193.158.180/api/v1/admin/email-templates"
$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json; charset=utf-8"
}

$templateBody = @{
    templateName = "booking_completed_owner"
    subject = "Rental Completed"
    bodyTemplate = $templateHtml
    description = "Email sent to vehicle owner when a rental is completed - shows earnings breakdown with mileage and platform fee deduction"
    category = "booking"
    isActive = $true
    isHtml = $true
} | ConvertTo-Json -Depth 10 -Compress

try {
    Write-Host "`nUpdating booking_completed_owner template..." -ForegroundColor Yellow
    
    $response = Invoke-RestMethod -Uri $apiUrl -Method Post -Headers $headers -Body $templateBody -ContentType "application/json; charset=utf-8"
    
    Write-Host "SUCCESS: booking_completed_owner template updated!" -ForegroundColor Green
    Write-Host "Response: $($response.message)" -ForegroundColor Cyan
} catch {
    Write-Host "ERROR updating booking_completed_owner: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.ErrorDetails.Message) {
        Write-Host "Details: $($_.ErrorDetails.Message)" -ForegroundColor Red
    }
    exit 1
}

Write-Host "`n✅ Owner Earnings Email Template Updated Successfully!" -ForegroundColor Green
