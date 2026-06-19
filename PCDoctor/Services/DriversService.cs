using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;

namespace PCDoctor.Services
{
    public class DriverItem
    {
        public string DeviceName { get; set; } = "";
        public string Provider   { get; set; } = "";
        public string Version    { get; set; } = "";
        public string DateStr    { get; set; } = "";
        public string Note       { get; set; } = "";
        public bool   IsOld      { get; set; }
    }

    public class DriversService
    {
        public List<DriverItem> GetDrivers()
        {
            var result = new List<DriverItem>();
            try
            {
                // System.Management + Task.Run = probleme STA/MTA -> on passe par PowerShell
                var json = RunPsOutput(
                    "@(Get-WmiObject Win32_PnPSignedDriver | Where-Object { $_.DeviceName } | " +
                    "Select-Object DeviceName, DriverProvider, DriverVersion, DriverDate) | " +
                    "ConvertTo-Json -Compress");

                if (string.IsNullOrWhiteSpace(json)) return result;

                using var doc = JsonDocument.Parse(json);
                var elements = doc.RootElement.ValueKind == JsonValueKind.Array
                    ? doc.RootElement.EnumerateArray()
                    : (IEnumerable<JsonElement>)new[] { doc.RootElement };

                var cutoff = DateTime.Today.AddYears(-3);

                foreach (var el in elements)
                {
                    var name     = GetStr(el, "DeviceName");
                    if (string.IsNullOrWhiteSpace(name)) continue;

                    var provider = GetStr(el, "DriverProvider");
                    var version  = GetStr(el, "DriverVersion");
                    var dateRaw  = GetStr(el, "DriverDate");

                    DateTime? date = ParseDmtfDate(dateRaw);
                    bool isOld = date.HasValue && date.Value < cutoff;

                    result.Add(new DriverItem
                    {
                        DeviceName = name,
                        Provider   = provider,
                        Version    = version,
                        DateStr    = date.HasValue ? date.Value.ToString("dd/MM/yyyy") : "",
                        IsOld      = isOld,
                        Note       = isOld ? "Ancien (>3 ans)" : ""
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"DriversService.GetDrivers : {ex.Message}");
            }
            return result;
        }

        public void OpenDeviceManager() =>
            Process.Start(new ProcessStartInfo("devmgmt.msc") { UseShellExecute = true });

        public void OpenOptionalUpdates() =>
            Process.Start(new ProcessStartInfo("ms-settings:windowsupdate-optionalupdates")
                { UseShellExecute = true });

        private static string GetStr(JsonElement el, string prop)
        {
            if (!el.TryGetProperty(prop, out var v)) return "";
            return v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";
        }

        // Format DMTF : YYYYMMDDHHMMSS.ffffff+UUU  (parfois avec * a la place)
        private static DateTime? ParseDmtfDate(string dmtf)
        {
            if (string.IsNullOrEmpty(dmtf) || dmtf.Length < 8) return null;
            try
            {
                int y  = int.Parse(dmtf.Substring(0, 4));
                int mo = int.Parse(dmtf.Substring(4, 2));
                int d  = int.Parse(dmtf.Substring(6, 2));
                if (y < 1980 || mo < 1 || mo > 12 || d < 1 || d > 31) return null;
                return new DateTime(y, mo, d);
            }
            catch { return null; }
        }

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
