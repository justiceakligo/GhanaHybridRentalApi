# ========================================
# Disk Space Cleanup Script
# For GhanaHybridRentalApi Project
# ========================================
# Run this script periodically to free up disk space consumed by:
# - Old Docker images
# - Build artifacts (bin/obj)
# - Temporary files
# ========================================

Write-Host "🧹 Starting Disk Space Cleanup..." -ForegroundColor Cyan
Write-Host "="*60

# 1. Clean Build Artifacts
Write-Host "`n📦 Cleaning build artifacts..." -ForegroundColor Yellow
if (Test-Path ".\bin") {
    Remove-Item -Path ".\bin" -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "  ✅ Removed bin folder" -ForegroundColor Green
}
if (Test-Path ".\obj") {
    Remove-Item -Path ".\obj" -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "  ✅ Removed obj folder" -ForegroundColor Green
}
if (Test-Path ".\publish") {
    Remove-Item -Path ".\publish" -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "  ✅ Removed publish folder" -ForegroundColor Green
}

# 2. Clean Temporary Log Files
Write-Host "`n📝 Cleaning temporary log files..." -ForegroundColor Yellow
$logFiles = @(
    ".\logs.txt",
    ".\acilogs.txt", 
    ".\aci_logs.txt",
    ".\build_log.txt",
    ".\probe.txt",
    ".\upload_result.txt",
    ".\details.txt"
)
foreach ($log in $logFiles) {
    if (Test-Path $log) {
        Remove-Item -Path $log -Force -ErrorAction SilentlyContinue
        Write-Host "  ✅ Removed $(Split-Path $log -Leaf)" -ForegroundColor Green
    }
}

# 3. Remove Dangling Docker Images
Write-Host "`n🐳 Cleaning Docker dangling images..." -ForegroundColor Yellow
docker image prune -f | Out-Null
Write-Host "  ✅ Removed dangling Docker images" -ForegroundColor Green

# 4. Keep Only Last 3 Versions of Local Images
Write-Host "`n🗂️  Keeping only last 3 versions of ghanarentalapi..." -ForegroundColor Yellow

# Get all local ghanarentalapi images sorted by creation time
$allImages = docker images ghanarentalapi --format "{{.Tag}};{{.CreatedAt}}" | 
    Where-Object { $_ -notlike "latest*" } |
    ForEach-Object {
        $parts = $_ -split ';'
        [PSCustomObject]@{
            Tag = $parts[0]
            Created = $parts[1]
        }
    } |
    Sort-Object Created -Descending

# Keep only the 3 most recent
$imagesToKeep = $allImages | Select-Object -First 3
$imagesToRemove = $allImages | Select-Object -Skip 3

foreach ($image in $imagesToRemove) {
    if ($image.Tag -ne "<none>") {
        Write-Host "  Removing: ghanarentalapi:$($image.Tag)" -ForegroundColor DarkGray
        docker rmi "ghanarentalapi:$($image.Tag)" -f 2>$null | Out-Null
    }
}

if ($imagesToRemove.Count -gt 0) {
    Write-Host "  ✅ Removed $($imagesToRemove.Count) old image(s)" -ForegroundColor Green
} else {
    Write-Host "  ℹ️  Only 3 or fewer versions exist - nothing to remove" -ForegroundColor Cyan
}

# 5. Aggressive Docker Cleanup (Optional - Uncomment if needed)
# This removes ALL unused Docker data including stopped containers, networks, etc.
# Write-Host "`n⚠️  Running aggressive Docker cleanup..." -ForegroundColor Yellow
# docker system prune -a -f --volumes | Out-Null
# Write-Host "  ✅ Aggressive cleanup complete" -ForegroundColor Green

# 6. Show Summary
Write-Host "`n"
Write-Host "="*60
Write-Host "📊 CLEANUP SUMMARY" -ForegroundColor Cyan
Write-Host "="*60

Write-Host "`n🐳 Remaining Local Images:" -ForegroundColor Yellow
docker images ghanarentalapi --format "table {{.Tag}}\t{{.Size}}\t{{.CreatedAt}}"

Write-Host "`n💾 Docker Disk Usage:" -ForegroundColor Yellow
docker system df

Write-Host "`n✨ CLEANUP COMPLETE!" -ForegroundColor Green
Write-Host "💡 Tip: Run this script after each deployment to keep disk usage low" -ForegroundColor Cyan
