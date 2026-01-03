# ⚠️ READ THIS FIRST - Windows Docker Disk Space

## Why You Only See 300MB Freed

Docker on Windows uses **WSL2** which stores all data in a **virtual hard disk file** (`.vhdx`).

When you delete Docker images:
- ✅ Space is freed **inside** the virtual disk
- ❌ The `.vhdx` file **DOES NOT shrink automatically**  
- ❌ Your C drive still shows the old size

**You cleaned ~17GB inside Docker, but the .vhdx file is still the same size!**

---

## 🔧 THE FIX - Get Your Disk Space Back

### **STEP 1: Clean Docker Images** (Already Done ✅)
```powershell
.\cleanup-disk-space.ps1
```

### **STEP 2: Compact WSL2 Disk** ⚡ **DO THIS NOW!**

**Right-click PowerShell → "Run as Administrator"**

Then run:
```powershell
cd "C:\Users\Justice\Documents\Fafa\Personal\Projects\GhanaHybridRentalApi"
.\compact-docker-disk.ps1
```

**What will happen:**
1. Script shuts down Docker Desktop
2. Shuts down WSL
3. Finds the virtual disk file (usually 20-50GB)
4. Compacts it to free actual C drive space
5. Shows you how much space was reclaimed (should be ~17GB)

**Time:** Takes 2-5 minutes

---

## 📊 Expected Results

**Before compaction:**
- Docker images deleted: ✅
- C drive space freed: ❌ Only 300MB
- Virtual disk size: Still 30-40GB

**After compaction:**
- Docker images deleted: ✅
- C drive space freed: ✅ ~17GB
- Virtual disk size: Reduced to ~15-20GB

---

## 🎯 Going Forward

**After EVERY deployment:**

```powershell
# 1. Clean images (normal PowerShell)
.\cleanup-disk-space.ps1

# 2. Free disk space (PowerShell as Admin - do this weekly)
.\compact-docker-disk.ps1
```

**Weekly schedule:**
- Sunday: Run both scripts as Admin
- Daily: Just use cleanup-disk-space.ps1

---

## ❓ Troubleshooting

**"Access Denied" error:**
- You're not running as Administrator
- Right-click PowerShell → "Run as Administrator"

**"Cannot find virtual disk":**
- Make sure Docker Desktop is installed
- The script will search and find it automatically

**Docker won't start after compaction:**
- This is normal - just start Docker Desktop
- Wait 30 seconds for it to fully start
- Run: `docker images` to verify everything is there

---

## 🔴 TL;DR

```powershell
# Run this NOW as Administrator to get your 17GB back:
.\compact-docker-disk.ps1
```

**You MUST do this on Windows or you'll never see disk space freed!**
