# ⚡ ZeroSystem: Sovereign Windows Native & Kernel Subsystem

[![ZeroPlatform Tier](https://img.shields.io/badge/ZeroPlatform-Tier%201%20(Compute%20%26%20System)-4f46e5.svg)](https://github.com/kzxl/ZeroPlatform)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Dependencies: 0](https://img.shields.io/badge/Dependencies-0%20External-brightgreen.svg)]()
[![Platform: Windows](https://img.shields.io/badge/Platform-Windows-lightgrey.svg)]()
[![Build & Test](https://github.com/kzxl/ZeroSystem/actions/workflows/ci.yml/badge.svg)](https://github.com/kzxl/ZeroSystem/actions/workflows/ci.yml)

**ZeroSystem** (`ZeroSystem.Core`) is the sovereign Windows Native, Kernel P/Invoke, DWM Desktop, and OS Internals Subsystem of the **ZeroPlatform** / **Zero Universe** ecosystem. It provides type-safe, resource-managed wrappers around low-level Win32, NTDLL, and Kernel APIs with **zero external dependencies** (100% pure C# BCL).

---

## 📦 Key Subsystem Capabilities

| Component | Description |
| :--- | :--- |
| **`NativeMethods`** | Centralized, hardened Win32 P/Invoke declarations for `Kernel32`, `User32`, `Advapi32`, `Ntdll`, `Shell32`, and `DwmApi`. |
| **`DosDeviceManager`** | High-level MS-DOS device mapping (`DefineDosDevice`). Mounts VSS Shadow Copies (`\\?\GLOBALROOT\Device\HarddiskVolumeShadowCopy{N}`) to unused drive letters (`Z:`) and cleanly unmounts them. |
| **`ProcessGuard`** | System privileges escalation (`SeBackupPrivilege`, `SeRestorePrivilege`, `SeProfileSingleProcessPrivilege`), safe PID detection, and foreground window querying. |
| **`MemoryKernel`** | Deep RAM optimization via undocumented `NtSetSystemInformation` (PurgeStandbyList = 4, SystemMemoryList = 80) and process working set trimming. |
| **`UnsafeFileStreamFactory`** | Opens permissive non-locking file streams (`FileShare.ReadWrite \| FileShare.Delete`) to inspect locked files without sharing violations. |
| **`DesktopWallpaperInjector`** | Interacts with `Progman` and DWM (`0x052C`) to spawn the behind-icon `WorkerW` canvas for Live Wallpapers and HUD overlays. |
| **`PowerAndDisplayWatcher`** | Real-time monitoring of AC/Battery power state and detection of active full-screen applications. |
| **`ShellRegistryHelper`** | Registers Windows Explorer context menu actions directly into `HKCU\Software\Classes` without requiring Administrator privileges. |

---

## 🚀 Quick Start

### NuGet Package Installation
```bash
dotnet add package ZeroSystem.Core
```

### Example 1: Mount VSS Shadow Copy
```csharp
using ZeroSystem;

if (DosDeviceManager.TryMountDevice(@"\\?\GLOBALROOT\Device\HarddiskVolumeShadowCopy1", out char driveLetter, out string err))
{
    Console.WriteLine($"VSS mounted at {driveLetter}:");
    // Access historical files via $"{driveLetter}:\\..."
    DosDeviceManager.TryUnmountDevice(driveLetter, null, out _);
}
```

### Example 2: Purge Standby List (RAM Optimization)
```csharp
using ZeroSystem;

if (ProcessGuard.EnablePrivilege(ProcessGuard.SeProfileSingleProcessPrivilege))
{
    bool success = MemoryKernel.PurgeStandbyList();
    Console.WriteLine($"Standby List Purged: {success}");
}
```

---

## 📄 License
Released under the permissive **MIT License**.
Architected and developed by **Phong Võ** (`kzxl`).
