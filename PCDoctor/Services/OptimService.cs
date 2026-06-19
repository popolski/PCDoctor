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