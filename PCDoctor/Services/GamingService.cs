using System;
using System.Diagnostics;
using Microsoft.Win32;

namespace PCDoctor.Services
{
    public class GamingService
    {
        // GUIDs des plans d'alimentation Windows
        private const string GuidHighPerf = "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c";
        private const string GuidUltimate  = "e9a42b02-d5df-448d-aa00-03f14749eb61";
        private const string GuidBalanced  = "381b4222-f694-41f0-9685-ff5bb260df2e";

        // ─── Plan Hautes performances ───
        public bool IsHighPerfActive()
        {
            try
            {
                var output = RunPsOutput("powercfg /getactivescheme");
                return output.Contains(GuidHighPerf, StringComparison.OrdinalIgnoreCase)
                    || output.Contains(GuidUltimate,  StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        }
        public void SetHighPerf(bool active)
        {
            if (active)
            {
                // Tente Ultimate Performance (peut ne pas exister sur certaines editions)
                RunPs($"powercfg /duplicatescheme {GuidUltimate}");
                RunPs($"powercfg /setactive {GuidUltimate}");
                // Si l'activation a echoue, bascule sur High Performance classique
                var check = RunPsOutput("powercfg /getactivescheme");
                if (!check.Contains(GuidUltimate, StringComparison.OrdinalIgnoreCase))
                    RunPs($"powercfg /setactive {GuidHighPerf}");
                Logger.Action("Plan alimentation: Hautes performances actif");
            }
            else
            {
                RunPs($"powercfg /setactive {GuidBalanced}");
                Logger.Action("Plan alimentation: Equilibre restaure");
            }
        }

        // ─── Game Mode ───
        public bool IsGameModeActive()
        {
            var v = GetDwordHkcu(@"Software\Microsoft\GameBar", "AutoGameModeEnabled");
            // Valeur absente = Windows decide automatiquement (traite comme actif)
            return v == null || v != 0;
        }
        public void SetGameMode(bool active)
        {
            SetDwordHkcu(@"Software\Microsoft\GameBar", "AutoGameModeEnabled", active ? 1 : 0);
            Logger.Action($"Game Mode: {(active ? "active" : "desactive")}");
        }

        // ─── GPU Scheduling materiel (HAGS) - necessite un redemarrage ───
        // HwSchMode : 1 = OFF, 2 = ON
        public bool IsHagsActive()
        {
            var v = RegistryHelper.GetDword(
                @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode");
            return v == 2;
        }
        public void SetHags(bool active)
        {
            RegistryHelper.SetDwordHklm(
                @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode", active ? 2 : 1);
            Logger.Action($"HAGS: {(active ? "active" : "desactive")} - redemarrage requis");
        }

        // ─── Xbox Game Bar ───
        public bool IsGameBarActive()
        {
            var v = GetDwordHkcu(
                @"Software\Microsoft\Windows\CurrentVersion\GameDVR", "AppCaptureEnabled");
            return v == null || v != 0;
        }
        public void SetGameBar(bool active)
        {
            int val = active ? 1 : 0;
            SetDwordHkcu(@"Software\Microsoft\Windows\CurrentVersion\GameDVR", "AppCaptureEnabled", val);
            SetDwordHkcu(@"Software\Microsoft\GameBar", "UseNexusForGameBarEnabled", val);
            Logger.Action($"Xbox Game Bar: {(active ? "active" : "desactive")}");
        }

        // ─── Priorite GPU (Win32PrioritySeparation) ───
        // 2 = defaut Windows ; 38 (0x26) = intervalles courts fixes, boost premier-plan maximal
        public bool IsGpuPriorityActive()
        {
            var v = RegistryHelper.GetDword(
                @"SYSTEM\CurrentControlSet\Control\PriorityControl", "Win32PrioritySeparation");
            return v == 38;
        }
        public void SetGpuPriority(bool active)
        {
            RegistryHelper.SetDwordHklm(
                @"SYSTEM\CurrentControlSet\Control\PriorityControl", "Win32PrioritySeparation",
                active ? 38 : 2);
            Logger.Action($"Priorite GPU: {(active ? "optimisee jeu (38)" : "standard (2)")}");
        }

        // ─── Acceleration souris (Enhance Pointer Precision) - HKCU ───
        // ON par defaut : MouseSpeed=1, Threshold1=6, Threshold2=10
        // OFF (gaming) : MouseSpeed=0, Threshold1=0, Threshold2=0
        public bool IsMouseAccelActive()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Mouse");
                var speed = key?.GetValue("MouseSpeed") as string;
                return speed != "0";
            }
            catch { return true; }
        }
        public void SetMouseAccel(bool active)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Mouse", writable: true);
                if (active)
                {
                    key?.SetValue("MouseSpeed", "1");
                    key?.SetValue("MouseThreshold1", "6");
                    key?.SetValue("MouseThreshold2", "10");
                }
                else
                {
                    key?.SetValue("MouseSpeed", "0");
                    key?.SetValue("MouseThreshold1", "0");
                    key?.SetValue("MouseThreshold2", "0");
                }
                Logger.Action($"Acceleration souris: {(active ? "activee" : "desactivee")}");
            }
            catch { }
        }

        // ─── Algorithme de Nagle (TCP) ───
        // TcpAckFrequency=1 + TCPNoDelay=1 -> Nagle desactive (latence reduite)
        // Ces valeurs sont sous HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\{guid}
        // On les applique sur toutes les interfaces IPv4 actives.
        public bool IsNagleDisabled()
        {
            try
            {
                using var interfaces = Registry.LocalMachine.OpenSubKey(
                    @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces");
                if (interfaces == null) return false;
                foreach (var name in interfaces.GetSubKeyNames())
                {
                    using var iface = interfaces.OpenSubKey(name);
                    if (iface?.GetValue("DhcpIPAddress") == null && iface?.GetValue("IPAddress") == null)
                        continue;
                    var freq  = iface?.GetValue("TcpAckFrequency");
                    var delay = iface?.GetValue("TCPNoDelay");
                    if (freq is int f && delay is int d && f == 1 && d == 1)
                        return true;
                }
                return false;
            }
            catch { return false; }
        }
        public void SetNagle(bool disableNagle)
        {
            try
            {
                // Cle globale
                using var global = Registry.LocalMachine.CreateSubKey(
                    @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters");
                global.SetValue("TCPNoDelay", disableNagle ? 1 : 0, RegistryValueKind.DWord);

                // Toutes les interfaces
                using var interfaces = Registry.LocalMachine.OpenSubKey(
                    @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces");
                if (interfaces != null)
                {
                    foreach (var name in interfaces.GetSubKeyNames())
                    {
                        using var iface = Registry.LocalMachine.OpenSubKey(
                            $@"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\{name}", writable: true);
                        if (iface == null) continue;
                        if (disableNagle)
                        {
                            iface.SetValue("TcpAckFrequency", 1, RegistryValueKind.DWord);
                            iface.SetValue("TCPNoDelay",      1, RegistryValueKind.DWord);
                        }
                        else
                        {
                            iface.DeleteValue("TcpAckFrequency", throwOnMissingValue: false);
                            iface.DeleteValue("TCPNoDelay",      throwOnMissingValue: false);
                        }
                    }
                }
                Logger.Action($"Algorithme Nagle : {(disableNagle ? "desactive" : "reactivé")}");
            }
            catch (Exception e) { Logger.Warn($"SetNagle : {e.Message}"); }
        }

        // ─── Helpers ───
        private static int? GetDwordHkcu(string keyPath, string valueName)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(keyPath);
                var v = key?.GetValue(valueName);
                return v == null ? (int?)null : (int)v;
            }
            catch { return null; }
        }
        private static void SetDwordHkcu(string keyPath, string valueName, int value)
        {
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(keyPath);
                key?.SetValue(valueName, value, RegistryValueKind.DWord);
            }
            catch { }
        }
        private static void RunPs(string cmd)
        {
            try
            {
                var psi = new ProcessStartInfo("powershell", $"-NoProfile -Command \"{cmd}\"")
                    { UseShellExecute = false, CreateNoWindow = true };
                using var p = Process.Start(psi);
                p?.WaitForExit();
            }
            catch { }
        }
        private static string RunPsOutput(string cmd)
        {
            try
            {
                var psi = new ProcessStartInfo("powershell", $"-NoProfile -Command \"{cmd}\"")
                    { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true };
                using var p = Process.Start(psi)!;
                var output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
                return output;
            }
            catch { return ""; }
        }
    }
}
