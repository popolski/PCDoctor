using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Win32;

namespace PCDoctor.Services
{
    public class SystemInfoService
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct MEMORYSTATUSEX
        {
            public uint  dwLength;
            public uint  dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

        [DllImport("kernel32.dll")]
        private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

        public string GetOsName()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
                return key?.GetValue("ProductName")?.ToString() ?? "Windows";
            }
            catch { return "Windows"; }
        }

        public (double total, double used, int pct) GetRam()
        {
            try
            {
                var ms = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
                if (!GlobalMemoryStatusEx(ref ms)) return (0, 0, 0);
                double total = Math.Round(ms.ullTotalPhys / 1024.0 / 1024 / 1024, 1);
                double avail = Math.Round(ms.ullAvailPhys / 1024.0 / 1024 / 1024, 1);
                double used  = Math.Round(total - avail, 1);
                return (total, used, (int)ms.dwMemoryLoad);
            }
            catch { return (0, 0, 0); }
        }

        public string GetMachineName() => Environment.MachineName;

        public List<DiskInfo> GetDisks()
        {
            var list = new List<DiskInfo>();
            try
            {
                foreach (var d in DriveInfo.GetDrives())
                {
                    if (!d.IsReady || d.DriveType != DriveType.Fixed) continue;
                    double totalGb = Math.Round(d.TotalSize / 1024.0 / 1024 / 1024, 1);
                    double freeGb  = Math.Round(d.TotalFreeSpace / 1024.0 / 1024 / 1024, 1);
                    double usedGb  = Math.Round(totalGb - freeGb, 1);
                    int    pct     = totalGb > 0 ? (int)Math.Round(usedGb / totalGb * 100) : 0;
                    list.Add(new DiskInfo
                    {
                        Letter  = d.Name.TrimEnd('\\'),
                        Text    = $"{usedGb} Go / {totalGb} Go ({freeGb} Go libres)",
                        Percent = pct
                    });
                }
            }
            catch { }
            return list;
        }

        public (bool rtp, bool av, string text) GetDefender()
        {
            try
            {
                var json = RunPs(
                    "Get-MpComputerStatus | Select-Object RealTimeProtectionEnabled, AntivirusEnabled | ConvertTo-Json -Compress");
                using var doc = JsonDocument.Parse(json);
                var o = doc.RootElement;
                bool rtp = o.TryGetProperty("RealTimeProtectionEnabled", out var r) && r.ValueKind == JsonValueKind.True;
                bool av  = o.TryGetProperty("AntivirusEnabled",          out var a) && a.ValueKind == JsonValueKind.True;
                return (rtp, av, (rtp && av) ? "Protection active" : "Verifiez la protection");
            }
            catch { return (false, false, "Etat inconnu"); }
        }

        public string GetUptime()
        {
            try
            {
                var up = TimeSpan.FromMilliseconds(Environment.TickCount64);
                return up.TotalDays >= 1
                    ? $"{up.Days}j {up.Hours}h {up.Minutes}min"
                    : $"{up.Hours}h {up.Minutes}min";
            }
            catch { return "Inconnu"; }
        }

        private static string RunPs(string cmd)
        {
            var psi = new ProcessStartInfo("powershell",
                $"-NoProfile -NonInteractive -Command \"{cmd}\"")
            {
                RedirectStandardOutput = true,
                UseShellExecute        = false,
                CreateNoWindow         = true
            };
            using var p = Process.Start(psi)!;
            var o = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            return o.Trim();
        }
    }

    public class DiskInfo
    {
        public string Letter  { get; set; } = "";
        public string Text    { get; set; } = "";
        public int    Percent { get; set; }
    }
}
