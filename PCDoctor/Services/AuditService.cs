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

        // Extensions shell (menus contextuels, overlay icons...)
        public AuditResult AuditShellExtensions()
        {
            var r = new AuditResult { Title = "Extensions shell", H1 = "Nom", H2 = "CLSID", H3 = "Fichier", H4 = "Type" };
            try
            {
                // Lit HKLM\Software\Microsoft\Windows\CurrentVersion\Shell Extensions\Approved
                var json = RunPs(
                    "@(Get-ItemProperty 'HKLM:\\Software\\Microsoft\\Windows\\CurrentVersion\\Shell Extensions\\Approved' | " +
                    "Get-Member -MemberType NoteProperty | Where-Object { $_.Name -match '^\\{' } | ForEach-Object {" +
                    "  $clsid = $_.Name;" +
                    "  $name = (Get-ItemProperty 'HKLM:\\Software\\Microsoft\\Windows\\CurrentVersion\\Shell Extensions\\Approved').$clsid;" +
                    "  $path = try { (Get-ItemProperty \"HKLM:\\Software\\Classes\\CLSID\\$clsid\\InProcServer32\" -ErrorAction Stop).'(default)' } catch { '' };" +
                    "  [pscustomobject]@{ Clsid=$clsid; Name=$name; Path=$path }" +
                    "}) | ConvertTo-Json -Compress");
                int ms = 0;
                foreach (var el in ParseArray(json))
                {
                    var path = Str(el, "Path");
                    bool isMs = path.Contains(@"\Windows\", StringComparison.OrdinalIgnoreCase) ||
                                path.Contains("Microsoft",  StringComparison.OrdinalIgnoreCase);
                    if (isMs) { ms++; continue; }
                    r.Rows.Add(new AuditRow
                    {
                        Col1 = Str(el, "Name"),
                        Col2 = Str(el, "Clsid"),
                        Col3 = string.IsNullOrEmpty(path) ? "Orpheline" : (System.IO.File.Exists(path) ? "OK" : "Manquant"),
                        Col4 = path
                    });
                }
                r.Subtitle = $"{r.Rows.Count} extensions tierces ({ms} Microsoft masquées)";
            }
            catch (Exception e) { r.Subtitle = "Erreur : " + e.Message; }
            return r;
        }

        // Grands fichiers (>100 Mo)
        public AuditResult AuditLargeFiles()
        {
            var r = new AuditResult { Title = "Grands fichiers (> 100 Mo)", H1 = "Fichier", H2 = "Taille", H3 = "Modifié", H4 = "Dossier" };
            try
            {
                var json = RunPs(
                    "@(Get-PSDrive -PSProvider FileSystem | Where-Object { $_.Used -ne $null } | ForEach-Object {" +
                    "  Get-ChildItem -Path \"$($_.Root)Users\" -Recurse -File -ErrorAction SilentlyContinue " +
                    "    | Where-Object { $_.Length -gt 104857600 } " +
                    "    | Select-Object Name, Length, LastWriteTime, DirectoryName" +
                    "} | Sort-Object Length -Descending | Select-Object -First 50) | ConvertTo-Json -Compress");
                long total = 0;
                foreach (var el in ParseArray(json))
                {
                    long bytes = el.TryGetProperty("Length", out var lv) ? lv.GetInt64() : 0;
                    total += bytes;
                    string size = bytes >= 1_073_741_824
                        ? $"{bytes / 1_073_741_824.0:F1} Go"
                        : $"{bytes / 1_048_576.0:F0} Mo";
                    string date = "";
                    if (el.TryGetProperty("LastWriteTime", out var dv) && dv.ValueKind == JsonValueKind.String)
                        date = dv.GetString()?.Substring(0, 10) ?? "";
                    r.Rows.Add(new AuditRow
                    {
                        Col1 = Str(el, "Name"),
                        Col2 = size,
                        Col3 = date,
                        Col4 = Str(el, "DirectoryName")
                    });
                }
                string totalStr = total >= 1_073_741_824
                    ? $"{total / 1_073_741_824.0:F1} Go"
                    : $"{total / 1_048_576.0:F0} Mo";
                r.Subtitle = $"{r.Rows.Count} fichiers (total {totalStr})";
            }
            catch (Exception e) { r.Subtitle = "Erreur : " + e.Message; }
            return r;
        }

        // Extensions navigateurs (Chrome & Edge)
        public AuditResult AuditBrowserExtensions()
        {
            var r = new AuditResult { Title = "Extensions navigateurs", H1 = "Extension", H2 = "Version", H3 = "Navigateur", H4 = "ID" };
            try
            {
                string userRoot = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var browsers = new[]
                {
                    ("Chrome", System.IO.Path.Combine(userRoot, @"Google\Chrome\User Data\Default\Extensions")),
                    ("Edge",   System.IO.Path.Combine(userRoot, @"Microsoft\Edge\User Data\Default\Extensions")),
                    ("Brave",  System.IO.Path.Combine(userRoot, @"BraveSoftware\Brave-Browser\User Data\Default\Extensions")),
                };
                foreach (var (browser, extRoot) in browsers)
                {
                    if (!System.IO.Directory.Exists(extRoot)) continue;
                    foreach (var extDir in System.IO.Directory.GetDirectories(extRoot))
                    {
                        string extId = System.IO.Path.GetFileName(extDir);
                        if (extId is "Temp" or "temp") continue;
                        // Cherche le manifest.json dans la version la plus récente
                        string name = extId, version = "";
                        foreach (var verDir in System.IO.Directory.GetDirectories(extDir))
                        {
                            var manifest = System.IO.Path.Combine(verDir, "manifest.json");
                            if (!System.IO.File.Exists(manifest)) continue;
                            try
                            {
                                var text = System.IO.File.ReadAllText(manifest);
                                using var doc = JsonDocument.Parse(text);
                                var root = doc.RootElement;
                                if (root.TryGetProperty("name", out var nv)) name = nv.GetString() ?? extId;
                                if (root.TryGetProperty("version", out var vv)) version = vv.GetString() ?? "";
                                // Résoudre les locales __MSG_xxx__
                                if (name.StartsWith("__MSG_")) name = extId;
                            }
                            catch { }
                        }
                        r.Rows.Add(new AuditRow { Col1 = name, Col2 = version, Col3 = browser, Col4 = extId });
                    }
                }
                r.Subtitle = r.Rows.Count == 0
                    ? "Aucune extension trouvée (Chrome/Edge/Brave)"
                    : $"{r.Rows.Count} extension(s) installée(s)";
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
