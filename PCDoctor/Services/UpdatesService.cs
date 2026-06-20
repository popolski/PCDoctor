using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace PCDoctor.Services
{
    public class UpdatesService
    {
        public string GetLastInstalledUpdate()
        {
            try
            {
                var output = RunPsOutput(
                    "Get-HotFix | Where-Object { $_.InstalledOn } | " +
                    "Sort-Object InstalledOn -Descending | Select-Object -First 1 | " +
                    "ForEach-Object { $_.InstalledOn.ToString() + '  -  ' + $_.HotFixID }");
                var r = output.Trim();
                return string.IsNullOrEmpty(r)
                    ? "Non disponible (voir Historique Windows Update)"
                    : r;
            }
            catch { return "Non disponible"; }
        }

        public bool IsRebootRequired()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired");
                return key != null;
            }
            catch { return false; }
        }

        public async Task<int> GetPendingUpdatesCountAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    var output = RunPsOutput(
                        "$s = New-Object -ComObject Microsoft.Update.Session; " +
                        "$s.CreateUpdateSearcher().Search('IsInstalled=0 and IsHidden=0').Updates.Count");
                    return int.TryParse(output.Trim(), out int n) ? n : -1;
                }
                catch { return -1; }
            });
        }

        // ─── Bloquer les MAJ de fonctionnalités ───────────────────────────────
        // Fige Windows sur la version actuelle via TargetReleaseVersion (GPO)
        private const string WuPolicy = @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate";

        public bool IsFeatureUpdatesBlocked()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(WuPolicy);
                return key?.GetValue("TargetReleaseVersion") is int i && i == 1;
            }
            catch { return false; }
        }

        public void SetBlockFeatureUpdates(bool block)
        {
            try
            {
                if (block)
                {
                    string version = GetCurrentWinRelease();
                    using var key = Registry.LocalMachine.CreateSubKey(WuPolicy);
                    key?.SetValue("TargetReleaseVersion",     1,         RegistryValueKind.DWord);
                    key?.SetValue("TargetReleaseVersionInfo", version,   RegistryValueKind.String);
                    key?.SetValue("ProductVersion",           "Windows 10", RegistryValueKind.String);
                }
                else
                {
                    using var key = Registry.LocalMachine.OpenSubKey(WuPolicy, writable: true);
                    key?.DeleteValue("TargetReleaseVersion",     throwOnMissingValue: false);
                    key?.DeleteValue("TargetReleaseVersionInfo", throwOnMissingValue: false);
                    key?.DeleteValue("ProductVersion",           throwOnMissingValue: false);
                }
            }
            catch { }
        }

        private static string GetCurrentWinRelease()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
                return key?.GetValue("DisplayVersion") as string
                    ?? key?.GetValue("ReleaseId")       as string
                    ?? "22H2";
            }
            catch { return "22H2"; }
        }

        // ─── Pause des MAJ (4 semaines qualité, 52 semaines fonctionnalités) ─
        private const string WuUxKey = @"SOFTWARE\Microsoft\WindowsUpdate\UX\Settings";

        public bool IsUpdatesPaused()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(WuUxKey);
                var s = key?.GetValue("PauseQualityUpdatesStartTime") as string;
                if (string.IsNullOrEmpty(s)) return false;
                // Vérifier que la pause est encore dans le futur
                using var key2 = Registry.LocalMachine.OpenSubKey(WuUxKey);
                var end = key2?.GetValue("PauseQualityUpdatesEndTime") as string;
                return !string.IsNullOrEmpty(end) && DateTime.TryParse(end, out var dt) && dt > DateTime.UtcNow;
            }
            catch { return false; }
        }

        public void SetPauseUpdates(bool pause)
        {
            try
            {
                using var key = Registry.LocalMachine.CreateSubKey(WuUxKey);
                if (pause)
                {
                    string now = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
                    string end4w  = DateTime.UtcNow.AddDays(28).ToString("yyyy-MM-ddTHH:mm:ssZ");
                    string end52w = DateTime.UtcNow.AddDays(365).ToString("yyyy-MM-ddTHH:mm:ssZ");
                    key?.SetValue("PauseQualityUpdatesStartTime",   now,   RegistryValueKind.String);
                    key?.SetValue("PauseQualityUpdatesEndTime",     end4w, RegistryValueKind.String);
                    key?.SetValue("PauseFeatureUpdatesStartTime",   now,   RegistryValueKind.String);
                    key?.SetValue("PauseFeatureUpdatesEndTime",     end52w,RegistryValueKind.String);
                    key?.SetValue("PauseUpdatesExpiryTime",         end4w, RegistryValueKind.String);
                }
                else
                {
                    foreach (var v in new[] {
                        "PauseQualityUpdatesStartTime", "PauseQualityUpdatesEndTime",
                        "PauseFeatureUpdatesStartTime", "PauseFeatureUpdatesEndTime",
                        "PauseUpdatesExpiryTime" })
                        key?.DeleteValue(v, throwOnMissingValue: false);
                }
            }
            catch { }
        }

        public void OpenWindowsUpdate() =>
            Process.Start(new ProcessStartInfo("ms-settings:windowsupdate") { UseShellExecute = true });

        public void OpenHistory() =>
            Process.Start(new ProcessStartInfo("ms-settings:windowsupdate-history") { UseShellExecute = true });

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
