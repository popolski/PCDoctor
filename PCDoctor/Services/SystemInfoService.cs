using System;
using System.Management;

namespace PCDoctor.Services
{
    // Logique métier pure : récupère les infos système. Réutilisable, testable.
    public class SystemInfoService
    {
        public string GetOsName()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT Caption FROM Win32_OperatingSystem");
                foreach (var o in searcher.Get())
                    return o["Caption"]?.ToString() ?? "Inconnu";
            }
            catch { }
            return "Inconnu";
        }

        public (double total, double used, int pct) GetRam()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT TotalVisibleMemorySize, FreePhysicalMemory FROM Win32_OperatingSystem");
                foreach (var o in searcher.Get())
                {
                    double totalKb = Convert.ToDouble(o["TotalVisibleMemorySize"]);
                    double freeKb = Convert.ToDouble(o["FreePhysicalMemory"]);
                    double totalGb = Math.Round(totalKb / 1024 / 1024, 1);
                    double usedGb = Math.Round((totalKb - freeKb) / 1024 / 1024, 1);
                    int pct = (int)Math.Round((totalKb - freeKb) / totalKb * 100);
                    return (totalGb, usedGb, pct);
                }
            }
            catch { }
            return (0, 0, 0);
        }

        public string GetMachineName() => Environment.MachineName;
        public System.Collections.Generic.List<DiskInfo> GetDisks()
        {
            var list = new System.Collections.Generic.List<DiskInfo>();
            try
            {
                foreach (var d in System.IO.DriveInfo.GetDrives())
                {
                    if (!d.IsReady || d.DriveType != System.IO.DriveType.Fixed) continue;
                    double totalGb = Math.Round(d.TotalSize / 1024.0 / 1024 / 1024, 1);
                    double freeGb = Math.Round(d.TotalFreeSpace / 1024.0 / 1024 / 1024, 1);
                    double usedGb = Math.Round(totalGb - freeGb, 1);
                    int pct = totalGb > 0 ? (int)Math.Round(usedGb / totalGb * 100) : 0;
                    list.Add(new DiskInfo
                    {
                        Letter = d.Name.TrimEnd('\\'),
                        Text = $"{usedGb} Go / {totalGb} Go ({freeGb} Go libres)",
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
                var scope = new ManagementScope(@"\\.\root\Microsoft\Windows\Defender");
                scope.Connect();
                var query = new ObjectQuery("SELECT * FROM MSFT_MpComputerStatus");
                using var searcher = new ManagementObjectSearcher(scope, query);
                foreach (var o in searcher.Get())
                {
                    bool rtp = Convert.ToBoolean(o["RealTimeProtectionEnabled"]);
                    bool av = Convert.ToBoolean(o["AntivirusEnabled"]);
                    string t = (rtp && av) ? "Protection active" : "Vérifiez la protection";
                    return (rtp, av, t);
                }
            }
            catch { }
            return (false, false, "État inconnu");
        }

        public string GetUptime()
        {
            try
            {
                var ms = (ulong)Environment.TickCount64;
                var ts = TimeSpan.FromMilliseconds(ms);
                return $"{ts.Days}j {ts.Hours}h {ts.Minutes}min";
            }
            catch { return "Inconnu"; }
        }
    }

    // Petite classe pour représenter un disque
    public class DiskInfo
    {
        public string Letter { get; set; } = "";
        public string Text { get; set; } = "";
        public int Percent { get; set; }
    }
}