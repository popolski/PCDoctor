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