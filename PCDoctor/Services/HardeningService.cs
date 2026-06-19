using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;

namespace PCDoctor.Services
{
    public class BitLockerDrive
    {
        public string Drive            { get; }
        public string Status           { get; }
        public string EncryptionMethod { get; }
        public bool   IsProtected      { get; }
        public string ProtectedLabel   => IsProtected ? "Protégé" : "Non protégé";
        public BitLockerDrive(string drive, string status, string enc, bool prot)
        { Drive = drive; Status = status; EncryptionMethod = enc; IsProtected = prot; }
    }

    public class AsrRule
    {
        public string Name      { get; }
        public string Guid      { get; }
        public bool   IsEnabled { get; }
        public string Icon      => IsEnabled ? "✅" : "⚠️";
        public AsrRule(string name, string guid, bool enabled)
        { Name = name; Guid = guid; IsEnabled = enabled; }
    }

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

        // ─── BitLocker ───────────────────────────────────────────────────────
        public List<BitLockerDrive> GetBitLockerStatus()
        {
            var list = new List<BitLockerDrive>();
            try
            {
                var psi = new ProcessStartInfo("powershell",
                    "-NoProfile -Command \"Get-BitLockerVolume | Select-Object MountPoint,VolumeStatus,EncryptionMethod,ProtectionStatus | ConvertTo-Json -Compress\"")
                { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true };
                using var p = Process.Start(psi)!;
                string json = p.StandardOutput.ReadToEnd().Trim();
                p.WaitForExit();
                if (string.IsNullOrEmpty(json)) return list;

                // Peut retourner un objet ou un tableau
                if (!json.StartsWith("[")) json = $"[{json}]";
                using var doc = JsonDocument.Parse(json);
                foreach (var el in doc.RootElement.EnumerateArray())
                {
                    list.Add(new BitLockerDrive(
                        el.GetProperty("MountPoint").GetString() ?? "?",
                        el.GetProperty("VolumeStatus").GetString() ?? "?",
                        el.GetProperty("EncryptionMethod").GetString() ?? "?",
                        el.GetProperty("ProtectionStatus").GetString() == "On"
                    ));
                }
            }
            catch (Exception e) { Logger.Warn($"GetBitLockerStatus : {e.Message}"); }
            return list;
        }

        // ─── ASR Rules (Attack Surface Reduction) ────────────────────────────

        private static readonly (string Name, string Guid)[] AsrRuleDefs =
        {
            ("Blocage des macros Office depuis Win32",          "92E97FA1-2EDF-4476-BDD6-9DD0B4DDDC7B"),
            ("Blocage des processus enfants Office",           "D4F940AB-401B-4EFC-AADC-AD5F3C50688A"),
            ("Blocage des scripts obfusqués",                  "5BEB7EFE-FD9A-4556-801D-275E5FFC04CC"),
            ("Blocage des injections de processus Win32",      "75668C1F-73B5-4CF0-BB93-3ECF5CB7CC84"),
            ("Blocage des exécutables depuis les emails",      "BE9BA2D9-53EA-4CDC-84E5-9B1EEEE46550"),
            ("Blocage des exécutables non signés depuis USB",  "B2B3F03D-6A65-4F7B-A9C7-1C7EF74A9BA4"),
            ("Credential stealing (LSASS)",                    "9E6C4E1F-7D60-472F-BA1A-A39EF669E4B2"),
            ("Blocage du contenu exécutable des emails Web",   "3B576869-A4EC-4529-8536-B80A7769E899"),
        };

        public List<AsrRule> GetAsrRules()
        {
            var list = new List<AsrRule>();
            try
            {
                var psi = new ProcessStartInfo("powershell",
                    "-NoProfile -Command \"(Get-MpPreference).AttackSurfaceReductionRules_Ids | ConvertTo-Json -Compress\"")
                { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true };
                using var p = Process.Start(psi)!;
                string json = p.StandardOutput.ReadToEnd().Trim();
                p.WaitForExit();

                var enabledGuids = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (!string.IsNullOrEmpty(json) && json != "null")
                {
                    if (!json.StartsWith("[")) json = $"[{json}]";
                    using var doc = JsonDocument.Parse(json);
                    foreach (var el in doc.RootElement.EnumerateArray())
                        enabledGuids.Add(el.GetString() ?? "");
                }

                foreach (var (name, guid) in AsrRuleDefs)
                    list.Add(new AsrRule(name, guid, enabledGuids.Contains(guid)));

            }
            catch (Exception e) { Logger.Warn($"GetAsrRules : {e.Message}"); }
            return list;
        }

        public void SetAsrRule(string guid, bool enable)
        {
            string action = enable ? "Enabled" : "Disabled";
            var psi = new ProcessStartInfo("powershell",
                $"-NoProfile -Command \"Add-MpPreference -AttackSurfaceReductionRules_Ids '{guid}' -AttackSurfaceReductionRules_Actions {action}\"")
            { UseShellExecute = false, CreateNoWindow = true };
            using var p = Process.Start(psi)!;
            p.WaitForExit();
            Logger.Action($"ASR rule {guid} : {action}");
        }

        public void EnableAllAsrRules()
        {
            foreach (var (_, guid) in AsrRuleDefs) SetAsrRule(guid, true);
            Logger.Action("Toutes les regles ASR activees");
        }

        // ─── Exploit Protection ───────────────────────────────────────────────
        public record ExploitProtectionStatus(bool DepEnabled, bool AslrEnabled, bool SeheEnabled);

        public ExploitProtectionStatus GetExploitProtection()
        {
            try
            {
                var psi = new ProcessStartInfo("powershell",
                    "-NoProfile -Command \"$e = Get-ProcessMitigation -System; '{0}|{1}|{2}' -f $e.DEP.Enable,$e.ASLR.ForceRelocateImages,$e.SEHOP.Enable\"")
                { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true };
                using var p = Process.Start(psi)!;
                string o = p.StandardOutput.ReadToEnd().Trim();
                p.WaitForExit();
                var parts = o.Split('|');
                bool dep  = parts.Length > 0 && parts[0].Trim().Equals("ON", StringComparison.OrdinalIgnoreCase);
                bool aslr = parts.Length > 1 && parts[1].Trim().Equals("ON", StringComparison.OrdinalIgnoreCase);
                bool seh  = parts.Length > 2 && parts[2].Trim().Equals("ON", StringComparison.OrdinalIgnoreCase);
                return new ExploitProtectionStatus(dep, aslr, seh);
            }
            catch { return new ExploitProtectionStatus(false, false, false); }
        }

        public void SetExploitProtection(bool enable)
        {
            // Active les protections systeme via Set-ProcessMitigation
            string state = enable ? "ON" : "OFF";
            var psi = new ProcessStartInfo("powershell",
                $"-NoProfile -Command \"Set-ProcessMitigation -System -Enable DEP,ForceRelocateImages,SEHOP\"")
            { UseShellExecute = false, CreateNoWindow = true };
            if (!enable)
                psi = new ProcessStartInfo("powershell",
                    "-NoProfile -Command \"Set-ProcessMitigation -System -Disable DEP,ForceRelocateImages,SEHOP\"")
                { UseShellExecute = false, CreateNoWindow = true };
            using var p = Process.Start(psi)!;
            p.WaitForExit();
            Logger.Action($"Exploit Protection systeme : {state}");
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