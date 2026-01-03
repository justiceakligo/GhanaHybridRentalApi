# Smoke tests: approve one pending vehicle, verify active list, create booking
# Allow overriding the target API via environment variable SMOKE_API for flexible testing
$api = $env:SMOKE_API
if (-not $api) { $api = 'http://4.149.192.12' }
$adminToken = (Get-Content admin_token.txt -Raw).Trim()
$adminHeaders = @{ Authorization = "Bearer $adminToken" }

Write-Host "1) Fetch pending vehicles (admin)" -ForegroundColor Cyan
try {
    $pending = Invoke-RestMethod -Uri "$api/api/v1/admin/vehicles/pending?page=1&pageSize=20" -Headers $adminHeaders -Method Get -ErrorAction Stop
    if ($pending.total -lt 1) { Write-Host "No pending vehicles found" -ForegroundColor Yellow; exit 1 }
    $v = $pending.data[0]
    Write-Host "Found pending vehicle: $($v.id) plate=$($v.plateNumber)" -ForegroundColor Green
} catch { Write-Host "Failed to list pending vehicles: $($_.Exception.Message)" -ForegroundColor Red; if ($_.Exception.Response) { $r=[System.IO.StreamReader]::new($_.Exception.Response.GetResponseStream()); Write-Host $r.ReadToEnd() }; exit 1 }

Write-Host "2) Activate vehicle" -ForegroundColor Cyan
try {
    $body = @{ Status = 'active' } | ConvertTo-Json
    Invoke-RestMethod -Uri "$api/api/v1/admin/vehicles/$($v.id)/status" -Method Put -Headers $adminHeaders -Body $body -ContentType 'application/json' -ErrorAction Stop
    Write-Host "Activated vehicle $($v.id)" -ForegroundColor Green
} catch { Write-Host "Failed to activate vehicle: $($_.Exception.Message)" -ForegroundColor Red; if ($_.Exception.Response) { $r=[System.IO.StreamReader]::new($_.Exception.Response.GetResponseStream()); Write-Host $r.ReadToEnd() }; exit 1 }

Write-Host "3) Verify vehicle appears in public active list" -ForegroundColor Cyan
try { Start-Sleep -Seconds 1; $list = Invoke-RestMethod -Uri "$api/api/v1/vehicles?status=active&page=1&pageSize=10" -Method Get -ErrorAction Stop; Write-Host "Active vehicles found: $($list.total)" -ForegroundColor Green } catch { Write-Host "Failed to list active vehicles: $($_.Exception.Message)" -ForegroundColor Red; exit 1 }

if ($list.total -lt 1) { Write-Host "No active vehicles found after activation" -ForegroundColor Red; exit 1 }
$vehicleId = $list.data[0].id
Write-Host "Using vehicle for booking: $vehicleId" -ForegroundColor Green

Write-Host "4) Login renter and create booking" -ForegroundColor Cyan
$rentLogin = @{ Phone = '+233540000100'; Password = 'Test12345' } | ConvertTo-Json
try { $rb = Invoke-RestMethod -Uri "$api/api/v1/auth/login" -Method Post -Body $rentLogin -ContentType 'application/json' -ErrorAction Stop; $rToken = $rb.token } catch { Write-Host "Renter login failed: $($_.Exception.Message)" -ForegroundColor Red; if ($_.Exception.Response) { $r=[System.IO.StreamReader]::new($_.Exception.Response.GetResponseStream()); Write-Host $r.ReadToEnd() }; exit 1 }

$pickup = (Get-Date).AddDays(2).ToString('s') + 'Z'
$return = (Get-Date).AddDays(4).ToString('s') + 'Z'
$booking = @{ VehicleId = $vehicleId; PickupDateTime = $pickup; ReturnDateTime = $return; WithDriver = $false; DriverId = $null; InsurancePlanId = $null; ProtectionPlanId = $null; PickupLocation = @{ address = 'Test pickup' }; ReturnLocation = @{ address = 'Test return' }; PaymentMethod = 'card' } | ConvertTo-Json -Depth 7
try {
    $bk = Invoke-RestMethod -Uri "$api/api/v1/bookings" -Method Post -Headers @{ Authorization = "Bearer $rToken" } -Body $booking -ContentType 'application/json' -ErrorAction Stop
    Write-Host "Booking created: $($bk.id)" -ForegroundColor Green
    # Accept agreement
    $accept = @{ acceptedNoSmoking = $true; acceptedFinesAndTickets = $true; acceptedAccidentProcedure = $true } | ConvertTo-Json
    Invoke-RestMethod -Uri "$api/api/v1/bookings/$($bk.id)/rental-agreement/accept" -Method Post -Headers @{ Authorization = "Bearer $rToken" } -Body $accept -ContentType 'application/json'
    Write-Host "Accepted rental agreement" -ForegroundColor Green
    # Create inspection links
    $ins = Invoke-RestMethod -Uri "$api/api/v1/bookings/$($bk.id)/inspection-links" -Method Post -Headers @{ Authorization = "Bearer $rToken" } -ContentType 'application/json'
    Write-Host "Inspection links created: pickup=$($ins.pickupLink)" -ForegroundColor Green
} catch { Write-Host "Booking flow failed: $($_.Exception.Message)" -ForegroundColor Red; if ($_.Exception.Response) { $r=[System.IO.StreamReader]::new($_.Exception.Response.GetResponseStream()); Write-Host $r.ReadToEnd() }; exit 1 }

Write-Host "SMOKE TESTS PASSED" -ForegroundColor Green
