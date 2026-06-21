using System;
using System.Diagnostics;
using System.ServiceProcess;
using Microsoft.Win32;

namespace PCDoctor.Services
{
    public class OptimService
    {
        // ── Hibernation ──────────────────────────────────────────────────────
        public bool IsHibernationActive()
        {
            var v = RegistryHelper.GetDword(@"SYSTEM\CurrentControlSet\Control\Power", "HibernateEnabled");
            return v == 1;
        }
        public void SetHibernation(bool active)
        {
            var psi = new ProcessStartInfo("powercfg", $"/h {(active ? "on" : "off")}")
                { UseShellExecute = false, CreateNoWindow = true };
            using var p = Process.Start(psi);
            p?.WaitForExit();
            Logger.Action($"Hibernation {(active ? "activée" : "désactivée")}");
        }

        // ── Fast Startup ─────────────────────────────────────────────────────
        public bool IsFastStartupActive()
        {
            var v = RegistryHelper.GetDword(@"SYSTEM\CurrentControlSet\Control\Session Manager\Power", "HiberbootEnabled");
            return v == 1;
        }
        public void SetFastStartup(bool active)
        {
            RegistryHelper.SetDwordHklm(@"SYSTEM\CurrentControlSet\Control\Session Manager\Power", "HiberbootEnabled", active ? 1 : 0);
            Logger.Action($"Fast Startup {(active ? "activé" : "désactivé")}");
        }

        // ── Power Throttling ─────────────────────────────────────────────────
        public bool IsPowerThrottlingDisabled()
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Power\PowerThrottling");
            if (key == null) return false;
            return (int)(key.GetValue("PowerThrottlingOff", 0) ?? 0) == 1;
        }
        public void SetPowerThrottling(bool disableThrottling)
        {
            using var key = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\Power\PowerThrottling");
            key.SetValue("PowerThrottlingOff", disableThrottling ? 1 : 0, RegistryValueKind.DWord);
            Logger.Action($"Power Throttling {(disableThrottling ? "désactivé (perfs boost)" : "réactivé (économie énergie)")}");
        }

        // ── Compression mémoire ───────────────────────────────────────────────
        // Pas d'API .NET pour MMAgent — PowerShell reste nécessaire ici
        public bool IsMemoryCompressionActive()
        {
            try
            {
                var psi = new ProcessStartInfo("powershell.exe",
                    "-NoProfile -NonInteractive -Command \"(Get-MMAgent).MemoryCompression\"")
                { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true };
                using var p = Process.Start(psi)!;
                var output = p.StandardOutput.ReadToEnd().Trim();
                p.WaitForExit();
                return output.Equals("True", StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        }
        public void SetMemoryCompression(bool active)
        {
            var cmd = active ? "Enable-MMAgent -MemoryCompression" : "Disable-MMAgent -MemoryCompression";
            RunPs(cmd);
            Logger.Action($"Compression mémoire {(active ? "activée" : "désactivée")}");
        }

        // ── Messages de démarrage détaillés (Verbose Status) ─────────────────
        public bool IsVerboseStatusActive()
        {
            var v = RegistryHelper.GetDword(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "VerboseStatus");
            return v == 1;
        }
        public void SetVerboseStatus(bool active)
        {
            if (active)
                RegistryHelper.SetDwordHklm(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "VerboseStatus", 1);
            else
                RegistryHelper.DeleteValueHklm(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "VerboseStatus");
            Logger.Action($"Verbose Status : {(active ? "actif" : "désactivé")}");
        }

        // ── Horloge UTC ──────────────────────────────────────────────────────
        public bool IsUtcClockActive()
        {
            var v = RegistryHelper.GetDword(
                @"SYSTEM\CurrentControlSet\Control\TimeZoneInformation", "RealTimeIsUniversal");
            return v == 1;
        }
        public void SetUtcClock(bool active)
        {
            if (active)
                RegistryHelper.SetDwordHklm(
                    @"SYSTEM\CurrentControlSet\Control\TimeZoneInformation", "RealTimeIsUniversal", 1);
            else
                RegistryHelper.DeleteValueHklm(
                    @"SYSTEM\CurrentControlSet\Control\TimeZoneInformation", "RealTimeIsUniversal");
            Logger.Action($"Horloge UTC {(active ? "activée" : "désactivée")}");
        }

        // ── SysMain (Superfetch) ──────────────────────────────────────────────
        public bool IsSysMainActive()
            => GetServiceStartType("SysMain") != ServiceStartMode.Disabled;

        public void SetSysMain(bool active)
        {
            SetServiceStartType("SysMain", active ? ServiceStartMode.Automatic : ServiceStartMode.Disabled);
            ControlService("SysMain", active);
            Logger.Action($"SysMain : {(active ? "actif" : "désactivé")}");
        }

        // ── Windows Search Indexing ───────────────────────────────────────────
        public bool IsSearchIndexActive()
            => GetServiceStartType("WSearch") != ServiceStartMode.Disabled;

        public void SetSearchIndex(bool active)
        {
            SetServiceStartType("WSearch", active ? ServiceStartMode.Automatic : ServiceStartMode.Disabled);
            ControlService("WSearch", active);
            Logger.Action($"Windows Search : {(active ? "actif" : "désactivé")}");
        }

        // ── Windows Error Reporting ───────────────────────────────────────────
        public bool IsWerActive()
            => GetServiceStartType("WerSvc") != ServiceStartMode.Disabled;

        public void SetWer(bool active)
        {
            SetServiceStartType("WerSvc", active ? ServiceStartMode.Manual : ServiceStartMode.Disabled);
            if (!active) ControlService("WerSvc", false);
            Logger.Action($"Windows Error Reporting : {(active ? "actif" : "désactivé")}");
        }

        // ── Helpers services ──────────────────────────────────────────────────
        private static ServiceStartMode GetServiceStartType(string name)
        {
            try
            {
                using var sc = new ServiceController(name);
                return sc.StartType;
            }
            catch { return ServiceStartMode.Disabled; }
        }

        // ServiceController n'expose pas SetStartupType → on passe par le registre
        private static void SetServiceStartType(string name, ServiceStartMode mode)
        {
            int value = mode switch
            {
                ServiceStartMode.Automatic => 2,
                ServiceStartMode.Manual    => 3,
                _                          => 4   // Disabled
            };
            RegistryHelper.SetDwordHklm($@"SYSTEM\CurrentControlSet\Services\{name}", "Start", value);
        }

        private static void ControlService(string name, bool start)
        {
            try
            {
                using var sc = new ServiceController(name);
                if (start && sc.Status != ServiceControllerStatus.Running)
                    sc.Start();
                else if (!start && sc.Status == ServiceControllerStatus.Running)
                    sc.Stop();
            }
            catch { }
        }

        private static void RunPs(string command)
        {
            try
            {
                var psi = new ProcessStartInfo("powershell.exe",
                    $"-NoProfile -NonInteractive -Command \"{command}\"")
                { UseShellExecute = false, CreateNoWindow = true };
                using var p = Process.Start(psi);
                p?.WaitForExit();
            }
            catch (Exception e) { Logger.Warn($"OptimService PS : {e.Message}"); }
        }
    }
}
