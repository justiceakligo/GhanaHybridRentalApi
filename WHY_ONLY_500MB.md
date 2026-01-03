# Why You're Only Getting 500MB Back

## The Reality

Most of the Docker images we "deleted" were actually **sharing layers** with other images. When Docker images share base layers (like the .NET runtime), deleting one image doesn't free much space if other images use the same layers.

## Check What's Really There

1. **Start Docker Desktop** (if not running)

2. **Run these commands:**

```powershell
# See ALL images including hidden ones
docker images -a

# See actual disk usage
docker system df -v

# Check build cache
docker builder du
```

## The REAL Cleanup (Gets You Maximum Space)

```powershell
# 1. Start Docker Desktop first
# Wait until it's fully started

# 2. Remove EVERYTHING Docker (run this exactly):
docker system prune -a --volumes -f

# 3. Remove build cache:
docker builder prune -a -f

# 4. Now compact the WSL disk (as Admin):
wsl --shutdown
# Wait 10 seconds, then run the compact script
```

## Alternative: Complete Reset (Nuclear Option)

If you want to start completely fresh:

### Option 1: WSL Reset (Fastest)
```powershell
# In PowerShell as Admin:
wsl --shutdown
wsl --unregister docker-desktop-data
wsl --unregister docker-desktop

# Restart Docker Desktop - it will recreate everything fresh
```

### Option 2: Docker Desktop Reset
1. Open Docker Desktop
2. Click Settings (gear icon)
3. Go to "Troubleshoot"
4. Click "Clean / Purge data"
5. Select "Reset to factory defaults"
6. This will delete EVERYTHING and give you a fresh start

### Option 3: Manual .vhdx Delete (Most Effective)
```powershell
# 1. Close Docker Desktop completely
# 2. In PowerShell as Admin:
wsl --shutdown

# 3. Delete the virtual disks:
Remove-Item "$env:LOCALAPPDATA\Docker\wsl\data\ext4.vhdx" -Force
Remove-Item "$env:LOCALAPPDATA\Docker\wsl\distro\ext4.vhdx" -Force -ErrorAction SilentlyContinue

# 4. Start Docker Desktop - it creates a fresh small disk
```

## Why Only 500MB?

Possible reasons:
1. **Shared layers**: Images share base layers (most likely)
2. **Already compact**: The disk was already mostly compacted
3. **Build cache**: Still have lots of build cache
4. **Volumes**: Have Docker volumes taking space
5. **Multiple WSL distros**: Ubuntu, Debian, etc. each have their own .vhdx

## Quick Diagnosis

Run this to see what's really using space:

```powershell
# 1. Check Docker usage
docker system df -v

# 2. Find all .vhdx files
Get-ChildItem -Path "$env:LOCALAPPDATA" -Filter "*.vhdx" -Recurse -ErrorAction SilentlyContinue | 
    Select-Object FullName, @{Name="SizeGB";Expression={[math]::Round($_.Length / 1GB, 2)}}

# 3. Check WSL distributions
wsl --list --verbose
```

## Recommended Action

**If you want maximum disk space back:**

```powershell
# DO THIS (as Administrator):

# 1. Close Docker Desktop
Stop-Process -Name "Docker Desktop" -Force -ErrorAction SilentlyContinue

# 2. Shutdown WSL
wsl --shutdown

# 3. Delete Docker's virtual disks (this is safe, Docker will recreate)
Remove-Item "$env:LOCALAPPDATA\Docker\wsl\data\ext4.vhdx" -Force
Remove-Item "$env:LOCALAPPDATA\Docker\wsl\distro\ext4.vhdx" -Force -ErrorAction SilentlyContinue

# 4. Start Docker Desktop
# It will create fresh, small disks

# 5. Rebuild only what you need
docker pull postgres:16  # If you need it
cd "C:\Users\Justice\Documents\Fafa\Personal\Projects\GhanaHybridRentalApi"
docker build -t ghanarentalapi:latest .
```

This will give you a fresh start and maximum disk space back.
