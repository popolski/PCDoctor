using System;
using System.Diagnostics;
using System.Linq;

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
            // Lire le premier serveur DNS de l'interface active principale
            string primary = RunPs(
                "(Get-DnsClientServerAddress -AddressFamily IPv4 | Where-Object {$_.ServerAddresses.Count -gt 0} | Select-Object -First 1).ServerAddresses[0]"
            ).Trim();

            foreach (var (label, p, _) in DnsPresets)
                if (p == primary) return label;

            return primary.Length > 0 ? $"Personnalisé ({primary})" : "Automatique (FAI)";
        }

        public string SetDnsPreset(string label)
        {
            var preset = System.Array.Find(DnsPresets, x => x.Label == label);
            try
            {
                if (string.IsNullOrEmpty(preset.Primary))
                {
                    // Remettre en automatique (DHCP)
                    RunPs("Get-NetAdapter | Where-Object Status -eq Up | ForEach-Object { Set-DnsClientServerAddress -InterfaceIndex $_.InterfaceIndex -ResetServerAddresses }");
                    Logger.Action("DNS remis en automatique (DHCP)");
                    return "DNS remis en automatique (FAI/DHCP).";
                }
                else
                {
                    string servers = string.IsNullOrEmpty(preset.Secondary)
                        ? $"'{preset.Primary}'"
                        : $"'{preset.Primary}','{preset.Secondary}'";
                    RunPs($"Get-NetAdapter | Where-Object Status -eq Up | ForEach-Object {{ Set-DnsClientServerAddress -InterfaceIndex $_.InterfaceIndex -ServerAddresses ({servers}) }}");
                    Logger.Action($"DNS changé : {label} ({preset.Primary})");
                    return $"DNS appliqué : {label} ({preset.Primary} / {preset.Secondary}).";
                }
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