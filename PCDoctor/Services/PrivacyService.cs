using System;
using System.Diagnostics;
using Microsoft.Win32;

namespace PCDoctor.Services
{
    public class PrivacyService
    {
        // ─── Télémétrie Windows ───
        public bool IsTelemetryActive()
        {
            var v = RegistryHelper.GetDword(@"SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry");
            return v != 0;
        }
        public void SetTelemetry(bool active)
        {
            if (active)
            {
                RegistryHelper.DeleteValueHklm(@"SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry");
                RunPs("Set-Service DiagTrack -StartupType Automatic; Start-Service DiagTrack");
            }
            else
            {
                RegistryHelper.SetDwordHklm(@"SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry", 0);
                RunPs("Stop-Service DiagTrack -Force; Set-Service DiagTrack -StartupType Disabled");
            }
        }

        // ─── Cortana ───
        public bool IsCortanaActive()
        {
            var v = RegistryHelper.GetDword(@"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "AllowCortana");
            return v != 0;
        }
        public void SetCortana(bool active)
        {
            if (active)
                RegistryHelper.DeleteValueHklm(@"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "AllowCortana");
            else
                RegistryHelper.SetDwordHklm(@"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "AllowCortana", 0);
        }

        // ─── Activity History ───
        public bool IsActivityActive()
        {
            var v = RegistryHelper.GetDword(@"SOFTWARE\Policies\Microsoft\Windows\System", "EnableActivityFeed");
            return v != 0;
        }
        public void SetActivity(bool active)
        {
            if (active)
                RegistryHelper.DeleteValueHklm(@"SOFTWARE\Policies\Microsoft\Windows\System", "EnableActivityFeed");
            else
            {
                RegistryHelper.SetDwordHklm(@"SOFTWARE\Policies\Microsoft\Windows\System", "EnableActivityFeed", 0);
                RegistryHelper.SetDwordHklm(@"SOFTWARE\Policies\Microsoft\Windows\System", "PublishUserActivities", 0);
            }
        }

        // ─── Pubs / suggestions (HKCU) ───
        public bool IsAdsActive()
        {
            var v = GetDwordHkcu(@"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SystemPaneSuggestionsEnabled");
            return v != 0;
        }
        public void SetAds(bool active)
        {
            int val = active ? 1 : 0;
            SetDwordHkcu(@"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SystemPaneSuggestionsEnabled", val);
            SetDwordHkcu(@"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SubscribedContent-338388Enabled", val);
        }

        // ─── Advertising ID (HKCU) ───
        public bool IsAdIdActive()
        {
            var v = GetDwordHkcu(@"Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo", "Enabled");
            return v != 0;
        }
        public void SetAdId(bool active)
        {
            SetDwordHkcu(@"Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo", "Enabled", active ? 1 : 0);
        }

        // ─── Télémétrie Office (HKCU) ───
        public bool IsOfficeActive()
        {
            var v = GetDwordHkcu(@"Software\Policies\Microsoft\Office\16.0\Common", "SendCustomerData");
            return v != 0;
        }
        public void SetOffice(bool active)
        {
            SetDwordHkcu(@"Software\Policies\Microsoft\Office\16.0\Common", "SendCustomerData", active ? 1 : 0);
        }

        // ─── Helpers HKCU ───
        private int? GetDwordHkcu(string keyPath, string valueName)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(keyPath);
                var v = key?.GetValue(valueName);
                if (v == null) return null;
                return (int)v;
            }
            catch { return null; }
        }
        private void SetDwordHkcu(string keyPath, string valueName, int value)
        {
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(keyPath);
                key?.SetValue(valueName, value, RegistryValueKind.DWord);
            }
            catch { }
        }

        private void RunPs(string cmd)
        {
            try
            {
                var psi = new ProcessStartInfo("powershell", $"-NoProfile -Command \"{cmd}\"")
                { UseShellExecute = false, CreateNoWindow = true };
                using var p = Process.Start(psi);
                p!.WaitForExit();
            }
            catch { }
        }
    }
}