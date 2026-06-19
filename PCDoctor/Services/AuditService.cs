using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;

namespace PCDoctor.Services
{
    public class AuditRow
    {
        public string Col1 { get; set; } = "";
        public string Col2 { get; set; } = "";
        public string Col3 { get; set; } = "";
        public string Col4 { get; set; } = "";
    }

    public class AuditResult
    {
        public string Title    { get; set; } = "";
        public string Subtitle { get; set; } = "";
        public string H1       { get; set; } = "";
        public string H2       { get; set; } = "";
        public string H3       { get; set; } = "";
        public string H4       { get; set; } = "";
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
                var json = RunPs(
                    "@(Get-WmiObject Win32_Service | Select-Object DisplayName, Name, State, StartMode, PathName) | ConvertTo-Json -Compress");
                int running = 0;
                foreach (var el in ParseArray(json))
                {
                    var path = Str(el, "PathName");
                    if (path.Contains(@"\Windows\", StringComparison.OrdinalIgnoreCase) ||
                        path.Contains("Microsoft",   StringComparison.OrdinalIgnoreCase)) continue;
                    var state = Str(el, "State");
                    if (state == "Running") running++;
                    r.Rows.Add(new AuditRow
                    {
                        Col1 = Str(el, "DisplayName") is { Length: > 0 } d ? d : Str(el, "Name"),
                        Col2 = state,
                        Col3 = Str(el, "StartMode"),
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
                var json = RunPs(
                    "@(Get-WmiObject Win32_SystemDriver | Select-Object Name, State, StartMode, PathName) | ConvertTo-Json -Compress");
                int orphans = 0;
                foreach (var el in ParseArray(json))
                {
                    var path = Str(el, "PathName").Replace(@"\??\", "").Trim('"');
                    bool isMs = path.Contains("Microsoft", StringComparison.OrdinalIgnoreCase) ||
                                path.Contains(@"\Windows\System32\drivers\", StringComparison.OrdinalIgnoreCase);
                    bool exists = !string.IsNullOrEmpty(path) && System.IO.File.Exists(path);
                    if (isMs && exists) continue;
                    if (!exists) orphans++;
                    r.Rows.Add(new AuditRow
                    {
                        Col1 = Str(el, "Name"),
                        Col2 = Str(el, "State"),
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
                var json = RunPs(
                    "Get-MpComputerStatus | Select-Object RealTimeProtectionEnabled, AntivirusEnabled, AntispywareEnabled, AntivirusSignatureVersion | ConvertTo-Json -Compress");
                using var doc = JsonDocument.Parse(json);
                var o = doc.RootElement;
                r.Rows.Add(new AuditRow { Col1 = "Protection temps réel", Col2 = Bool(o, "RealTimeProtectionEnabled") ? "Activée" : "Désactivée" });
                r.Rows.Add(new AuditRow { Col1 = "Antivirus",              Col2 = Bool(o, "AntivirusEnabled")          ? "Activé"  : "Désactivé" });
                r.Rows.Add(new AuditRow { Col1 = "Anti-spyware",           Col2 = Bool(o, "AntispywareEnabled")        ? "Activée" : "Désactivée" });
                r.Rows.Add(new AuditRow { Col1 = "Version définitions",    Col2 = Str(o, "AntivirusSignatureVersion") });
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
                var sysJson = RunPs(
                    "Get-WmiObject Win32_ComputerSystem | Select-Object Manufacturer, Model | ConvertTo-Json -Compress");
                using var sys = JsonDocument.Parse(sysJson);
                var s = sys.RootElement;
                r.Rows.Add(new AuditRow { Col1 = "Fabricant", Col2 = Str(s, "Manufacturer") });
                r.Rows.Add(new AuditRow { Col1 = "Modèle",    Col2 = Str(s, "Model") });

                var cpuJson = RunPs(
                    "Get-WmiObject Win32_Processor | Select-Object -First 1 -ExpandProperty Name");
                r.Rows.Add(new AuditRow { Col1 = "Processeur", Col2 = cpuJson.Trim() });

                r.Rows.Add(new AuditRow { Col1 = "Nom machine", Col2 = Environment.MachineName });
                r.Rows.Add(new AuditRow { Col1 = "Utilisateur", Col2 = Environment.UserName });
                r.Subtitle = "Configuration matérielle";
            }
            catch (Exception e) { r.Subtitle = "Erreur : " + e.Message; }
            return r;
        }

        // ─── Helpers ───

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

        private static IEnumerable<JsonElement> ParseArray(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) yield break;
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement.Clone();
            if (root.ValueKind == JsonValueKind.Array)
                foreach (var el in root.EnumerateArray()) yield return el;
            else
                yield return root;
        }

        private static string Str(JsonElement el, string prop) =>
            el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String
                ? v.GetString() ?? "" : "";

        private static bool Bool(JsonElement el, string prop) =>
            el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.True;
    }
}
