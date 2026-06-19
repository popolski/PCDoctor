using System;
using System.Diagnostics;
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
            RunCmd($"powercfg /h {(active ? "on" : "off")}");
            Logger.Action($"Hibernation {(active ? "activée" : "désactivée")}");
        }

        // ── Fast Startup ─────────────────────────────────────────────────────
        // HiberbootEnabled = 1 -> Fast Startup actif
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
        // PowerThrottlingOff = 1 -> throttling DESACTIVE (meilleures perfs)
        // Convention UI : ON = fonctionnalite active = throttling desactive = boost perf
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
        // Gérée par PowerShell (Enable/Disable-MMAgent -MemoryCompression)
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
        // VerboseStatus = 1 -> affiche "Application des stratégies..." au boot/arrêt
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
            Logger.Action($"Verbose Status : {(active ? "actif" : "desactive")}");
        }

        // ── Horloge UTC (dual-boot Linux) ────────────────────────────────────
        // RealTimeIsUniversal = 1 -> Windows stocke l'heure RTC en UTC (comme Linux)
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
        {
            try
            {
                var psi = new ProcessStartInfo("powershell.exe",
                    "-NoProfile -NonInteractive -Command \"(Get-Service SysMain).StartType\"")
                { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true };
                using var p = Process.Start(psi)!;
                var o = p.StandardOutput.ReadToEnd().Trim();
                p.WaitForExit();
                return !o.Equals("Disabled", StringComparison.OrdinalIgnoreCase);
            }
            catch { return true; }
        }
        public void SetSysMain(bool active)
        {
            string start = active ? "Automatic" : "Disabled";
            string stop  = active ? "Start-Service SysMain -ErrorAction SilentlyContinue" : "Stop-Service SysMain -Force -ErrorAction SilentlyContinue";
            RunPs($"Set-Service SysMain -StartupType {start}; {stop}");
            Logger.Action($"SysMain : {(active ? "actif" : "désactivé")}");
        }

        // ── Windows Search Indexing ───────────────────────────────────────────
        public bool IsSearchIndexActive()
        {
            try
            {
                var psi = new ProcessStartInfo("powershell.exe",
                    "-NoProfile -NonInteractive -Command \"(Get-Service WSearch).StartType\"")
                { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true };
                using var p = Process.Start(psi)!;
                var o = p.StandardOutput.ReadToEnd().Trim();
                p.WaitForExit();
                return !o.Equals("Disabled", StringComparison.OrdinalIgnoreCase);
            }
            catch { return true; }
        }
        public void SetSearchIndex(bool active)
        {
            string start = active ? "Automatic" : "Disabled";
            string stop  = active ? "Start-Service WSearch -ErrorAction SilentlyContinue" : "Stop-Service WSearch -Force -ErrorAction SilentlyContinue";
            RunPs($"Set-Service WSearch -StartupType {start}; {stop}");
            Logger.Action($"Windows Search : {(active ? "actif" : "désactivé")}");
        }

        // ── Windows Error Reporting ───────────────────────────────────────────
        public bool IsWerActive()
        {
            try
            {
                var psi = new ProcessStartInfo("powershell.exe",
                    "-NoProfile -NonInteractive -Command \"(Get-Service WerSvc).StartType\"")
                { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true };
                using var p = Process.Start(psi)!;
                var o = p.StandardOutput.ReadToEnd().Trim();
                p.WaitForExit();
                return !o.Equals("Disabled", StringComparison.OrdinalIgnoreCase);
            }
            catch { return true; }
        }
        public void SetWer(bool active)
        {
            string start = active ? "Manual" : "Disabled";
            string stop  = active ? "" : "; Stop-Service WerSvc -Force -ErrorAction SilentlyContinue";
            RunPs($"Set-Service WerSvc -StartupType {start}{stop}");
            Logger.Action($"Windows Error Reporting : {(active ? "actif" : "désactivé")}");
        }

        private void RunCmd(string cmd)
        {
            try
            {
                var psi = new ProcessStartInfo("cmd.exe", "/c " + cmd)
                { UseShellExecute = false, CreateNoWindow = true };
                using var p = Process.Start(psi);
                p!.WaitForExit();
            }
            catch (Exception e) { Logger.Warn($"OptimService : {e.Message}"); }
        }

        private void RunPs(string command)
        {
            try
            {
                var psi = new ProcessStartInfo("powershell.exe",
                    $"-NoProfile -NonInteractive -Command \"{command}\"")
                { UseShellExecute = false, CreateNoWindow = true };
                using var p = Process.Start(psi);
                p!.WaitForExit();
            }
            catch (Exception e) { Logger.Warn($"OptimService PS : {e.Message}"); }
        }
    }
}