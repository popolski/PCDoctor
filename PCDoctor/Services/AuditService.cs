using System;
using System.Collections.Generic;
using System.Management;

namespace PCDoctor.Services
{
    // Une ligne d'audit générique (s'adapte à tous les types de résultats)
    public class AuditRow
    {
        public string Col1 { get; set; } = "";
        public string Col2 { get; set; } = "";
        public string Col3 { get; set; } = "";
        public string Col4 { get; set; } = "";
    }

    // Résultat complet d'un audit : titre, en-têtes de colonnes, lignes
    public class AuditResult
    {
        public string Title { get; set; } = "";
        public string Subtitle { get; set; } = "";
        public string H1 { get; set; } = "";
        public string H2 { get; set; } = "";
        public string H3 { get; set; } = "";
        public string H4 { get; set; } = "";
        public List<AuditRow> Rows { get; set; } = new();
    }

    public class AuditService
    {
        // Services tiers (non-Microsoft)
        public AuditResult AuditServices()
        {
            var r = new AuditResult { Title = "Services tiers", H1 = "Nom", H2 = "État", H3 = "Démarrage", H4 = "Chemin" };
            try
            {
                using var s = new ManagementObjectSearcher("SELECT Name, DisplayName, State, StartMode, PathName FROM Win32_Service");
                int running = 0;
                foreach (var o in s.Get())
                {
                    string path = o["PathName"]?.ToString() ?? "";
                    if (path.Contains(@"\Windows\", StringComparison.OrdinalIgnoreCase) ||
                        path.Contains("Microsoft", StringComparison.OrdinalIgnoreCase)) continue;
                    string state = o["State"]?.ToString() ?? "";
                    if (state == "Running") running++;
                    r.Rows.Add(new AuditRow
                    {
                        Col1 = o["DisplayName"]?.ToString() ?? o["Name"]?.ToString() ?? "",
                        Col2 = state,
                        Col3 = o["StartMode"]?.ToString() ?? "",
                        Col4 = path
                    });
                }
                r.Subtitle = $"{r.Rows.Count} services tiers ({running} actifs)";
            }
            catch (Exception e) { r.Subtitle = "Erreur : " + e.Message; }
            return r;
        }

        // Drivers tiers ou orphelins
        public AuditResult AuditDrivers()
        {
            var r = new AuditResult { Title = "Drivers tiers ou suspects", H1 = "Nom", H2 = "État", H3 = "Binaire", H4 = "Chemin" };
            try
            {
                using var s = new ManagementObjectSearcher("SELECT Name, State, StartMode, PathName FROM Win32_SystemDriver");
                int orphans = 0;
                foreach (var o in s.Get())
                {
                    string path = (o["PathName"]?.ToString() ?? "")
                        .Replace(@"\??\", "").Trim('"');
                    bool isMs = path.Contains("Microsoft", StringComparison.OrdinalIgnoreCase) ||
                                path.Contains(@"\Windows\System32\drivers\", StringComparison.OrdinalIgnoreCase);
                    bool exists = !string.IsNullOrEmpty(path) && System.IO.File.Exists(path);
                    if (isMs && exists) continue;
                    if (!exists) orphans++;
                    r.Rows.Add(new AuditRow
                    {
                        Col1 = o["Name"]?.ToString() ?? "",
                        Col2 = o["State"]?.ToString() ?? "",
                        Col3 = exists ? "OK" : "Orphelin",
                        Col4 = path
                    });
                }
                r.Subtitle = $"{r.Rows.Count} drivers ({orphans} orphelin(s))";
            }
            catch (Exception e) { r.Subtitle = "Erreur : " + e.Message; }
            return r;
        }

        // État de Windows Defender
        public AuditResult AuditDefender()
        {
            var r = new AuditResult { Title = "Windows Defender", H1 = "Paramètre", H2 = "État", H3 = "", H4 = "" };
            try
            {
                var scope = new ManagementScope(@"\\.\root\Microsoft\Windows\Defender");
                scope.Connect();
                using var s = new ManagementObjectSearcher(scope, new ObjectQuery("SELECT * FROM MSFT_MpComputerStatus"));
                foreach (var o in s.Get())
                {
                    r.Rows.Add(new AuditRow { Col1 = "Protection temps réel", Col2 = Convert.ToBoolean(o["RealTimeProtectionEnabled"]) ? "Activée" : "Désactivée" });
                    r.Rows.Add(new AuditRow { Col1 = "Antivirus", Col2 = Convert.ToBoolean(o["AntivirusEnabled"]) ? "Activé" : "Désactivé" });
                    r.Rows.Add(new AuditRow { Col1 = "Protection anti-spyware", Col2 = Convert.ToBoolean(o["AntispywareEnabled"]) ? "Activée" : "Désactivée" });
                    r.Rows.Add(new AuditRow { Col1 = "Version définitions", Col2 = o["AntivirusSignatureVersion"]?.ToString() ?? "?" });
                }
                r.Subtitle = "État de la protection";
            }
            catch (Exception e) { r.Subtitle = "Erreur : " + e.Message; }
            return r;
        }

        // Infos système
        public AuditResult AuditSystemInfo()
        {
            var r = new AuditResult { Title = "Informations système", H1 = "Élément", H2 = "Valeur", H3 = "", H4 = "" };
            try
            {
                using var s = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem");
                foreach (var o in s.Get())
                {
                    r.Rows.Add(new AuditRow { Col1 = "Fabricant", Col2 = o["Manufacturer"]?.ToString() ?? "" });
                    r.Rows.Add(new AuditRow { Col1 = "Modèle", Col2 = o["Model"]?.ToString() ?? "" });
                }
                using var cpu = new ManagementObjectSearcher("SELECT Name FROM Win32_Processor");
                foreach (var o in cpu.Get())
                    r.Rows.Add(new AuditRow { Col1 = "Processeur", Col2 = o["Name"]?.ToString() ?? "" });
                r.Rows.Add(new AuditRow { Col1 = "Nom machine", Col2 = Environment.MachineName });
                r.Rows.Add(new AuditRow { Col1 = "Utilisateur", Col2 = Environment.UserName });
                r.Subtitle = "Configuration matérielle";
            }
            catch (Exception e) { r.Subtitle = "Erreur : " + e.Message; }
            return r;
        }
    }
}