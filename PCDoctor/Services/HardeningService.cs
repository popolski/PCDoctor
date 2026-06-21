using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;

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

        // ─── LLMNR ───
        public bool IsLlmnrActive()
        {
            var v = RegistryHelper.GetDword(LlmnrKey, "EnableMulticast");
            return v != 0;
        }
        public void SetLlmnr(bool active)
        {
            if (active)
                RegistryHelper.DeleteValueHklm(LlmnrKey, "EnableMulticast");
            else
                RegistryHelper.SetDwordHklm(LlmnrKey, "EnableMulticast", 0);
        }

        // ─── SMBv1 ─── via dism.exe
        public bool IsSmb1Active()
        {
            try
            {
                string output = RunExe("dism.exe", "/Online /Get-FeatureInfo /FeatureName:SMB1Protocol");
                return output.Contains("State : Enabled", StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        }
        public void SetSmb1(bool active)
        {
            string verb = active ? "/Enable-Feature" : "/Disable-Feature";
            RunExeNoOutput("dism.exe", $"/Online {verb} /FeatureName:SMB1Protocol /NoRestart");
        }

        // ─── BitLocker ─── via WMI Win32_EncryptableVolume
        public List<BitLockerDrive> GetBitLockerStatus()
        {
            var list = new List<BitLockerDrive>();
            try
            {
                var scope = new ManagementScope(@"\\.\ROOT\CIMV2\Security\MicrosoftVolumeEncryption");
                scope.Connect();
                using var searcher = new ManagementObjectSearcher(scope,
                    new ObjectQuery("SELECT DriveLetter, ProtectionStatus, ConversionStatus, EncryptionMethod FROM Win32_EncryptableVolume"));
                foreach (ManagementObject obj in searcher.Get())
                {
                    string drive  = obj["DriveLetter"]?.ToString() ?? "?";
                    uint   prot   = (uint)(obj["ProtectionStatus"] ?? 0u);
                    uint   conv   = (uint)(obj["ConversionStatus"] ?? 0u);
                    uint   method = (uint)(obj["EncryptionMethod"]  ?? 0u);
                    list.Add(new BitLockerDrive(drive, ConversionStatusLabel(conv), EncMethodLabel(method), prot == 1));
                }
            }
            catch (Exception e) { Logger.Warn($"GetBitLockerStatus : {e.Message}"); }
            return list;
        }

        private static string ConversionStatusLabel(uint s) => s switch
        {
            0 => "Non chiffré",
            1 => "Chiffré",
            2 => "Chiffrement en cours",
            3 => "Déchiffrement en cours",
            _ => "État inconnu"
        };
        private static string EncMethodLabel(uint m) => m switch
        {
            3 => "AES-128", 4 => "AES-256",
            6 => "XTS-AES-128", 7 => "XTS-AES-256",
            0 => "Aucun",
            _ => $"Méthode {m}"
        };

        // ─── ASR Rules ─── via WMI MSFT_MpPreference
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
            var enabled = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                var scope = DefenderScope();
                using var searcher = new ManagementObjectSearcher(scope,
                    new ObjectQuery("SELECT AttackSurfaceReductionRules_Ids FROM MSFT_MpPreference"));
                foreach (ManagementObject obj in searcher.Get())
                {
                    if (obj["AttackSurfaceReductionRules_Ids"] is string[] ids)
                        foreach (var id in ids) enabled.Add(id);
                }
            }
            catch (Exception e) { Logger.Warn($"GetAsrRules : {e.Message}"); }
            foreach (var (name, guid) in AsrRuleDefs)
                list.Add(new AsrRule(name, guid, enabled.Contains(guid)));
            return list;
        }

        public void SetAsrRule(string guid, bool enable)
        {
            try
            {
                var scope = DefenderScope();
                using var cls = new ManagementClass(scope, new ManagementPath("MSFT_MpPreference"), null);
                var inParams = cls.GetMethodParameters("Add");
                inParams["AttackSurfaceReductionRules_Ids"]     = new[] { guid };
                inParams["AttackSurfaceReductionRules_Actions"] = new[] { enable ? 1u : 0u };
                cls.InvokeMethod("Add", inParams, null);
                Logger.Action($"ASR rule {guid} : {(enable ? "activée" : "désactivée")}");
            }
            catch (Exception e) { Logger.Warn($"SetAsrRule : {e.Message}"); }
        }

        public void EnableAllAsrRules()
        {
            foreach (var (_, guid) in AsrRuleDefs) SetAsrRule(guid, true);
            Logger.Action("Toutes les règles ASR activées");
        }

        // ─── Exploit Protection ─── (PowerShell maintenu : bits de mitigation complexes)
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
                return new ExploitProtectionStatus(
                    parts.Length > 0 && parts[0].Trim().Equals("ON", StringComparison.OrdinalIgnoreCase),
                    parts.Length > 1 && parts[1].Trim().Equals("ON", StringComparison.OrdinalIgnoreCase),
                    parts.Length > 2 && parts[2].Trim().Equals("ON", StringComparison.OrdinalIgnoreCase));
            }
            catch { return new ExploitProtectionStatus(false, false, false); }
        }

        public void SetExploitProtection(bool enable)
        {
            string flags = "DEP,ForceRelocateImages,SEHOP";
            string verb  = enable ? "Enable" : "Disable";
            var psi = new ProcessStartInfo("powershell",
                $"-NoProfile -Command \"Set-ProcessMitigation -System -{verb} {flags}\"")
            { UseShellExecute = false, CreateNoWindow = true };
            using var p = Process.Start(psi)!;
            p.WaitForExit();
            Logger.Action($"Exploit Protection système : {(enable ? "activée" : "désactivée")}");
        }

        // ─── mDNS / Bonjour ───
        public bool IsMdnsActive()
        {
            var v = RegistryHelper.GetDword(@"SYSTEM\CurrentControlSet\Services\Dnscache\Parameters", "EnableMDNS");
            return v != 0;
        }
        public bool IsBonjourInstalled()
        {
            try
            {
                string o = RunExe("sc.exe", "query \"Bonjour Service\"");
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
            Logger.Action($"mDNS Windows {(active ? "réactivé" : "désactivé")}");
        }
        public string DisableBonjour()
        {
            try
            {
                RunExeNoOutput("sc.exe", "stop \"Bonjour Service\"");
                RunExeNoOutput("sc.exe", "config \"Bonjour Service\" start= disabled");
                Logger.Action("Service Bonjour désactivé");
                return "Service Bonjour désactivé (arrêté et démarrage mis à Désactivé).";
            }
            catch (Exception e) { return $"Erreur : {e.Message}"; }
        }

        // ─── Defender : signatures ─── via WMI MSFT_MpComputerStatus
        public (string version, string date) GetDefenderSignatureInfo()
        {
            try
            {
                var scope = DefenderScope();
                using var searcher = new ManagementObjectSearcher(scope,
                    new ObjectQuery("SELECT AntivirusSignatureVersion, AntivirusSignatureLastUpdated FROM MSFT_MpComputerStatus"));
                foreach (ManagementObject obj in searcher.Get())
                {
                    string ver  = obj["AntivirusSignatureVersion"]?.ToString() ?? "?";
                    string raw  = obj["AntivirusSignatureLastUpdated"]?.ToString() ?? "";
                    string date = "?";
                    if (!string.IsNullOrEmpty(raw))
                        try { date = ManagementDateTimeConverter.ToDateTime(raw).ToString("dd/MM/yyyy HH:mm"); }
                        catch { date = raw; }
                    return (ver, date);
                }
            }
            catch (Exception e) { Logger.Warn($"GetDefenderSignatureInfo : {e.Message}"); }
            return ("?", "?");
        }

        public string UpdateDefenderSignatures()
        {
            try
            {
                string mpCmd = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "Windows Defender", "MpCmdRun.exe");
                var psi = new ProcessStartInfo(mpCmd, "-SignatureUpdate")
                    { UseShellExecute = false, CreateNoWindow = true };
                using var p = Process.Start(psi)!;
                p.WaitForExit();
                Logger.Action("Signatures Defender mises à jour");
                if (p.ExitCode == 0)
                {
                    var (ver, date) = GetDefenderSignatureInfo();
                    return $"Signatures mises à jour — v{ver} ({date})";
                }
                return $"Mise à jour terminée avec le code {p.ExitCode}.";
            }
            catch (Exception e) { return $"Erreur : {e.Message}"; }
        }

        // ─── Scan rapide Defender ─── via MpCmdRun.exe
        public string StartQuickScan()
        {
            try
            {
                string mpCmd = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "Windows Defender", "MpCmdRun.exe");
                var psi = new ProcessStartInfo(mpCmd, "-Scan -ScanType 1")
                    { UseShellExecute = false, CreateNoWindow = true };
                using var p = Process.Start(psi)!;
                p.WaitForExit();
                Logger.Action("Scan rapide Defender lancé (MpCmdRun)");
                return p.ExitCode == 0
                    ? "Scan rapide lancé — visible dans Sécurité Windows."
                    : $"MpCmdRun.exe a retourné le code {p.ExitCode}.";
            }
            catch (Exception e) { return $"Erreur : {e.Message}"; }
        }

        // ─── PUA Protection ─── via WMI MSFT_MpPreference
        public bool IsPuaActive()
        {
            try
            {
                var scope = DefenderScope();
                using var searcher = new ManagementObjectSearcher(scope,
                    new ObjectQuery("SELECT PUAProtection FROM MSFT_MpPreference"));
                foreach (ManagementObject obj in searcher.Get())
                    return (uint)(obj["PUAProtection"] ?? 0u) == 1u;
            }
            catch (Exception e) { Logger.Warn($"IsPuaActive : {e.Message}"); }
            return false;
        }

        public bool SetPua(bool active)
        {
            try
            {
                var scope = DefenderScope();
                using var cls = new ManagementClass(scope, new ManagementPath("MSFT_MpPreference"), null);
                var inParams = cls.GetMethodParameters("Set");
                inParams["PUAProtection"] = active ? 1u : 0u;
                cls.InvokeMethod("Set", inParams, null);
            }
            catch (Exception e) { Logger.Warn($"SetPua : {e.Message}"); }
            bool ok = IsPuaActive() == active;
            Logger.Action($"PUA Protection : {(active ? "activée" : "désactivée")}{(ok ? "" : " [ECHEC VERIFICATION]")}");
            return ok;
        }

        // ─── Helpers ─────────────────────────────────────────────────────────
        private static ManagementScope DefenderScope()
        {
            var scope = new ManagementScope(@"\\.\ROOT\Microsoft\Windows\Defender");
            scope.Connect();
            return scope;
        }

        private static string RunExe(string exe, string args)
        {
            var psi = new ProcessStartInfo(exe, args)
                { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true };
            using var p = Process.Start(psi)!;
            string o = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            return o;
        }

        private static void RunExeNoOutput(string exe, string args)
        {
            var psi = new ProcessStartInfo(exe, args)
                { UseShellExecute = false, CreateNoWindow = true };
            using var p = Process.Start(psi);
            p?.WaitForExit();
        }
    }
}
