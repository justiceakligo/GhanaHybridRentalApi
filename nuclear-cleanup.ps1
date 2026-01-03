# ========================================
# COMPLETE Docker Cleanup - Windows
# ========================================
# This removes EVERYTHING and gives you maximum disk space back
# ========================================

Write-Host "`n🔥 COMPLETE DOCKER CLEANUP" -ForegroundColor Red
Write-Host "="*60
Write-Host "This will remove ALL Docker data and free maximum disk space" -ForegroundColor Yellow
Write-Host "="*60

# Ask for confirmation
Write-Host "`n⚠️  WARNING: This will remove:" -ForegroundColor Yellow
Write-Host "  • All Docker images (you'll need to rebuild)" -ForegroundColor Cyan
Write-Host "  • All containers" -ForegroundColor Cyan
Write-Host "  • All volumes" -ForegroundColor Cyan
Write-Host "  • All build cache" -ForegroundColor Cyan
Write-Host "  • All networks" -ForegroundColor Cyan

$confirm = Read-Host "`nDo you want to continue? (yes/no)"
if ($confirm -ne "yes") {
    Write-Host "Cancelled." -ForegroundColor Yellow
    exit
}

# Step 1: Make sure Docker is running
Write-Host "`nStep 1: Starting Docker Desktop..." -ForegroundColor Yellow
Start-Process "C:\Program Files\Docker\Docker\Docker Desktop.exe" -ErrorAction SilentlyContinue
Write-Host "Waiting for Docker to start (30 seconds)..." -ForegroundColor Cyan
Start-Sleep -Seconds 30

# Test if Docker is running
$dockerRunning = $false
try {
    docker ps | Out-Null
    $dockerRunning = $true
    Write-Host "✅ Docker is running" -ForegroundColor Green
} catch {
    Write-Host "❌ Docker is not running. Please start Docker Desktop manually." -ForegroundColor Red
    Write-Host "Then run this script again." -ForegroundColor Yellow
    exit 1
}

# Step 2: Nuclear cleanup
Write-Host "`nStep 2: Removing ALL Docker data..." -ForegroundColor Yellow
docker system prune -a --volumes -f

Write-Host "`nStep 3: Removing build cache..." -ForegroundColor Yellow
docker builder prune -a -f

# Step 3: Show what's left
Write-Host "`n" + ("="*60)
Write-Host "📊 REMAINING DOCKER DATA:" -ForegroundColor Cyan
Write-Host ("="*60)
docker system df

# Step 4: Stop Docker and compact WSL disk
Write-Host "`nStep 4: Compacting WSL2 virtual disk..." -ForegroundColor Yellow
Write-Host "Shutting down Docker..." -ForegroundColor Cyan
Stop-Process -Name "Docker Desktop" -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 3

Write-Host "Shutting down WSL..." -ForegroundColor Cyan
wsl --shutdown
Start-Sleep -Seconds 5

# Find the virtual disk
$vhdxPath = $null
$possiblePaths = @(
    "$env:LOCALAPPDATA\Docker\wsl\data\ext4.vhdx",
    "$env:LOCALAPPDATA\Docker\wsl\distro\ext4.vhdx"
)

foreach ($path in $possiblePaths) {
    if (Test-Path $path) {
        $vhdxPath = $path
        break
    }
}

if ($vhdxPath) {
    $sizeBefore = (Get-Item $vhdxPath).Length / 1GB
    Write-Host "`nVirtual disk: $vhdxPath" -ForegroundColor Cyan
    Write-Host "Size before: $([math]::Round($sizeBefore, 2)) GB" -ForegroundColor Yellow
    
    # Optimize using Optimize-VHD (requires Hyper-V)
    Write-Host "`nOptimizing virtual disk..." -ForegroundColor Yellow
    
    $diskpartScript = @"
select vdisk file="$vhdxPath"
attach vdisk readonly
compact vdisk
detach vdisk
exit
"@
    
    $scriptPath = Join-Path $env:TEMP "compact.txt"
    $diskpartScript | Out-File -FilePath $scriptPath -Encoding ASCII
    diskpart /s $scriptPath
    Remove-Item $scriptPath -Force
    
    Start-Sleep -Seconds 3
    
    $sizeAfter = (Get-Item $vhdxPath).Length / 1GB
    $saved = $sizeBefore - $sizeAfter
    
    Write-Host "`n✅ COMPACTION COMPLETE!" -ForegroundColor Green
    Write-Host "Size before: $([math]::Round($sizeBefore, 2)) GB" -ForegroundColor Yellow
    Write-Host "Size after:  $([math]::Round($sizeAfter, 2)) GB" -ForegroundColor Yellow
    Write-Host "Space saved: $([math]::Round($saved, 2)) GB" -ForegroundColor Green
} else {
    Write-Host "Could not find Docker virtual disk" -ForegroundColor Red
}

Write-Host "`n" + ("="*60)
Write-Host "✅ CLEANUP COMPLETE!" -ForegroundColor Green
Write-Host ("="*60)
Write-Host "`nNext steps:" -ForegroundColor Cyan
Write-Host "1. Start Docker Desktop" -ForegroundColor Yellow
Write-Host "2. Rebuild your image: docker build -t ghanarentalapi:latest ." -ForegroundColor Yellow
Write-Host "3. Check space: docker system df" -ForegroundColor Yellow
Write-Host ""
