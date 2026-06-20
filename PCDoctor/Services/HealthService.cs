using System;
using System.Collections.Generic;
using Microsoft.Win32;

namespace PCDoctor.Services
{
    public class HealthCheck
    {
        public string Label    { get; }
        public string Category { get; }
        public bool   IsOk     { get; }
        public string PageTag  { get; }
        public string Advice   { get; }
        public string Icon          => IsOk ? "✅" : "⚠️";
        public bool   IsClickable   => !IsOk && !string.IsNullOrEmpty(PageTag);
        public double ArrowOpacity  => IsClickable ? 0.5 : 0.0;

        public HealthCheck(string label, string category, bool isOk, string pageTag, string advice)
        {
            Label = label; Category = category; IsOk = isOk; PageTag = pageTag; Advice = advice;
        }
    }

    public class HealthService
    {
        public List<HealthCheck> GetChecks()
        {
            var list = new List<HealthCheck>();

            // ─── Sécurité ──────────────────────────────────────────────────
            list.Add(new(
                "LLMNR désactivé",
                "Sécurité",
                GetDword(@"SOFTWARE\Policies\Microsoft\Windows NT\DNSClient", "EnableMulticast") == 0,
                "HardeningPage",
                "LLMNR expose au relay attack sur les réseaux locaux."));

            list.Add(new(
                "Protection PUA (SmartScreen) active",
                "Sécurité",
                IsSmartScreenPuaEnabled(),
                "HardeningPage",
                "La protection PUA bloque les logiciels potentiellement indésirables."));

            list.Add(new(
                "mDNS désactivé",
                "Sécurité",
                GetDword(@"SYSTEM\CurrentControlSet\Services\Dnscache\Parameters", "EnableMDNS") == 0,
                "HardeningPage",
                "mDNS élargit la surface d'attaque sur les réseaux non fiables."));

            // ─── Confidentialité ───────────────────────────────────────────
            list.Add(new(
                "Télémétrie désactivée",
                "Confidentialité",
                GetDword(@"SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry") == 0,
                "PrivacyPage",
                "La télémétrie envoie des données d'usage à Microsoft."));

            list.Add(new(
                "Windows Recall désactivé",
                "Confidentialité",
                GetDwordHkcu(@"Software\Policies\Microsoft\Windows\WindowsAI", "DisableAIDataAnalysis") == 1,
                "PrivacyPage",
                "Recall capture votre écran en continu pour l'IA."));

            list.Add(new(
                "Wi-Fi Sense désactivé",
                "Confidentialité",
                GetDword(@"SOFTWARE\Microsoft\WcmSvc\wifinetworkmanager\config", "AutoConnectAllowedOEM") == 0,
                "PrivacyPage",
                "Wi-Fi Sense partage vos mots de passe Wi-Fi avec vos contacts."));

            list.Add(new(
                "Advertising ID désactivé",
                "Confidentialité",
                GetDwordHkcu(@"Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo", "Enabled") == 0,
                "PrivacyPage",
                "L'Advertising ID permet le suivi publicitaire cross-app."));

            list.Add(new(
                "Activity History désactivé",
                "Confidentialité",
                GetDword(@"SOFTWARE\Policies\Microsoft\Windows\System", "EnableActivityFeed") == 0,
                "PrivacyPage",
                "Activity History enregistre vos activités et les synchronise dans le cloud."));

            list.Add(new(
                "Localisation désactivée",
                "Confidentialité",
                IsLocationDisabled(),
                "PrivacyPage",
                "La localisation permet aux apps de connaître votre position."));

            // ─── Explorateur ───────────────────────────────────────────────
            list.Add(new(
                "Extensions de fichiers visibles",
                "Explorateur",
                GetDwordHkcu(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "HideFileExt") == 0,
                "ExplorerPage",
                "Masquer les extensions facilite les arnaques (fichier.pdf.exe)."));

            list.Add(new(
                "Fast Startup désactivé",
                "Optimisations",
                GetDword(@"SYSTEM\CurrentControlSet\Control\Session Manager\Power", "HiberbootEnabled") == 0,
                "OptimPage",
                "Fast Startup peut empêcher l'application de certaines mises à jour."));

            return list;
        }

        // ─── Helpers ──────────────────────────────────────────────────────

        private static bool IsSmartScreenPuaEnabled()
        {
            // Cherche dans les deux emplacements : GPO (Policies) puis Defender direct
            foreach (var path in new[]
            {
                @"SOFTWARE\Policies\Microsoft\Windows Defender\MpEngine",
                @"SOFTWARE\Microsoft\Windows Defender\MpEngine",
            })
            {
                try
                {
                    using var key = Registry.LocalMachine.OpenSubKey(path);
                    var v = key?.GetValue("MpEnablePus");
                    if (v is int i) return i == 1;
                }
                catch { }
            }
            return false;
        }

        private static bool IsLocationDisabled()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\location");
                var val = key?.GetValue("Value") as string;
                return val != null && val.Equals("Deny", StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        }

        private static int? GetDword(string keyPath, string name)
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(keyPath);
                var v = key?.GetValue(name);
                return v == null ? (int?)null : (int)v;
            }
            catch { return null; }
        }

        private static int? GetDwordHkcu(string keyPath, string name)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(keyPath);
                var v = key?.GetValue(name);
                return v == null ? (int?)null : (int)v;
            }
            catch { return null; }
        }
    }
}
