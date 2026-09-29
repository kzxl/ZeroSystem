using System;
using System.Runtime.InteropServices;

namespace ZeroSystem.Native;

internal static class NativeMethods
{
    // Window & Process
    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    public static extern IntPtr GetDesktopWindow();

    [DllImport("user32.dll")]
    public static extern IntPtr GetShellWindow();

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string? className, string? windowTitle);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern IntPtr SendMessageTimeout(
        IntPtr hWnd,
        uint msg,
        IntPtr wParam,
        IntPtr lParam,
        uint fuFlags,
        uint uTimeout,
        out UIntPtr lpdwResult);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    // Memory & Working Set
    [DllImport("psapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool EmptyWorkingSet(IntPtr hProcess);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    [DllImport("ntdll.dll", SetLastError = true)]
    public static extern int NtSetSystemInformation(int SystemInformationClass, ref int SystemInformation, int SystemInformationLength);

    // Tokens & Privileges
    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool LookupPrivilegeValue(string? lpSystemName, string lpName, out LUID lpLuid);

    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool AdjustTokenPrivileges(
        IntPtr TokenHandle,
        [MarshalAs(UnmanagedType.Bool)] bool DisableAllPrivileges,
        ref TOKEN_PRIVILEGES NewState,
        uint BufferLength,
        IntPtr PreviousState,
        IntPtr ReturnLength);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool CloseHandle(IntPtr hObject);

    // DOS Devices
    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DefineDosDevice(uint dwFlags, string lpDeviceName, string? lpTargetPath);

    // Power
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetSystemPowerStatus(out SYSTEM_POWER_STATUS lpSystemPowerStatus);

    // Structs
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
        public int Width => Right - Left;
        public int Height => Bottom - Top;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct LUID
    {
        public uint LowPart;
        public int HighPart;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct TOKEN_PRIVILEGES
    {
        public uint PrivilegeCount;
        public LUID Luid;
        public uint Attributes;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SYSTEM_POWER_STATUS
    {
        public byte ACLineStatus;
        public byte BatteryFlag;
        public byte BatteryLifePercent;
        public byte SystemStatusFlag;
        public int BatteryLifeTime;
        public int BatteryFullLifeTime;
    }

    // Constants
    public const uint TOKEN_ADJUST_PRIVILEGES = 0x0020;
    public const uint TOKEN_QUERY = 0x0008;
    public const uint SE_PRIVILEGE_ENABLED = 0x00000002;
    public const string SE_PROFILE_SINGLE_PROCESS_NAME = "SeProfileSingleProcessPrivilege";
    public const string SE_INCREASE_QUOTA_NAME = "SeIncreaseQuotaPrivilege";

    public const int SystemMemoryListInformation = 80;
    public const int MemoryPurgeStandbyList = 4;

    public const uint DDD_RAW_TARGET_PATH = 0x00000001;
    public const uint DDD_REMOVE_DEFINITION = 0x00000002;
    public const uint DDD_EXACT_MATCH_ON_REMOVE = 0x00000004;

    public const uint SMTO_NORMAL = 0x0000;
    public const uint WM_SPAWN_WORKER = 0x052C;

    // Console Window
    public const int SW_HIDE = 0;
    public const int SW_SHOW = 5;
    public const int SW_RESTORE = 9;

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr GetConsoleWindow();

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsIconic(IntPtr hWnd);

    // Restart Manager (File Lock Inspection)
    public const int CCH_RM_MAX_APP_NAME = 255;
    public const int CCH_RM_MAX_SVC_NAME = 63;
    public const int ERROR_MORE_DATA = 234;

    public enum RM_APP_TYPE
    {
        RmUnknownApp = 0,
        RmMainWindow = 1,
        RmOtherWindow = 2,
        RmService = 3,
        RmExplorer = 4,
        RmConsole = 5,
        RmCritical = 1000
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RM_FILETIME
    {
        public uint dwLowDateTime;
        public uint dwHighDateTime;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RM_UNIQUE_PROCESS
    {
        public int dwProcessId;
        public RM_FILETIME ProcessStartTime;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct RM_PROCESS_INFO
    {
        public RM_UNIQUE_PROCESS Process;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCH_RM_MAX_APP_NAME + 1)]
        public string strAppName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCH_RM_MAX_SVC_NAME + 1)]
        public string strServiceShortName;
        public RM_APP_TYPE ApplicationType;
        public uint AppStatus;
        public uint TSSessionId;
        [MarshalAs(UnmanagedType.Bool)]
        public bool bRestartable;
    }

    [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
    public static extern int RmStartSession(out uint pSessionHandle, uint dwSessionFlags, string strSessionKey);

    [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
    public static extern int RmRegisterResources(
        uint pSessionHandle,
        uint nFiles,
        string[]? rgsFilenames,
        uint nApplications,
        [In] RM_UNIQUE_PROCESS[]? rgApplications,
        uint nServices,
        string[]? rgsServiceNames);

    [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
    public static extern int RmGetList(
        uint pSessionHandle,
        out uint pnProcInfoNeeded,
        ref uint pnProcInfo,
        [In, Out] RM_PROCESS_INFO[]? rgAffectedApps,
        out uint lpdwRebootReasons);

    [DllImport("rstrtmgr.dll")]
    public static extern int RmEndSession(uint pSessionHandle);

    // Firmware Table (SMBIOS)
    public const uint FIRMWARE_TABLE_PROVIDER_RSMB = 0x52534D42; // 'RSMB'

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern uint GetSystemFirmwareTable(
        uint FirmwareTableProviderSignature,
        uint FirmwareTableID,
        IntPtr pFirmwareTableBuffer,
        uint BufferSize);

    // Display & Monitors
    public delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct MONITORINFOEX
    {
        public uint cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string szDevice;
    }

    public const uint MONITORINFOF_PRIMARY = 0x00000001;

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);

    [DllImport("user32.dll")]
    public static extern int GetSystemMetrics(int nIndex);

    public const int SM_CXSCREEN = 0;
    public const int SM_CYSCREEN = 1;
    public const int SM_XVIRTUALSCREEN = 76;
    public const int SM_YVIRTUALSCREEN = 77;
    public const int SM_CXVIRTUALSCREEN = 78;
    public const int SM_CYVIRTUALSCREEN = 79;
    public const int SM_CMONITORS = 80;

    [DllImport("shcore.dll")]
    public static extern int GetDpiForMonitor(IntPtr hmonitor, int dpiType, out uint dpiX, out uint dpiY);

    // Service Management (Advapi32)
    public const uint SC_MANAGER_CONNECT = 0x0001;
    public const uint SC_MANAGER_ENUMERATE_SERVICE = 0x0004;

    public const uint SERVICE_QUERY_STATUS = 0x0004;
    public const uint SERVICE_START = 0x0010;
    public const uint SERVICE_STOP = 0x0020;
    public const uint SERVICE_CONTROL_STOP = 0x00000001;

    public const uint SERVICE_STOPPED = 0x00000001;
    public const uint SERVICE_START_PENDING = 0x00000002;
    public const uint SERVICE_STOP_PENDING = 0x00000003;
    public const uint SERVICE_RUNNING = 0x00000004;
    public const uint SERVICE_CONTINUE_PENDING = 0x00000005;
    public const uint SERVICE_PAUSE_PENDING = 0x00000006;
    public const uint SERVICE_PAUSED = 0x00000007;

    public const int SC_STATUS_PROCESS_INFO = 0;

    [StructLayout(LayoutKind.Sequential)]
    public struct SERVICE_STATUS
    {
        public uint dwServiceType;
        public uint dwCurrentState;
        public uint dwControlsAccepted;
        public uint dwWin32ExitCode;
        public uint dwServiceSpecificExitCode;
        public uint dwCheckPoint;
        public uint dwWaitHint;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SERVICE_STATUS_PROCESS
    {
        public uint dwServiceType;
        public uint dwCurrentState;
        public uint dwControlsAccepted;
        public uint dwWin32ExitCode;
        public uint dwServiceSpecificExitCode;
        public uint dwCheckPoint;
        public uint dwWaitHint;
        public uint dwProcessId;
        public uint dwServiceFlags;
    }

    [DllImport("advapi32.dll", EntryPoint = "OpenSCManagerW", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern IntPtr OpenSCManager(string? machineName, string? databaseName, uint desiredAccess);

    [DllImport("advapi32.dll", EntryPoint = "OpenServiceW", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern IntPtr OpenService(IntPtr hSCManager, string serviceName, uint desiredAccess);

    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool QueryServiceStatusEx(
        IntPtr hService,
        int infoLevel,
        IntPtr lpBuffer,
        uint cbBufSize,
        out uint pcbBytesNeeded);

    [DllImport("advapi32.dll", EntryPoint = "StartServiceW", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool StartService(IntPtr hService, uint dwNumServiceArgs, IntPtr lpServiceArgVectors);

    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ControlService(IntPtr hService, uint dwControl, ref SERVICE_STATUS lpServiceStatus);

    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool CloseServiceHandle(IntPtr hSCObject);

    // Raw File / Physical Drive
    public const uint GENERIC_READ = 0x80000000;
    public const uint GENERIC_WRITE = 0x40000000;
    public const uint FILE_SHARE_READ = 0x00000001;
    public const uint FILE_SHARE_WRITE = 0x00000002;
    public const uint OPEN_EXISTING = 3;
    public static readonly IntPtr INVALID_HANDLE_VALUE = new(-1);

    [DllImport("kernel32.dll", EntryPoint = "CreateFileW", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern IntPtr CreateFile(
        string lpFileName,
        uint dwDesiredAccess,
        uint dwShareMode,
        IntPtr lpSecurityAttributes,
        uint dwCreationDisposition,
        uint dwFlagsAndAttributes,
        IntPtr hTemplateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DeviceIoControl(
        IntPtr hDevice,
        uint dwIoControlCode,
        IntPtr lpInBuffer,
        uint nInBufferSize,
        IntPtr lpOutBuffer,
        uint nOutBufferSize,
        out uint lpBytesReturned,
        IntPtr lpOverlapped);

    // IOCTL Storage Properties
    public const uint IOCTL_STORAGE_QUERY_PROPERTY = 0x002D1400;
    public const uint IOCTL_DISK_GET_DRIVE_GEOMETRY_EX = 0x000700A0;

    public enum STORAGE_PROPERTY_ID
    {
        StorageDeviceProperty = 0,
        StorageAdapterProperty = 1,
        StorageDeviceIdProperty = 2,
        StorageDeviceUniqueIdProperty = 3,
        StorageDeviceWriteCacheProperty = 4,
        StorageMiniportProperty = 5,
        StorageAccessAlignmentProperty = 6,
        StorageDeviceSeekPenaltyProperty = 7,
        StorageDeviceTrimProperty = 8
    }

    public enum STORAGE_QUERY_TYPE
    {
        PropertyStandardQuery = 0,
        PropertyExistsQuery = 1,
        PropertyMaskQuery = 2,
        PropertyQueryMaxDefined = 3
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct STORAGE_PROPERTY_QUERY
    {
        public STORAGE_PROPERTY_ID PropertyId;
        public STORAGE_QUERY_TYPE QueryType;
        public byte AdditionalParameters;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DEVICE_SEEK_PENALTY_DESCRIPTOR
    {
        public uint Version;
        public uint Size;
        [MarshalAs(UnmanagedType.I1)]
        public bool IncursSeekPenalty;
    }

    public enum STORAGE_BUS_TYPE
    {
        BusTypeUnknown = 0x00,
        BusTypeScsi = 0x01,
        BusTypeAtapi = 0x02,
        BusTypeAta = 0x03,
        BusType1394 = 0x04,
        BusTypeSsa = 0x05,
        BusTypeFibre = 0x06,
        BusTypeUsb = 0x07,
        BusTypeRAID = 0x08,
        BusTypeiScsi = 0x09,
        BusTypeSas = 0x0A,
        BusTypeSata = 0x0B,
        BusTypeSd = 0x0C,
        BusTypeMmc = 0x0D,
        BusTypeVirtual = 0x0E,
        BusTypeFileBackedVirtual = 0x0F,
        BusTypeSpaces = 0x10,
        BusTypeNvme = 0x11,
        BusTypeSCM = 0x12,
        BusTypeUfs = 0x13,
        BusTypeMax = 0x14
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct STORAGE_DEVICE_DESCRIPTOR
    {
        public uint Version;
        public uint Size;
        public byte DeviceType;
        public byte DeviceTypeModifier;
        [MarshalAs(UnmanagedType.I1)]
        public bool RemovableMedia;
        [MarshalAs(UnmanagedType.I1)]
        public bool CommandQueueing;
        public uint VendorIdOffset;
        public uint ProductIdOffset;
        public uint ProductRevisionOffset;
        public uint SerialNumberOffset;
        public STORAGE_BUS_TYPE BusType;
        public uint RawPropertiesLength;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DISK_GEOMETRY
    {
        public long Cylinders;
        public int MediaType;
        public uint TracksPerCylinder;
        public uint SectorsPerTrack;
        public uint BytesPerSector;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DISK_GEOMETRY_EX
    {
        public DISK_GEOMETRY Geometry;
        public long DiskSize;
        public byte Data;
    }

    // IP Helper (Extended TCP/UDP Tables with PID)
    public const int AF_INET = 2;
    public const int AF_INET6 = 23;
    public const int TCP_TABLE_OWNER_PID_ALL = 5;
    public const int UDP_TABLE_OWNER_PID = 1;

    [StructLayout(LayoutKind.Sequential)]
    public struct MIB_TCPROW_OWNER_PID
    {
        public uint dwState;
        public uint dwLocalAddr;
        public uint dwLocalPort;
        public uint dwRemoteAddr;
        public uint dwRemotePort;
        public uint dwOwningPid;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MIB_UDPROW_OWNER_PID
    {
        public uint dwLocalAddr;
        public uint dwLocalPort;
        public uint dwOwningPid;
    }

    [DllImport("iphlpapi.dll", SetLastError = true)]
    public static extern uint GetExtendedTcpTable(
        IntPtr pTcpTable,
        ref uint pdwSize,
        [MarshalAs(UnmanagedType.Bool)] bool bOrder,
        uint ulAf,
        int TableClass,
        uint Reserved = 0);

    [DllImport("iphlpapi.dll", SetLastError = true)]
    public static extern uint GetExtendedUdpTable(
        IntPtr pUdpTable,
        ref uint pdwSize,
        [MarshalAs(UnmanagedType.Bool)] bool bOrder,
        uint ulAf,
        int TableClass,
        uint Reserved = 0);

    // Deep NTDLL Process Control
    public const int SystemProcessInformation = 5;

    [DllImport("ntdll.dll", SetLastError = true)]
    public static extern int NtQuerySystemInformation(
        int SystemInformationClass,
        IntPtr SystemInformation,
        uint SystemInformationLength,
        out uint ReturnLength);

    [DllImport("ntdll.dll", SetLastError = true)]
    public static extern int NtSuspendProcess(IntPtr processHandle);

    [DllImport("ntdll.dll", SetLastError = true)]
    public static extern int NtResumeProcess(IntPtr processHandle);

    // Thread Affinity & MMCSS
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetProcessAffinityMask(IntPtr hProcess, UIntPtr dwProcessAffinityMask);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern UIntPtr SetThreadAffinityMask(IntPtr hThread, UIntPtr dwThreadAffinityMask);

    [DllImport("kernel32.dll")]
    public static extern IntPtr GetCurrentThread();

    [DllImport("kernel32.dll")]
    public static extern IntPtr GetCurrentProcess();

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetThreadPriority(IntPtr hThread, int nPriority);

    public const int THREAD_PRIORITY_NORMAL = 0;
    public const int THREAD_PRIORITY_ABOVE_NORMAL = 1;
    public const int THREAD_PRIORITY_HIGHEST = 2;
    public const int THREAD_PRIORITY_TIME_CRITICAL = 15;

    [DllImport("avrt.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern IntPtr AvSetMmThreadCharacteristics(string TaskName, out uint TaskIndex);

    [DllImport("avrt.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool AvRevertMmThreadCharacteristics(IntPtr AvrtHandle);

    // Memory Lock & Large Pages
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool VirtualLock(IntPtr lpAddress, UIntPtr dwSize);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool VirtualUnlock(IntPtr lpAddress, UIntPtr dwSize);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern UIntPtr GetLargePageMinimum();

    [StructLayout(LayoutKind.Sequential)]
    public struct WIN32_MEMORY_RANGE_ENTRY
    {
        public IntPtr VirtualAddress;
        public UIntPtr NumberOfBytes;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool PrefetchVirtualMemory(
        IntPtr hProcess,
        UIntPtr NumberOfEntries,
        [In] WIN32_MEMORY_RANGE_ENTRY[] VirtualAddresses,
        uint Flags);

    // Toolhelp32 Snapshot & Process Control
    public const uint TH32CS_SNAPPROCESS = 0x00000002;
    public const uint PROCESS_SUSPEND_RESUME = 0x0800;
    public const uint PROCESS_SET_INFORMATION = 0x0200;
    public const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct PROCESSENTRY32
    {
        public uint dwSize;
        public uint cntUsage;
        public uint th32ProcessID;
        public UIntPtr th32DefaultHeapID;
        public uint th32ModuleID;
        public uint cntThreads;
        public uint th32ParentProcessID;
        public int pcPriClassBase;
        public uint dwFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szExeFile;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr CreateToolhelp32Snapshot(uint dwFlags, uint th32ProcessID);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool Process32First(IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool Process32Next(IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr OpenProcess(uint processAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, uint processId);
}
