using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace PCDoctor.Services
{
    public class ProcessInfo
    {
        public string Name      { get; set; } = "";
        public string MemoryText { get; set; } = "";
        public double MemoryMb  { get; set; }
    }

    public class MonitorService
    {
        private PerformanceCounter? _cpuCounter;

        // P/Invoke pour la RAM — rapide, sans WMI
        [StructLayout(LayoutKind.Sequential)]
        private struct MEMORYSTATUSEX
        {
            public uint   dwLength;
            public uint   dwMemoryLoad;
            public ulong  ullTotalPhys;
            public ulong  ullAvailPhys;
            public ulong  ullTotalPageFile;
            public ulong  ullAvailPageFile;
            public ulong  ullTotalVirtual;
            public ulong  ullAvailVirtual;
            public ulong  ullAvailExtendedVirtual;
        }

        [DllImport("kernel32.dll")]
        private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

        public MonitorService()
        {
            try
            {
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                _cpuCounter.NextValue(); // 1ère lecture toujours 0, on l'amorce
            }
            catch { _cpuCounter = null; }
        }

        public int GetCpuPercent()
        {
            try { return _cpuCounter != null ? (int)Math.Round(_cpuCounter.NextValue()) : 0; }
            catch { return 0; }
        }

        public (double used, double total, int pct) GetRam()
        {
            try
            {
                var ms = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
                if (!GlobalMemoryStatusEx(ref ms)) return (0, 0, 0);
                double total = Math.Round(ms.ullTotalPhys / 1024.0 / 1024 / 1024, 1);
                double avail = Math.Round(ms.ullAvailPhys / 1024.0 / 1024 / 1024, 1);
                double used  = Math.Round(total - avail, 1);
                int    pct   = (int)ms.dwMemoryLoad;
                return (used, total, pct);
            }
            catch { return (0, 0, 0); }
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

        // Infos détaillées (appelé une seule fois à l'ouverture de la page)
        public Dictionary<string, string> GetDetails()
        {
            var d = new Dictionary<string, string>();
            try
            {
                var json = RunPs(
                    "@(Get-WmiObject Win32_Processor | Select-Object -First 1 Name, NumberOfCores, NumberOfLogicalProcessors) | ConvertTo-Json -Compress");
                using var doc = JsonDocument.Parse(json);
                var cpu = doc.RootElement.ValueKind == JsonValueKind.Array
                    ? doc.RootElement[0] : doc.RootElement;
                d["Processeur"] = Str(cpu, "Name");
                d["Coeurs"]     = $"{Str(cpu, "NumberOfCores")} coeurs / {Str(cpu, "NumberOfLogicalProcessors")} threads";
            }
            catch { }

            try
            {
                var json = RunPs("Get-WmiObject Win32_BaseBoard | Select-Object Manufacturer, Product | ConvertTo-Json -Compress");
                using var doc = JsonDocument.Parse(json);
                var mb = doc.RootElement;
                d["Carte mere"] = $"{Str(mb, "Manufacturer")} {Str(mb, "Product")}".Trim();
            }
            catch { }

            try
            {
                d["BIOS"] = RunPs("Get-WmiObject Win32_BIOS | Select-Object -ExpandProperty SMBIOSBIOSVersion").Trim();
            }
            catch { }

            try
            {
                // Exclure les adaptateurs virtuels (Sunshine, parsec, VirtualBox, etc.)
                d["Carte graphique"] = RunPs(
                    "Get-WmiObject Win32_VideoController | " +
                    "Where-Object { $_.Name -notmatch 'Virtual|Parsec|IddSample|sudoMaker|Mirror|Remote' } | " +
                    "Select-Object -First 1 -ExpandProperty Name").Trim();
            }
            catch { }

            try
            {
                var json = RunPs("Get-WmiObject Win32_OperatingSystem | Select-Object Caption, Version | ConvertTo-Json -Compress");
                using var doc = JsonDocument.Parse(json);
                var os = doc.RootElement;
                d["Systeme"] = $"{Str(os, "Caption")} (build {Str(os, "Version")})";
            }
            catch { }

            try
            {
                var raw = RunPs(
                    "Get-WmiObject MSAcpi_ThermalZoneTemperature -Namespace root/wmi | " +
                    "Select-Object -First 1 -ExpandProperty CurrentTemperature");
                if (int.TryParse(raw.Trim(), out int tenthsK) && tenthsK > 0)
                    d["Température CPU"] = $"{(tenthsK - 2732) / 10.0:F0} °C (approx.)";
            }
            catch { }

            return d;
        }

        public List<ProcessInfo> GetTopProcesses(int count = 10)
        {
            var list = new List<ProcessInfo>();
            try
            {
                var procs = Process.GetProcesses()
                    .Select(p =>
                    {
                        try { return new ProcessInfo { Name = p.ProcessName, MemoryMb = Math.Round(p.WorkingSet64 / 1024.0 / 1024, 1) }; }
                        catch { return null; }
                    })
                    .Where(x => x != null && x.MemoryMb > 0)
                    .OrderByDescending(x => x!.MemoryMb)
                    .Take(count);
                foreach (var p in procs)
                {
                    p!.MemoryText = $"{p.MemoryMb} Mo";
                    list.Add(p);
                }
            }
            catch (Exception e) { Logger.Warn($"GetTopProcesses : {e.Message}"); }
            return list;
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

        private static string Str(JsonElement el, string prop) =>
            el.TryGetProperty(prop, out var v) ? v.ToString() : "";
    }
}
