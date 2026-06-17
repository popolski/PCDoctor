using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;

namespace PCDoctor.Services
{
    public class ProcessInfo
    {
        public string Name { get; set; } = "";
        public string MemoryText { get; set; } = "";
        public double MemoryMb { get; set; }
    }

    public class MonitorService
    {
        private PerformanceCounter? _cpuCounter;

        public MonitorService()
        {
            try
            {
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                _cpuCounter.NextValue(); // 1ère lecture toujours 0, on l'amorce
            }
            catch { _cpuCounter = null; }
        }

        // CPU global en % (temps réel)
        public int GetCpuPercent()
        {
            try { return _cpuCounter != null ? (int)Math.Round(_cpuCounter.NextValue()) : 0; }
            catch { return 0; }
        }

        // RAM : (utilisée Go, total Go, pourcentage)
        public (double used, double total, int pct) GetRam()
        {
            try
            {
                using var s = new ManagementObjectSearcher("SELECT TotalVisibleMemorySize, FreePhysicalMemory FROM Win32_OperatingSystem");
                foreach (var o in s.Get())
                {
                    double totalKb = Convert.ToDouble(o["TotalVisibleMemorySize"]);
                    double freeKb = Convert.ToDouble(o["FreePhysicalMemory"]);
                    double totalGb = Math.Round(totalKb / 1024 / 1024, 1);
                    double usedGb = Math.Round((totalKb - freeKb) / 1024 / 1024, 1);
                    int pct = (int)Math.Round((totalKb - freeKb) / totalKb * 100);
                    return (usedGb, totalGb, pct);
                }
            }
            catch { }
            return (0, 0, 0);
        }

        // Infos détaillées statiques
        public Dictionary<string, string> GetDetails()
        {
            var d = new Dictionary<string, string>();
            try
            {
                using (var s = new ManagementObjectSearcher("SELECT Name, NumberOfCores, NumberOfLogicalProcessors FROM Win32_Processor"))
                    foreach (var o in s.Get())
                    {
                        d["Processeur"] = o["Name"]?.ToString() ?? "";
                        d["Cœurs"] = $"{o["NumberOfCores"]} cœurs / {o["NumberOfLogicalProcessors"]} threads";
                    }
                using (var s = new ManagementObjectSearcher("SELECT Manufacturer, Product FROM Win32_BaseBoard"))
                    foreach (var o in s.Get())
                        d["Carte mère"] = $"{o["Manufacturer"]} {o["Product"]}";
                using (var s = new ManagementObjectSearcher("SELECT SMBIOSBIOSVersion FROM Win32_BIOS"))
                    foreach (var o in s.Get())
                        d["BIOS"] = o["SMBIOSBIOSVersion"]?.ToString() ?? "";
                using (var s = new ManagementObjectSearcher("SELECT Name, AdapterRAM FROM Win32_VideoController"))
                    foreach (var o in s.Get())
                        d["Carte graphique"] = o["Name"]?.ToString() ?? "";
                using (var s = new ManagementObjectSearcher("SELECT Caption, Version FROM Win32_OperatingSystem"))
                    foreach (var o in s.Get())
                        d["Système"] = $"{o["Caption"]} (build {o["Version"]})";
            }
            catch (Exception e) { Logger.Warn($"GetDetails : {e.Message}"); }
            return d;
        }

        // Uptime réel basé sur LastBootUpTime (fiable même avec Fast Startup)
        public string GetUptime()
        {
            try
            {
                using var s = new ManagementObjectSearcher("SELECT LastBootUpTime FROM Win32_OperatingSystem");
                foreach (var o in s.Get())
                {
                    string raw = o["LastBootUpTime"]?.ToString() ?? "";
                    var boot = ManagementDateTimeConverter.ToDateTime(raw);
                    var up = DateTime.Now - boot;
                    if (up.TotalDays >= 1)
                        return $"{up.Days}j {up.Hours}h {up.Minutes}min";
                    return $"{up.Hours}h {up.Minutes}min";
                }
            }
            catch (Exception e) { Logger.Warn($"GetUptime : {e.Message}"); }
            return "Inconnu";
        }

        // Top processus par mémoire
        public List<ProcessInfo> GetTopProcesses(int count = 10)
        {
            var list = new List<ProcessInfo>();
            try
            {
                var procs = Process.GetProcesses()
                    .Select(p => { try { return new ProcessInfo { Name = p.ProcessName, MemoryMb = Math.Round(p.WorkingSet64 / 1024.0 / 1024, 1) }; } catch { return null; } })
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
    }
}