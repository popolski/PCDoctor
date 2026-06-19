using System;
using System.Diagnostics;

namespace PCDoctor.Services
{
    public class HardeningService
    {
        private const string LlmnrKey = @"SOFTWARE\Policies\Microsoft\Windows NT\DNSClient";

        // ─── LLMNR ─── (active = EnableMulticast absent ou !=0 ; sécurisé = 0)
        public bool IsLlmnrActive()
        {
            var v = RegistryHelper.GetDword(LlmnrKey, "EnableMulticast");
            return v != 0; // null (absent) ou !=0 => actif
        }

        public void SetLlmnr(bool active)
        {
            if (active)
                RegistryHelper.DeleteValueHklm(LlmnrKey, "EnableMulticast"); // retour défaut = actif
            else
                RegistryHelper.SetDwordHklm(LlmnrKey, "EnableMulticast", 0); // 0 = désactivé (sécurisé)
        }

        // ─── SMBv1 ─── via DISM (feature Windows). On lance la commande.
        public bool IsSmb1Active()
        {
            try
            {
                var psi = new ProcessStartInfo("powershell",
                    "-NoProfile -Command \"(Get-WindowsOptionalFeature -Online -FeatureName SMB1Protocol).State\"")
                { RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true };
                using var p = Process.Start(psi);
                string outp = p!.StandardOutput.ReadToEnd();
                p.WaitForExit();
                return outp.Trim().Equals("Enabled", StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        }

        public void SetSmb1(bool active)
        {
            string verb = active ? "Enable" : "Disable";
            var psi = new ProcessStartInfo("powershell",
                $"-NoProfile -Command \"{verb}-WindowsOptionalFeature -Online -FeatureName SMB1Protocol -NoRestart\"")
            { UseShellExecute = false, CreateNoWindow = true };
            using var p = Process.Start(psi);
            p!.WaitForExit();
        }

        // ─── mDNS / Bonjour ───
        // mDNS Windows integre : HKLM\SYSTEM\CurrentControlSet\Services\Dnscache\Parameters\EnableMDNS
        // 0 = desactive, 1 (ou absent) = actif
        // Bonjour Apple : service "Bonjour Service" (mDNSResponder)
        public bool IsMdnsActive()
        {
            var v = RegistryHelper.GetDword(@"SYSTEM\CurrentControlSet\Services\Dnscache\Parameters", "EnableMDNS");
            return v != 0; // absent ou 1 = actif
        }
        public bool IsBonjourInstalled()
        {
            try
            {
                var psi = new ProcessStartInfo("sc.exe", "query \"Bonjour Service\"")
                { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true };
                using var p = Process.Start(psi)!;
                string o = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
                return o.Contains("Bonjour", StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        }
        public void SetMdns(bool active)
        {
            if (active)
                RegistryHelper.DeleteValueHklm(@"SYSTEM\CurrentControlSet\Services\Dnscache\Parameters", "EnableMDNS");
            else
                RegistryHelper.SetDwordHklm(@"SYSTEM\CurrentControlSet\Services\Dnscache\Parameters", "EnableMDNS", 0);
            Logger.Action($"mDNS Windows {(active ? "reactivé" : "desactive")}");
        }
        public string DisableBonjour()
        {
            try
            {
                var psi = new ProcessStartInfo("sc.exe", "stop \"Bonjour Service\"")
                { UseShellExecute = false, CreateNoWindow = true };
                using var p1 = Process.Start(psi)!;
                p1.WaitForExit();

                var psi2 = new ProcessStartInfo("sc.exe", "config \"Bonjour Service\" start= disabled")
                { UseShellExecute = false, CreateNoWindow = true };
                using var p2 = Process.Start(psi2)!;
                p2.WaitForExit();

                Logger.Action("Service Bonjour desactive");
                return "Service Bonjour desactive (arrete et demarrage mis a Desactive).";
            }
            catch (Exception e) { return $"Erreur : {e.Message}"; }
        }

        // ─── Defender : informations signatures ───
        public (string version, string date) GetDefenderSignatureInfo()
        {
            try
            {
                var psi = new ProcessStartInfo("powershell",
                    "-NoProfile -Command \"$s = Get-MpComputerStatus; '{0}|{1}' -f $s.AntivirusSignatureVersion,$s.AntivirusSignatureLastUpdated\"")
                { RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true };
                using var p = Process.Start(psi)!;
                string o = p.StandardOutput.ReadToEnd().Trim();
                p.WaitForExit();
                var parts = o.Split('|');
                string ver  = parts.Length > 0 ? parts[0].Trim() : "?";
                string date = parts.Length > 1 ? parts[1].Trim() : "?";
                // date est au format ISO -> on tente de la formater
                if (DateTime.TryParse(date, out var dt))
                    date = dt.ToString("dd/MM/yyyy HH:mm");
                return (ver, date);
            }
            catch { return ("?", "?"); }
        }

        // Lance une MAJ des signatures Defender
        public string UpdateDefenderSignatures()
        {
            try
            {
                var psi = new ProcessStartInfo("powershell",
                    "-NoProfile -Command \"Update-MpSignature\"")
                { UseShellExecute = false, CreateNoWindow = true };
                using var p = Process.Start(psi)!;
                p.WaitForExit();
                Logger.Action("Signatures Defender mises à jour");
                if (p.ExitCode == 0)
                {
                    var (ver, date) = GetDefenderSignatureInfo();
                    return $"Signatures mises à jour - v{ver} ({date})";
                }
                return $"Mise à jour terminée avec le code {p.ExitCode}.";
            }
            catch (Exception e) { return $"Erreur : {e.Message}"; }
        }

        // Lance un scan rapide Defender
        public string StartQuickScan()
        {
            try
            {
                var psi = new ProcessStartInfo("powershell",
                    "-NoProfile -Command \"Start-MpScan -ScanType QuickScan\"")
                { UseShellExecute = false, CreateNoWindow = true };
                using var p = Process.Start(psi)!;
                p.WaitForExit();
                Logger.Action("Scan rapide Defender lancé");
                return p.ExitCode == 0
                    ? "Scan rapide terminé."
                    : $"Scan terminé avec le code {p.ExitCode}.";
            }
            catch (Exception e) { return $"Erreur : {e.Message}"; }
        }

        // ─── SmartScreen / PUA Protection ─── via Defender
        public bool IsPuaActive()
        {
            try
            {
                var psi = new ProcessStartInfo("powershell",
                    "-NoProfile -Command \"(Get-MpPreference).PUAProtection\"")
                { RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true };
                using var p = Process.Start(psi);
                string outp = p!.StandardOutput.ReadToEnd();
                p.WaitForExit();
                return outp.Trim() == "1";
            }
            catch { return false; }
        }

        public void SetPua(bool active)
        {
            string val = active ? "Enabled" : "Disabled";
            var psi = new ProcessStartInfo("powershell",
                $"-NoProfile -Command \"Set-MpPreference -PUAProtection {val}\"")
            { UseShellExecute = false, CreateNoWindow = true };
            using var p = Process.Start(psi);
            p!.WaitForExit();
        }
    }
}