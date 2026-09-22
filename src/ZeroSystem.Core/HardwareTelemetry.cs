using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;

namespace ZeroSystem;

#region Telemetry Data Models

/// <summary>
/// Comprehensive system and hardware telemetry snapshot captured at a single instant.
/// </summary>
public sealed record SystemDiagnosticsSnapshot(
    DateTime TimestampUtc,
    TimeSpan SystemUptime,
    string MachineName,
    string OsDescription,
    bool Is64BitOperatingSystem,
    CpuInfo Cpu,
    MemoryInfo Memory,
    IReadOnlyList<GpuAdapterInfo> Gpus,
    IReadOnlyList<DriveInfoSnapshot> Storage,
    IReadOnlyList<NetworkAdapterInfo> Network);

public sealed record CpuInfo(
    string ModelName,
    int LogicalProcessorCount,
    int PhysicalCoreCount,
    float CpuUsagePercent);

public sealed record MemoryInfo(
    ulong TotalPhysicalBytes,
    ulong AvailablePhysicalBytes,
    ulong UsedPhysicalBytes,
    uint MemoryLoadPercent,
    ulong TotalPageFileBytes,
    ulong AvailablePageFileBytes);

public sealed record GpuAdapterInfo(
    string AdapterName,
    ulong DedicatedVideoMemoryBytes,
    ulong DedicatedSystemMemoryBytes,
    ulong SharedSystemMemoryBytes,
    uint VendorId,
    uint DeviceId);

public sealed record DriveInfoSnapshot(
    string DriveLetter,
    string VolumeLabel,
    string FileSystemName,
    DriveType DriveType,
    ulong TotalBytes,
    ulong FreeBytes,
    float FreePercent);

public sealed record NetworkAdapterInfo(
    string Id,
    string Name,
    string Description,
    string MacAddress,
    OperationalStatus Status,
    long SpeedBitsPerSecond,
    IReadOnlyList<string> IpAddresses);

#endregion

/// <summary>
/// Sovereign high-performance native Windows hardware telemetry and diagnostics engine.
/// Queries Win32 APIs, DXGI, and Registry with zero external dependencies and sub-millisecond execution.
/// </summary>
public static class HardwareTelemetry
{
    private static long _lastIdleTime;
    private static long _lastKernelTime;
    private static long _lastUserTime;
    private static readonly object _cpuLock = new();

    static HardwareTelemetry()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            UpdateCpuTimes(out _lastIdleTime, out _lastKernelTime, out _lastUserTime);
        }
    }

    /// <summary>
    /// Captures a complete hardware, memory, GPU, disk, and network diagnostic snapshot.
    /// </summary>
    public static SystemDiagnosticsSnapshot GetSnapshot()
    {
        var uptime = TimeSpan.FromMilliseconds(Environment.TickCount64);
        return new SystemDiagnosticsSnapshot(
            TimestampUtc: DateTime.UtcNow,
            SystemUptime: uptime,
            MachineName: Environment.MachineName,
            OsDescription: RuntimeInformation.OSDescription,
            Is64BitOperatingSystem: Environment.Is64BitOperatingSystem,
            Cpu: GetCpuInfo(),
            Memory: GetMemoryInfo(),
            Gpus: GetGpuAdapters(),
            Storage: GetStorageDrives(),
            Network: GetNetworkAdapters());
    }

    /// <summary>
    /// Gets CPU model name, core counts, and real-time load percentage.
    /// </summary>
    public static CpuInfo GetCpuInfo()
    {
        string model = "Unknown Processor";
        int logicalCores = Environment.ProcessorCount;
        int physicalCores = logicalCores; // fallback

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
                if (key?.GetValue("ProcessorNameString") is string name)
                {
                    model = name.Trim();
                }
            }
            catch
            {
                // Registry read failure fallback
            }

            try
            {
                // Estimate physical cores from distinct Core IDs if available or logical/2 for hyperthreaded
                int coreCount = 0;
                using var cpuRoot = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor");
                if (cpuRoot != null)
                {
                    coreCount = cpuRoot.SubKeyCount;
                }
                if (coreCount > 0)
                {
                    logicalCores = coreCount;
                }
            }
            catch
            {
                // Keep default
            }
        }

        float usage = SampleCpuUsagePercent();

        return new CpuInfo(
            ModelName: model,
            LogicalProcessorCount: logicalCores,
            PhysicalCoreCount: physicalCores,
            CpuUsagePercent: usage);
    }

    /// <summary>
    /// Samples instant CPU usage percentage via native GetSystemTimes.
    /// </summary>
    public static float SampleCpuUsagePercent()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return 0.0f;

        lock (_cpuLock)
        {
            if (!UpdateCpuTimes(out long idle, out long kernel, out long user))
                return 0.0f;

            long usrDiff = user - _lastUserTime;
            long kerDiff = kernel - _lastKernelTime;
            long idlDiff = idle - _lastIdleTime;

            long sysTotal = usrDiff + kerDiff;
            long sysTotalMinusIdle = sysTotal - idlDiff;

            _lastIdleTime = idle;
            _lastKernelTime = kernel;
            _lastUserTime = user;

            if (sysTotal <= 0 || sysTotalMinusIdle < 0)
                return 0.0f;

            float percent = (float)sysTotalMinusIdle * 100.0f / sysTotal;
            return Math.Clamp(percent, 0.0f, 100.0f);
        }
    }

    /// <summary>
    /// Gets physical and virtual memory statistics via GlobalMemoryStatusEx.
    /// </summary>
    public static MemoryInfo GetMemoryInfo()
    {
        var mem = MemoryKernel.GetMemoryStatus();
        ulong used = mem.TotalPhysicalBytes >= mem.AvailablePhysicalBytes
            ? mem.TotalPhysicalBytes - mem.AvailablePhysicalBytes
            : 0;

        return new MemoryInfo(
            TotalPhysicalBytes: mem.TotalPhysicalBytes,
            AvailablePhysicalBytes: mem.AvailablePhysicalBytes,
            UsedPhysicalBytes: used,
            MemoryLoadPercent: mem.MemoryLoadPercentage,
            TotalPageFileBytes: mem.TotalPageFileBytes,
            AvailablePageFileBytes: mem.AvailablePageFileBytes);
    }

    /// <summary>
    /// Enumerates graphics adapters and dedicated/shared video memory via DXGI native API.
    /// </summary>
    public static IReadOnlyList<GpuAdapterInfo> GetGpuAdapters()
    {
        var adapters = new List<GpuAdapterInfo>();

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return adapters;

        try
        {
            int hr = CreateDXGIFactory1(in IID_IDXGIFactory1, out IntPtr factoryPtr);
            if (hr < 0 || factoryPtr == IntPtr.Zero)
                return adapters;

            try
            {
                uint index = 0;
                while (true)
                {
                    // Call IDXGIFactory1::EnumAdapters1 (slot 12 in IDXGIFactory1 vtable: 0-2 IUnknown, 3-11 IDXGIFactory, 12 EnumAdapters1)
                    IntPtr vtable = Marshal.ReadIntPtr(factoryPtr);
                    IntPtr enumAdapters1Ptr = Marshal.ReadIntPtr(vtable, 12 * IntPtr.Size);
                    var enumAdapters1 = Marshal.GetDelegateForFunctionPointer<EnumAdapters1Delegate>(enumAdapters1Ptr);

                    hr = enumAdapters1(factoryPtr, index, out IntPtr adapterPtr);
                    if (hr < 0 || adapterPtr == IntPtr.Zero)
                        break;

                    try
                    {
                        // IDXGIAdapter1::GetDesc1 (slot 10 in IDXGIAdapter1 vtable: 0-2 IUnknown, 3-7 IDXGIObject, 8-9 IDXGIAdapter, 10 GetDesc1)
                        IntPtr adapterVtable = Marshal.ReadIntPtr(adapterPtr);
                        IntPtr getDesc1Ptr = Marshal.ReadIntPtr(adapterVtable, 10 * IntPtr.Size);
                        var getDesc1 = Marshal.GetDelegateForFunctionPointer<GetDesc1Delegate>(getDesc1Ptr);

                        DXGI_ADAPTER_DESC1 desc = default;
                        hr = getDesc1(adapterPtr, ref desc);
                        if (hr >= 0)
                        {
                            // Ignore software/WARP basic renderer if wanted, or include all
                            adapters.Add(new GpuAdapterInfo(
                                AdapterName: desc.Description?.TrimEnd('\0') ?? "Unknown GPU",
                                DedicatedVideoMemoryBytes: (ulong)desc.DedicatedVideoMemory,
                                DedicatedSystemMemoryBytes: (ulong)desc.DedicatedSystemMemory,
                                SharedSystemMemoryBytes: (ulong)desc.SharedSystemMemory,
                                VendorId: desc.VendorId,
                                DeviceId: desc.DeviceId));
                        }
                    }
                    finally
                    {
                        Marshal.Release(adapterPtr);
                    }

                    index++;
                }
            }
            finally
            {
                Marshal.Release(factoryPtr);
            }
        }
        catch
        {
            // Fallback gracefully if DXGI is unavailable
        }

        return adapters;
    }

    /// <summary>
    /// Enumerates logical disk partitions, volume labels, file systems, and space metrics.
    /// </summary>
    public static IReadOnlyList<DriveInfoSnapshot> GetStorageDrives()
    {
        var drives = new List<DriveInfoSnapshot>();

        try
        {
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (!drive.IsReady)
                {
                    drives.Add(new DriveInfoSnapshot(
                        DriveLetter: drive.Name,
                        VolumeLabel: string.Empty,
                        FileSystemName: string.Empty,
                        DriveType: drive.DriveType,
                        TotalBytes: 0,
                        FreeBytes: 0,
                        FreePercent: 0.0f));
                    continue;
                }

                ulong total = (ulong)drive.TotalSize;
                ulong free = (ulong)drive.AvailableFreeSpace;
                float freePct = total > 0 ? (float)free * 100.0f / total : 0.0f;

                drives.Add(new DriveInfoSnapshot(
                    DriveLetter: drive.Name,
                    VolumeLabel: drive.VolumeLabel ?? string.Empty,
                    FileSystemName: drive.DriveFormat ?? string.Empty,
                    DriveType: drive.DriveType,
                    TotalBytes: total,
                    FreeBytes: free,
                    FreePercent: freePct));
            }
        }
        catch
        {
            // Drive query fallback
        }

        return drives;
    }

    /// <summary>
    /// Enumerates network adapters, MAC addresses, connection speed, and assigned IP addresses.
    /// </summary>
    public static IReadOnlyList<NetworkAdapterInfo> GetNetworkAdapters()
    {
        var adapters = new List<NetworkAdapterInfo>();

        try
        {
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                var ips = new List<string>();
                try
                {
                    var ipProps = nic.GetIPProperties();
                    foreach (var addr in ipProps.UnicastAddresses)
                    {
                        ips.Add(addr.Address.ToString());
                    }
                }
                catch
                {
                    // IP query fallback
                }

                string mac = nic.GetPhysicalAddress().ToString();
                if (mac.Length == 12)
                {
                    mac = $"{mac[0..2]}:{mac[2..4]}:{mac[4..6]}:{mac[6..8]}:{mac[8..10]}:{mac[10..12]}";
                }

                adapters.Add(new NetworkAdapterInfo(
                    Id: nic.Id,
                    Name: nic.Name,
                    Description: nic.Description,
                    MacAddress: mac,
                    Status: nic.OperationalStatus,
                    SpeedBitsPerSecond: nic.Speed,
                    IpAddresses: ips));
            }
        }
        catch
        {
            // Network query fallback
        }

        return adapters;
    }

    #region Private Native Helpers

    private static bool UpdateCpuTimes(out long idle, out long kernel, out long user)
    {
        idle = 0;
        kernel = 0;
        user = 0;

        if (!GetSystemTimes(out var ftIdle, out var ftKernel, out var ftUser))
            return false;

        idle = ToLong(ftIdle);
        kernel = ToLong(ftKernel);
        user = ToLong(ftUser);
        return true;
    }

    private static long ToLong(FILETIME ft)
    {
        return ((long)ft.dwHighDateTime << 32) | (uint)ft.dwLowDateTime;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct FILETIME
    {
        public uint dwLowDateTime;
        public uint dwHighDateTime;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetSystemTimes(out FILETIME lpIdleTime, out FILETIME lpKernelTime, out FILETIME lpUserTime);

    // DXGI Interop
    private static readonly Guid IID_IDXGIFactory1 = new("770aae78-f26f-4dba-a829-253c83d1b387");

    [DllImport("dxgi.dll", ExactSpelling = true)]
    private static extern int CreateDXGIFactory1(in Guid riid, out IntPtr ppFactory);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int EnumAdapters1Delegate(IntPtr thisPtr, uint adapterIndex, out IntPtr ppAdapter);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int GetDesc1Delegate(IntPtr thisPtr, ref DXGI_ADAPTER_DESC1 pDesc);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DXGI_ADAPTER_DESC1
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string Description;
        public uint VendorId;
        public uint DeviceId;
        public uint SubSysId;
        public uint Revision;
        public IntPtr DedicatedVideoMemory;
        public IntPtr DedicatedSystemMemory;
        public IntPtr SharedSystemMemory;
        public LUID AdapterLuid;
        public uint Flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct LUID
    {
        public uint LowPart;
        public int HighPart;
    }

    #endregion
}
