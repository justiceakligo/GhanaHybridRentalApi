# Disk Space Management Guide

## 🚨 Problem
Docker builds and deployments consume significant disk space over time due to:
- **Old Docker images**: Each build creates a new ~246MB image
- **Build artifacts**: bin/obj folders from .NET builds (~150-200MB)
- **Build cache**: Docker layer cache accumulates over time (10-15GB+)
- **Dangling images**: Untagged images from failed/interrupted builds
- **⚠️ WSL2 Virtual Disk**: On Windows, Docker uses a virtual disk (`.vhdx`) that **doesn't automatically shrink** when you delete images!

---

## ⚡ CRITICAL: WSL2 Disk Compaction (Windows Only)

**WHY YOU ONLY SEE 300MB FREED:**

Docker on Windows uses WSL2, which stores all data in a virtual hard disk file. When you delete Docker images:
- ✅ Space is freed **inside** the virtual disk
- ❌ The `.vhdx` file **does NOT shrink automatically**
- ❌ Your C drive still shows the old size

**THE FIX - Run this after cleanup:**

```powershell
# MUST run as Administrator
.\compact-docker-disk.ps1
```

**What this does:**
1. Shuts down Docker and WSL
2. Finds the Docker virtual disk (usually 20-50GB)
3. Compacts it to reclaim actual C drive space
4. Gives you back the ~17GB we cleaned

**How to run as Administrator:**
1. Right-click PowerShell → "Run as Administrator"
2. Navigate to project folder
3. Run: `.\compact-docker-disk.ps1`

---

## ✅ Solution Implemented

### 1. **Cleanup Script Created**
Location: `cleanup-disk-space.ps1`

**What it does:**
- ✅ Removes bin/obj/publish folders
- ✅ Deletes temporary log files
- ✅ Removes dangling Docker images
- ✅ Keeps only last 3 versions of local images
- ✅ Shows disk usage summary
- ⚠️ **Does NOT** compact the WSL2 disk (see script #2)

**How to use:**
```powershell
# Step 1: Clean Docker images
.\cleanup-disk-space.ps1

# Step 2: Actually free C drive space (run as Admin)
.\compact-docker-disk.ps1
```

### 2. **WSL2 Disk Compaction Script** ⚡ CRITICAL
Location: `compact-docker-disk.ps1`

**What it does:**
- ✅ Shuts down Docker and WSL safely
- ✅ Finds Docker's virtual disk file
- ✅ Compacts it to reclaim C drive space
- ✅ Shows actual GB saved on C drive

**How to use:**
```powershell
# MUST run as Administrator!
# Right-click PowerShell → Run as Administrator
.\compact-docker-disk.ps1
```

**⚠️ IMPORTANT:** Without this step, you won't see actual disk space freed on Windows!

### 2. **Docker Cleanup Performed**
**Space Reclaimed: ~17.5 GB**

**Actions taken:**
- Removed 60+ old Docker images
- Cleaned build cache
- Removed dangling volumes
- Deleted temporary build artifacts
- Removed old deployment scripts
- Cleaned temporary SQL query files

---

## 📋 Best Practices to Prevent Disk Issues

### **After Each Deployment:**
```powershell
# Step 1: Clean Docker images and build artifacts
.\cleanup-disk-space.ps1

# Step 2: Compact WSL2 disk (run as Admin, once a week)
.\compact-docker-disk.ps1
```

### **Weekly Maintenance:**
```powershell
# Aggressive Docker cleanup (removes ALL unused data)
docker system prune -a -f --volumes

# Show disk usage
docker system df
```

### **Before Each Build:**
```powershell
# Clean .NET build artifacts
dotnet clean
Remove-Item -Path ".\bin", ".\obj" -Recurse -Force -ErrorAction SilentlyContinue
```

### **Remove Old Images Manually:**
```powershell
# List all images with sizes
docker images --format "table {{.Repository}}\t{{.Tag}}\t{{.Size}}\t{{.CreatedAt}}" | Sort-Object -Descending

# Remove specific old version
docker rmi ghanarentalapi:1.190 -f

# Remove all old versions except latest 3
docker images ghanarentalapi --format "{{.Tag}}" | 
    Select-Object -Skip 3 | 
    ForEach-Object { docker rmi "ghanarentalapi:$_" -f }
```

---

## 🔧 Automated Cleanup Options

### **Option 1: Add to Deployment Script**
Add this to the bottom of your deployment scripts:

```powershell
# At end of deploy-v1.XXX.ps1
Write-Host "`nCleaning up old images..." -ForegroundColor Yellow
.\cleanup-disk-space.ps1
```

### **Option 2: Scheduled Task (Windows)**
Create a Windows scheduled task to run cleanup weekly:

```powershell
# Run as Administrator
$action = New-ScheduledTaskAction -Execute "PowerShell.exe" -Argument "-File C:\Users\Justice\Documents\Fafa\Personal\Projects\GhanaHybridRentalApi\cleanup-disk-space.ps1"
$trigger = New-ScheduledTaskTrigger -Weekly -DaysOfWeek Sunday -At 2am
Register-ScheduledTask -Action $action -Trigger $trigger -TaskName "Docker Cleanup" -Description "Weekly Docker and build cleanup"
```

### **Option 3: Pre-Build Hook**
Add to `.vscode/tasks.json`:

```json
{
  "label": "Clean Before Build",
  "type": "shell",
  "command": "Remove-Item -Path './bin', './obj' -Recurse -Force -ErrorAction SilentlyContinue",
  "problemMatcher": []
}
```

---

## 📊 Disk Space Monitoring

### **Check Docker Usage:**
```powershell
# Summary
docker system df

# Detailed view
docker system df -v

# List images by size
docker images --format "table {{.Repository}}\t{{.Tag}}\t{{.Size}}" | Sort-Object
```

### **Check Build Artifacts:**
```powershell
# Total size of bin/obj folders
Get-ChildItem -Path ".\bin", ".\obj" -Recurse -ErrorAction SilentlyContinue | 
    Measure-Object -Property Length -Sum | 
    Select-Object @{Name="TotalMB";Expression={[math]::Round($_.Sum / 1MB, 2)}}
```

### **Check Project Directory Size:**
```powershell
Get-ChildItem -Path . -Recurse -File | 
    Measure-Object -Property Length -Sum | 
    Select-Object @{Name="TotalGB";Expression={[math]::Round($_.Sum / 1GB, 2)}}
```

---

## 🎯 Current State (After Cleanup)

### **Kept Items:**
✅ Last 3 local images: 1.199, 1.200, 1.201  
✅ Migration scripts: `migrations.sql`, `add-*.sql`  
✅ Recent deployment scripts: v1.195+  
✅ Documentation: All .md files  
✅ Essential configs: appsettings.json, Dockerfile  

### **Removed Items:**
❌ 60+ old Docker images (12+ GB)  
❌ Docker build cache (5+ GB)  
❌ Old deployment scripts (v1.180-v1.194)  
❌ Temporary log files  
❌ Temporary SQL query files  
❌ bin/obj folders (150 MB)  

### **Total Space Reclaimed: ~17.5 GB**

---

## 🚀 Going Forward

### **Every Deployment:**
1. Build new image: `docker build -t ghanarentalapi:1.XXX .`
2. Deploy to Azure
3. **Run cleanup**: `.\cleanup-disk-space.ps1`
4. Verify: `docker images ghanarentalapi`

### **Monthly Deep Clean:**
```powershell
# Stop all containers
docker stop $(docker ps -aq)

# Remove all stopped containers
docker container prune -f

# Remove all unused images, networks, volumes
docker system prune -a -f --volumes

# Clean .NET build artifacts
dotnet clean
Remove-Item -Path ".\bin", ".\obj", ".\publish" -Recurse -Force -ErrorAction SilentlyContinue
```

### **Emergency Disk Space Recovery:**
```powershell
# Nuclear option - removes EVERYTHING except images you're using
docker system prune -a -f --volumes
docker image prune -a -f

# Rebuild only what you need
docker pull postgres:16
docker build -t ghanarentalapi:latest .
```

---

## 📝 Files to Keep vs Delete

### **✅ Keep:**
- `appsettings.json`, `appsettings.Production.json`
- `Dockerfile`, `.dockerignore`
- `Program.cs`, all source code
- `migrations.sql`, `add-*.sql`, `create-*.sql`
- Latest 3 deployment scripts
- All `.md` documentation files
- `cleanup-disk-space.ps1`

### **❌ Safe to Delete:**
- `logs.txt`, `acilogs.txt`, any `.log` files
- `check-*.sql`, `count-*.sql`, `find-*.sql` (temp queries)
- Old deployment scripts (v1.180 and earlier)
- `bin/`, `obj/`, `publish/` folders
- `details.txt`, `probe.txt`, `upload_result.txt`

---

## 🔍 Troubleshooting

### **"No space left on device"**
```powershell
docker system prune -a -f --volumes
.\cleanup-disk-space.ps1
```

### **Build failing due to disk space**
```powershell
# Clean everything
dotnet clean
docker system prune -a -f
Remove-Item -Path ".\bin", ".\obj" -Recurse -Force

# Rebuild
dotnet build
```

### **Can't delete images - in use**
```powershell
# Stop all containers
docker stop $(docker ps -aq)

# Remove stopped containers
docker rm $(docker ps -aq)

# Now remove images
docker rmi <image-id> -f
```

---

## 💡 Tips

1. **Use .dockerignore**: Already configured to exclude unnecessary files from images
2. **Multi-stage builds**: Dockerfile already uses multi-stage build (reduces image size)
3. **Don't commit builds**: bin/obj already in .gitignore
4. **Regular cleanup**: Run cleanup script weekly
5. **Monitor space**: Check `docker system df` regularly

---

## 📞 Quick Reference

```powershell
# After each deployment: Run cleanup
.\cleanup-disk-space.ps1

# Weekly: Compact WSL2 disk (RUN AS ADMIN!)
.\compact-docker-disk.ps1

# Monthly: Deep clean
docker system prune -a -f --volumes
.\compact-docker-disk.ps1  # Run as Admin

# Emergency: Remove everything unused
docker system prune -a -f --volumes && dotnet clean
.\compact-docker-disk.ps1  # Run as Admin
```

---

## 🔴 CRITICAL REMINDER FOR WINDOWS USERS

**Docker cleanup alone DOES NOT free C drive space!**

You MUST run the compaction script to actually reclaim disk space:

```powershell
# After ANY Docker cleanup, always run (as Admin):
.\compact-docker-disk.ps1
```

**Why:**
- Docker uses a virtual disk file (ext4.vhdx)
- This file can grow to 50-100GB+
- It NEVER shrinks automatically
- You must manually compact it

**Remember:** Each Docker build creates a ~246MB image. Without cleanup + compaction
---

**Remember:** Each Docker build creates a ~246MB image. Without cleanup, you'll lose ~5-10GB per week during active development! 🚨
