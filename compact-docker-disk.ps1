# ========================================
# WSL2 Docker Disk Compaction Script
# MUST RUN AS ADMINISTRATOR
# ========================================
# This script compacts the Docker WSL2 virtual disk
# to actually free up space on your C drive
# ========================================

Write-Host "`n🔧 DOCKER WSL2 DISK COMPACTION" -ForegroundColor Cyan
Write-Host "="*60

# Check if running as administrator
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "`n❌ ERROR: This script must run as Administrator!" -ForegroundColor Red
    Write-Host "`nTo run as admin:" -ForegroundColor Yellow
    Write-Host "  1. Right-click PowerShell" -ForegroundColor Cyan
    Write-Host "  2. Select 'Run as Administrator'" -ForegroundColor Cyan
    Write-Host "  3. Navigate to this directory" -ForegroundColor Cyan
    Write-Host "  4. Run: .\compact-docker-disk.ps1`n" -ForegroundColor Cyan
    exit 1
}

Write-Host "`n✅ Running as Administrator" -ForegroundColor Green

# Step 1: Shutdown Docker and WSL
Write-Host "`nStep 1: Shutting down Docker Desktop and WSL..." -ForegroundColor Yellow
Write-Host "  Please close Docker Desktop if it's running..." -ForegroundColor Cyan

# Gracefully stop Docker
Stop-Process -Name "Docker Desktop" -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2

# Shutdown WSL
Write-Host "  Shutting down WSL..." -ForegroundColor Cyan
wsl --shutdown
Start-Sleep -Seconds 5

Write-Host "  ✅ Docker and WSL shut down" -ForegroundColor Green

# Step 2: Find the Docker WSL2 virtual disk
Write-Host "`nStep 2: Locating Docker WSL2 virtual disk..." -ForegroundColor Yellow

$possiblePaths = @(
    "$env:LOCALAPPDATA\Docker\wsl\data\ext4.vhdx",
    "$env:LOCALAPPDATA\Docker\wsl\distro\ext4.vhdx",
    "$env:USERPROFILE\AppData\Local\Docker\wsl\data\ext4.vhdx",
    "$env:LOCALAPPDATA\Packages\CanonicalGroupLimited.UbuntuonWindows_79rhkp1fndgsc\LocalState\ext4.vhdx"
)

$vhdxPath = $null
foreach ($path in $possiblePaths) {
    if (Test-Path $path) {
        $vhdxPath = $path
        Write-Host "  ✅ Found: $vhdxPath" -ForegroundColor Green
        break
    }
}

if (-not $vhdxPath) {
    Write-Host "  ❌ Could not find Docker virtual disk in default locations" -ForegroundColor Red
    Write-Host "`nSearching entire AppData folder (this may take a moment)..." -ForegroundColor Yellow
    $found = Get-ChildItem -Path "$env:LOCALAPPDATA" -Filter "ext4.vhdx" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($found) {
        $vhdxPath = $found.FullName
        Write-Host "  ✅ Found: $vhdxPath" -ForegroundColor Green
    } else {
        Write-Host "`n❌ ERROR: Could not find Docker WSL2 virtual disk" -ForegroundColor Red
        Write-Host "Make sure Docker Desktop is installed and has been run at least once.`n" -ForegroundColor Yellow
        exit 1
    }
}

# Step 3: Check current size
$sizeBefore = (Get-Item $vhdxPath).Length / 1GB
Write-Host "`nStep 3: Current disk information:" -ForegroundColor Yellow
Write-Host "  Path: $vhdxPath" -ForegroundColor Cyan
Write-Host "  Size: $([math]::Round($sizeBefore, 2)) GB" -ForegroundColor Cyan

# Step 4: Create diskpart script
Write-Host "`nStep 4: Creating diskpart script..." -ForegroundColor Yellow
$diskpartScriptPath = Join-Path $env:TEMP "compact-docker.txt"
$diskpartScript = @"
select vdisk file="$vhdxPath"
attach vdisk readonly
compact vdisk
detach vdisk
exit
"@

$diskpartScript | Out-File -FilePath $diskpartScriptPath -Encoding ASCII
Write-Host "  ✅ Script created" -ForegroundColor Green

# Step 5: Run diskpart to compact
Write-Host "`nStep 5: Compacting virtual disk..." -ForegroundColor Yellow
Write-Host "  This may take 2-5 minutes depending on disk size..." -ForegroundColor Cyan
Write-Host "  Please be patient...`n" -ForegroundColor Cyan

diskpart /s $diskpartScriptPath

Start-Sleep -Seconds 2

# Step 6: Check new size
$sizeAfter = (Get-Item $vhdxPath).Length / 1GB
$saved = $sizeBefore - $sizeAfter

Write-Host "`n" + ("="*60)
Write-Host "✅ COMPACTION COMPLETE!" -ForegroundColor Green
Write-Host ("="*60)
Write-Host "`n📊 RESULTS:" -ForegroundColor Cyan
Write-Host "  Size before: $([math]::Round($sizeBefore, 2)) GB" -ForegroundColor Yellow
Write-Host "  Size after:  $([math]::Round($sizeAfter, 2)) GB" -ForegroundColor Yellow
Write-Host "  Space saved: $([math]::Round($saved, 2)) GB" -ForegroundColor Green

if ($saved -gt 0.1) {
    Write-Host "`n🎉 Successfully reclaimed $([math]::Round($saved, 2)) GB on your C drive!" -ForegroundColor Green
} else {
    Write-Host "`n⚠️  Minimal space saved. The disk may already be compact." -ForegroundColor Yellow
}

# Cleanup
Remove-Item -Path $diskpartScriptPath -Force -ErrorAction SilentlyContinue

Write-Host "`n💡 NEXT STEPS:" -ForegroundColor Cyan
Write-Host "  1. Start Docker Desktop" -ForegroundColor Yellow
Write-Host "  2. Wait for Docker to fully start" -ForegroundColor Yellow
Write-Host "  3. Verify your images are still there: docker images" -ForegroundColor Yellow

Write-Host "`n📝 TIP: Run this script monthly after running cleanup-disk-space.ps1" -ForegroundColor Cyan
Write-Host ""
