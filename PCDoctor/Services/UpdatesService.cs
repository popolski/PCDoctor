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
