using System;
using System.Diagnostics;
using System.Linq;
using System.Management;

namespace PCDoctor.Services
{
    public class NetworkService
    {
        // IPv6 : actif si le binding ms_tcpip6 est activé sur au moins une interface
        public bool IsIpv6Active()
        {
            string o = RunPs("(Get-NetAdapterBinding -ComponentID ms_tcpip6 | Where-Object Enabled).Count");
            return int.TryParse(o.Trim(), out int n) && n > 0;
        }
        public void SetIpv6(bool active)
        {
            string verb = active ? "Enable" : "Disable";
            RunPs($"Get-NetAdapter | {verb}-NetAdapterBinding -ComponentID ms_tcpip6");
            Logger.Action($"IPv6 {(active ? "activé" : "désactivé")}");
        }

        // DoH : actif si un serveur DoH Cloudflare est enregistré
        public bool IsDohActive()
        {
            string o = RunPs("(Get-DnsClientDohServerAddress | Where-Object ServerAddress -eq '1.1.1.1').Count");
            return int.TryParse(o.Trim(), out int n) && n > 0;
        }
        public void SetDoh(bool active)
        {
            if (active)
            {
                RunPs("Add-DnsClientDohServerAddress -ServerAddress '1.1.1.1' -DohTemplate 'https://cloudflare-dns.com/dns-query' -AllowFallbackToUdp $false -AutoUpgrade $true -ErrorAction SilentlyContinue");
                Logger.Action("DoH Cloudflare activé");
            }
            else
            {
                RunPs("Get-DnsClientDohServerAddress | Where-Object ServerAddress -eq '1.1.1.1' | Remove-DnsClientDohServerAddress -ErrorAction SilentlyContinue");
                Logger.Action("DoH désactivé");
            }
        }

        // DNS serveurs prédéfinis
        public static readonly (string Label, string Primary, string Secondary)[] DnsPresets =
        {
            ("Automatique (FAI)",   "",                  ""),
            ("Cloudflare",          "1.1.1.1",           "1.0.0.1"),
            ("Google",              "8.8.8.8",           "8.8.4.4"),
            ("Quad9",               "9.9.9.9",           "149.112.112.112"),
            ("OpenDNS",             "208.67.222.222",    "208.67.220.220"),
        };

        public string GetCurrentDnsPreset()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT DNSServerSearchOrder FROM Win32_NetworkAdapterConfiguration WHERE IPEnabled = True");
                foreach (var obj in searcher.Get())
                {
                    var servers = obj["DNSServerSearchOrder"] as string[];
                    if (servers?.Length > 0)
                    {
                        string primary = servers[0];
                        foreach (var (label, p, _) in DnsPresets)
                            if (p == primary) return label;
                        return $"Personnalisé ({primary})";
                    }
                }
            }
            catch (Exception e) { Logger.Warn($"GetCurrentDnsPreset : {e.Message}"); }
            return "Automatique (FAI)";
        }

        public string SetDnsPreset(string label)
        {
            var preset = Array.Find(DnsPresets, x => x.Label == label);
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT * FROM Win32_NetworkAdapterConfiguration WHERE IPEnabled = True");
                foreach (ManagementObject obj in searcher.Get())
                {
                    if (string.IsNullOrEmpty(preset.Primary))
                        obj.InvokeMethod("SetDNSServerSearchOrder", new object?[] { null });
                    else
                    {
                        var servers = string.IsNullOrEmpty(preset.Secondary)
                            ? new[] { preset.Primary }
                            : new[] { preset.Primary, preset.Secondary };
                        obj.InvokeMethod("SetDNSServerSearchOrder", new object[] { servers });
                    }
                }
                if (string.IsNullOrEmpty(preset.Primary))
                {
                    Logger.Action("DNS remis en automatique (DHCP)");
                    return "DNS remis en automatique (FAI/DHCP).";
                }
                Logger.Action($"DNS changé : {label} ({preset.Primary})");
                return $"DNS appliqué : {label} ({preset.Primary} / {preset.Secondary}).";
            }
            catch (Exception e) { Logger.Warn($"SetDnsPreset : {e.Message}"); return $"Erreur : {e.Message}"; }
        }

        // Flush DNS
        public string FlushDns()
        {
            try
            {
                var psi = new ProcessStartInfo("ipconfig.exe", "/flushdns")
                { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true };
                using var p = Process.Start(psi)!;
                string output = p.StandardOutput.ReadToEnd().Trim();
                p.WaitForExit();
                Logger.Action("Cache DNS vidé");
                return p.ExitCode == 0 ? "Cache DNS vidé avec succès." : $"ipconfig /flushdns : code {p.ExitCode}";
            }
            catch (Exception e) { Logger.Warn($"FlushDns : {e.Message}"); return $"Erreur : {e.Message}"; }
        }

        // Reset Winsock
        public string ResetWinsock()
        {
            try
            {
                var psi = new ProcessStartInfo("netsh.exe", "winsock reset")
                { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true };
                using var p = Process.Start(psi)!;
                p.StandardOutput.ReadToEnd();
                p.WaitForExit();
                Logger.Action("Winsock réinitialisé");
                return p.ExitCode == 0
                    ? "Winsock réinitialisé. Redémarrage nécessaire pour finaliser."
                    : $"netsh winsock reset : code {p.ExitCode}";
            }
            catch (Exception e) { Logger.Warn($"ResetWinsock : {e.Message}"); return $"Erreur : {e.Message}"; }
        }

        private string RunPs(string cmd)
        {
            try
            {
                var psi = new ProcessStartInfo("powershell", $"-NoProfile -Command \"{cmd}\"")
                { RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true };
                using var p = Process.Start(psi);
                string o = p!.StandardOutput.ReadToEnd();
                p.WaitForExit();
                return o;
            }
            catch (Exception e) { Logger.Warn($"NetworkService : {e.Message}"); return ""; }
        }
    }
}