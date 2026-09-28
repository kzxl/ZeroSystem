using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using ZeroSystem.Native;

namespace ZeroSystem;

/// <summary>
/// Specifies the autostart execution scope for an application.
/// </summary>
public enum StartupScope
{
    /// <summary>
    /// Starts when the user logs into their desktop session. Does not require administrative elevation.
    /// Windows: Registry Run Key; Linux: ~/.config/autostart; macOS: ~/Library/LaunchAgents.
    /// </summary>
    UserLogin,

    /// <summary>
    /// Starts as a background service at system boot under elevated privileges (e.g. SYSTEM / root).
    /// Windows: Task Scheduler (/sc onstart); Linux: systemd (/etc/systemd/system); macOS: /Library/LaunchDaemons.
    /// </summary>
    SystemBoot
}

/// <summary>
/// Represents the registration status of an autostart application.
/// </summary>
public sealed class StartupRegistrationStatus
{
    public bool IsEnabled { get; set; }
    public StartupScope Scope { get; set; }
    public string AppName { get; set; } = string.Empty;
    public string ExecutablePath { get; set; } = string.Empty;
    public string Arguments { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
}

/// <summary>
/// Sovereign, zero-dependency platform manager for background execution, console visibility,
/// and user login / system boot autostart across Windows, Linux, and macOS.
/// </summary>
public static class StartupManager
{
    #region OS Platform Helpers

    public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    public static bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
    public static bool IsMacOS => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

    #endregion

    #region Console Visibility Management

    /// <summary>
    /// Hides the current console window on Windows to run silently in the background.
    /// Safe to call on any platform or inside non-console processes (no-op).
    /// </summary>
    public static bool HideConsoleWindow()
    {
        if (!IsWindows) return false;

        try
        {
            IntPtr hWnd = NativeMethods.GetConsoleWindow();
            if (hWnd != IntPtr.Zero)
            {
                return NativeMethods.ShowWindow(hWnd, NativeMethods.SW_HIDE);
            }
        }
        catch { }

        return false;
    }

    /// <summary>
    /// Shows the current console window on Windows if previously hidden.
    /// </summary>
    public static bool ShowConsoleWindow()
    {
        if (!IsWindows) return false;

        try
        {
            IntPtr hWnd = NativeMethods.GetConsoleWindow();
            if (hWnd != IntPtr.Zero)
            {
                return NativeMethods.ShowWindow(hWnd, NativeMethods.SW_SHOW);
            }
        }
        catch { }

        return false;
    }

    /// <summary>
    /// Checks whether the current process console window is currently visible.
    /// </summary>
    public static bool IsConsoleVisible()
    {
        if (!IsWindows) return true;

        try
        {
            IntPtr hWnd = NativeMethods.GetConsoleWindow();
            if (hWnd != IntPtr.Zero)
            {
                return NativeMethods.IsWindowVisible(hWnd);
            }
        }
        catch { }

        return false;
    }

    #endregion

    #region Path & Logging Utilities

    /// <summary>
    /// Resolves the current executing binary path across .NET versions and OS platforms.
    /// </summary>
    public static string GetCurrentExecutablePath()
    {
        string? processPath = Environment.ProcessPath;
        if (!string.IsNullOrEmpty(processPath))
        {
            return processPath;
        }

        try
        {
            string? mainModule = Process.GetCurrentProcess().MainModule?.FileName;
            if (!string.IsNullOrEmpty(mainModule))
            {
                return mainModule;
            }
        }
        catch { }

        return AppDomain.CurrentDomain.FriendlyName;
    }

    /// <summary>
    /// Returns a standard persistent application logging directory and file path.
    /// </summary>
    /// <param name="appName">Application identifier (e.g. "ZPrint", "ZeroApp").</param>
    /// <param name="logFileName">Log file name (default: "service.log").</param>
    public static string GetDefaultLogPath(string appName, string logFileName = "service.log")
    {
        if (string.IsNullOrWhiteSpace(appName)) throw new ArgumentException("App name cannot be empty.", nameof(appName));

        string baseDir;
        if (IsWindows)
        {
            baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), appName);
        }
        else
        {
            baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), $".{appName.ToLowerInvariant()}");
        }

        if (!Directory.Exists(baseDir))
        {
            try { Directory.CreateDirectory(baseDir); } catch { }
        }

        return Path.Combine(baseDir, logFileName);
    }

    #endregion

    #region Registration & Unregistration

    /// <summary>
    /// Registers the application to start automatically on login or system boot.
    /// </summary>
    public static (bool Success, string Message) Register(
        string appName,
        string? executablePath = null,
        string arguments = "",
        StartupScope scope = StartupScope.UserLogin)
    {
        if (string.IsNullOrWhiteSpace(appName))
            throw new ArgumentException("App name cannot be empty.", nameof(appName));

        string exe = string.IsNullOrEmpty(executablePath) ? GetCurrentExecutablePath() : executablePath!;
        string trimmedArgs = arguments?.Trim() ?? string.Empty;

        return scope == StartupScope.UserLogin
            ? RegisterUserLogin(appName, exe, trimmedArgs)
            : RegisterSystemBoot(appName, exe, trimmedArgs);
    }

    /// <summary>
    /// Removes the application from automatic startup.
    /// </summary>
    public static (bool Success, string Message) Unregister(string appName, StartupScope scope = StartupScope.UserLogin)
    {
        if (string.IsNullOrWhiteSpace(appName))
            throw new ArgumentException("App name cannot be empty.", nameof(appName));

        return scope == StartupScope.UserLogin
            ? UnregisterUserLogin(appName)
            : UnregisterSystemBoot(appName);
    }

    /// <summary>
    /// Queries the current registration status of the application for the given scope.
    /// </summary>
    public static StartupRegistrationStatus GetStatus(string appName, StartupScope scope = StartupScope.UserLogin)
    {
        if (string.IsNullOrWhiteSpace(appName))
            throw new ArgumentException("App name cannot be empty.", nameof(appName));

        return scope == StartupScope.UserLogin
            ? GetUserLoginStatus(appName)
            : GetSystemBootStatus(appName);
    }

    #endregion

    #region UserLogin Implementation

    private static (bool Success, string Message) RegisterUserLogin(string appName, string exePath, string args)
    {
        string fullCmd = string.IsNullOrEmpty(args) ? $"\"{exePath}\"" : $"\"{exePath}\" {args}";

        if (IsWindows)
        {
            string escapedCmd = fullCmd.Replace("\"", "\\\"");
            string regArgs = $"add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Run\" /v \"{appName}\" /t REG_SZ /d \"{escapedCmd}\" /f";
            var (code, output, err) = RunProcess("reg.exe", regArgs);
            if (code == 0)
            {
                return (true, $"User autostart registered for '{appName}'. Command: {fullCmd}");
            }
            return (false, $"Registry error: {err} {output}".Trim());
        }
        else if (IsLinux)
        {
            try
            {
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "autostart");
                Directory.CreateDirectory(dir);
                string file = Path.Combine(dir, $"{appName.ToLowerInvariant()}.desktop");

                string content = $"[Desktop Entry]\nType=Application\nName={appName}\nExec=\"{exePath}\" {args}\nHidden=false\nNoDisplay=false\nX-GNOME-Autostart-enabled=true\n";
                File.WriteAllText(file, content);
                return (true, $"Linux desktop autostart written to: {file}");
            }
            catch (Exception ex)
            {
                return (false, $"Failed to create desktop autostart file: {ex.Message}");
            }
        }
        else if (IsMacOS)
        {
            try
            {
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "LaunchAgents");
                Directory.CreateDirectory(dir);
                string plistPath = Path.Combine(dir, $"com.zerouniverse.{appName.ToLowerInvariant()}.plist");

                var sb = new StringBuilder();
                sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                sb.AppendLine("<!DOCTYPE plist PUBLIC \"-//Apple//DTD PLIST 1.0//EN\" \"http://www.apple.com/DTDs/PropertyList-1.0.dtd\">");
                sb.AppendLine("<plist version=\"1.0\">");
                sb.AppendLine("<dict>");
                sb.AppendLine($"    <key>Label</key><string>com.zerouniverse.{appName.ToLowerInvariant()}</string>");
                sb.AppendLine("    <key>ProgramArguments</key>");
                sb.AppendLine("    <array>");
                sb.AppendLine($"        <string>{exePath}</string>");
                if (!string.IsNullOrEmpty(args))
                {
                    foreach (var token in args.Split(' '))
                    {
                        if (!string.IsNullOrEmpty(token)) sb.AppendLine($"        <string>{token}</string>");
                    }
                }
                sb.AppendLine("    </array>");
                sb.AppendLine("    <key>RunAtLoad</key><true/>");
                sb.AppendLine("    <key>KeepAlive</key><true/>");
                sb.AppendLine("</dict>");
                sb.AppendLine("</plist>");

                File.WriteAllText(plistPath, sb.ToString());
                return (true, $"macOS LaunchAgent created at: {plistPath}");
            }
            catch (Exception ex)
            {
                return (false, $"Failed to create macOS LaunchAgent: {ex.Message}");
            }
        }

        return (false, "Platform not supported for user autostart.");
    }

    private static (bool Success, string Message) UnregisterUserLogin(string appName)
    {
        if (IsWindows)
        {
            string regArgs = $"delete \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Run\" /v \"{appName}\" /f";
            var (code, output, err) = RunProcess("reg.exe", regArgs);
            if (code == 0)
            {
                return (true, $"User autostart unregistered for '{appName}'.");
            }
            return (false, $"Registry error: {err} {output}".Trim());
        }
        else if (IsLinux)
        {
            string file = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "autostart", $"{appName.ToLowerInvariant()}.desktop");
            if (File.Exists(file))
            {
                File.Delete(file);
                return (true, $"Removed autostart file: {file}");
            }
            return (true, "No autostart file found.");
        }
        else if (IsMacOS)
        {
            string file = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "LaunchAgents", $"com.zerouniverse.{appName.ToLowerInvariant()}.plist");
            if (File.Exists(file))
            {
                File.Delete(file);
                return (true, $"Removed LaunchAgent: {file}");
            }
            return (true, "No LaunchAgent found.");
        }

        return (false, "Platform not supported.");
    }

    private static StartupRegistrationStatus GetUserLoginStatus(string appName)
    {
        var status = new StartupRegistrationStatus
        {
            AppName = appName,
            Scope = StartupScope.UserLogin
        };

        if (IsWindows)
        {
            var (code, output, _) = RunProcess("reg.exe", $"query \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Run\" /v \"{appName}\"");
            if (code == 0 && output.Contains(appName))
            {
                status.IsEnabled = true;
                status.Details = output.Trim();
            }
            else
            {
                status.IsEnabled = false;
                status.Details = "Registry entry not found.";
            }
        }
        else if (IsLinux)
        {
            string file = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "autostart", $"{appName.ToLowerInvariant()}.desktop");
            status.IsEnabled = File.Exists(file);
            status.Details = status.IsEnabled ? $"Config file: {file}" : "Desktop autostart file not found.";
        }
        else if (IsMacOS)
        {
            string file = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "LaunchAgents", $"com.zerouniverse.{appName.ToLowerInvariant()}.plist");
            status.IsEnabled = File.Exists(file);
            status.Details = status.IsEnabled ? $"Plist: {file}" : "LaunchAgent plist not found.";
        }

        return status;
    }

    #endregion

    #region SystemBoot Implementation

    private static (bool Success, string Message) RegisterSystemBoot(string appName, string exePath, string args)
    {
        if (IsWindows)
        {
            string fullCmd = string.IsNullOrEmpty(args) ? $"\\\"{exePath}\\\"" : $"\\\"{exePath}\\\" {args}";
            string schArgs = $"/create /tn \"{appName}\" /tr \"{fullCmd}\" /sc onstart /ru SYSTEM /rl HIGHEST /f";
            var (code, output, err) = RunProcess("schtasks.exe", schArgs);
            if (code == 0)
            {
                return (true, $"System boot task '{appName}' created successfully.\nCommand: {fullCmd}\nTrigger: Machine startup (SYSTEM account).");
            }
            return (false, $"Task Scheduler error (Run as Administrator required):\n{err} {output}".Trim());
        }
        else if (IsLinux)
        {
            try
            {
                string unit = $@"[Unit]
Description={appName} Daemon Service
After=network.target

[Service]
Type=simple
ExecStart=""{exePath}"" {args}
Restart=always
RestartSec=5

[Install]
WantedBy=multi-user.target
";
                string unitPath = $"/etc/systemd/system/{appName.ToLowerInvariant()}.service";
                File.WriteAllText(unitPath, unit);
                RunProcess("systemctl", "daemon-reload");
                RunProcess("systemctl", $"enable {appName.ToLowerInvariant()}");
                return (true, $"Systemd service installed to {unitPath} and enabled.\nRun: sudo systemctl start {appName.ToLowerInvariant()}");
            }
            catch (Exception ex)
            {
                return (false, $"Systemd service installation failed (sudo required): {ex.Message}");
            }
        }

        return (false, "System service not supported on this platform.");
    }

    private static (bool Success, string Message) UnregisterSystemBoot(string appName)
    {
        if (IsWindows)
        {
            var (code, output, err) = RunProcess("schtasks.exe", $"/delete /tn \"{appName}\" /f");
            if (code == 0)
            {
                return (true, $"System boot task '{appName}' deleted.");
            }
            return (false, $"Failed to delete task: {err} {output}".Trim());
        }
        else if (IsLinux)
        {
            string svc = appName.ToLowerInvariant();
            RunProcess("systemctl", $"stop {svc}");
            RunProcess("systemctl", $"disable {svc}");
            string unitPath = $"/etc/systemd/system/{svc}.service";
            if (File.Exists(unitPath))
            {
                try { File.Delete(unitPath); } catch { }
                RunProcess("systemctl", "daemon-reload");
            }
            return (true, $"Systemd service {svc} removed.");
        }

        return (false, "Platform not supported.");
    }

    private static StartupRegistrationStatus GetSystemBootStatus(string appName)
    {
        var status = new StartupRegistrationStatus
        {
            AppName = appName,
            Scope = StartupScope.SystemBoot
        };

        if (IsWindows)
        {
            var (code, output, _) = RunProcess("schtasks.exe", $"/query /tn \"{appName}\" /fo LIST");
            if (code == 0)
            {
                status.IsEnabled = true;
                status.Details = output.Trim();
            }
            else
            {
                status.IsEnabled = false;
                status.Details = "System task not found in Task Scheduler.";
            }
        }
        else if (IsLinux)
        {
            var (code, output, _) = RunProcess("systemctl", $"status {appName.ToLowerInvariant()}");
            status.IsEnabled = code == 0;
            status.Details = output.Trim();
        }

        return status;
    }

    /// <summary>
    /// Starts the system-level background task or service immediately.
    /// </summary>
    public static (bool Success, string Message) StartSystemService(string appName)
    {
        if (IsWindows)
        {
            var (code, output, err) = RunProcess("schtasks.exe", $"/run /tn \"{appName}\"");
            return (code == 0, code == 0 ? $"Service '{appName}' started." : $"{err} {output}".Trim());
        }
        else if (IsLinux)
        {
            var (code, output, err) = RunProcess("systemctl", $"start {appName.ToLowerInvariant()}");
            return (code == 0, code == 0 ? $"Service '{appName}' started." : $"{err} {output}".Trim());
        }

        return (false, "Platform not supported.");
    }

    /// <summary>
    /// Stops the system-level background task and terminates associated processes.
    /// </summary>
    public static (bool Success, string Message) StopSystemService(string appName, string? processImageName = null)
    {
        if (IsWindows)
        {
            RunProcess("schtasks.exe", $"/end /tn \"{appName}\"");
            if (!string.IsNullOrEmpty(processImageName))
            {
                var (code, output, _) = RunProcess("taskkill.exe", $"/f /im {processImageName}");
                return (true, $"Stopped task '{appName}' and process '{processImageName}': {output.Trim()}");
            }
            return (true, $"Stopped task '{appName}'.");
        }
        else if (IsLinux)
        {
            var (code, output, err) = RunProcess("systemctl", $"stop {appName.ToLowerInvariant()}");
            return (code == 0, code == 0 ? $"Service '{appName}' stopped." : $"{err} {output}".Trim());
        }

        return (false, "Platform not supported.");
    }

    #endregion

    #region Process Runner Helper

    private static (int ExitCode, string Output, string Error) RunProcess(string fileName, string arguments)
    {
        try
        {
            using var p = new Process();
            p.StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            p.Start();
            string output = p.StandardOutput.ReadToEnd();
            string err = p.StandardError.ReadToEnd();
            p.WaitForExit(5000);

            return (p.ExitCode, output, err);
        }
        catch (Exception ex)
        {
            return (-1, string.Empty, ex.Message);
        }
    }

    #endregion
}
