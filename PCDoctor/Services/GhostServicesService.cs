using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace PCDoctor.Services
{
    public class GhostService
    {
        public string Name        { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string ImagePath   { get; set; } = "";
        public bool   IsSelected  { get; set; } = false;
    }

    public class GhostServicesService
    {
        // Chemins système qui ne peuvent jamais être "fantômes"
        private static readonly string[] SystemPathFragments = new[]
        {
            "svchost.exe", "system32", "syswow64",
            "\\Windows\\", "wininit", "lsass", "services.exe"
        };

        public List<GhostService> Scan()
        {
            var result = new List<GhostService>();

            // PowerShell + JSON pour éviter le conflit STA/MTA
            var json = RunPsOutput(
                "@(Get-CimInstance Win32_Service | Where-Object { $_.PathName } | " +
                "Select-Object Name, DisplayName, PathName) | ConvertTo-Json -Compress");

            if (string.IsNullOrWhiteSpace(json)) return result;

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var elements = root.ValueKind == JsonValueKind.Array
                ? root.EnumerateArray()
                : (IEnumerable<JsonElement>)new[] { root };

            foreach (var el in elements)
            {
                var name        = el.TryGetProperty("Name",        out var n) ? n.GetString() ?? "" : "";
                var displayName = el.TryGetProperty("DisplayName", out var d) ? d.GetString() ?? "" : "";
                var pathName    = el.TryGetProperty("PathName",    out var p) ? p.GetString() ?? "" : "";

                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(pathName))
                    continue;

                // Garde-fou 1 : liste noire SafetyGuard (Wellbia, BattlEye, Vanguard...)
                if (SafetyGuard.IsProtectedService(name)) continue;

                // Garde-fou 2 : chemins système — jamais fantômes
                if (ContainsSystemPath(pathName)) continue;

                // Garde-fou 3 : chemin dans un emplacement éditeur connu
                if (SafetyGuard.IsSafeLocation(pathName)) continue;

                // Extraire le chemin de l'exécutable (gérer les guillemets et args)
                var exe = ExtractExePath(pathName);
                if (string.IsNullOrWhiteSpace(exe)) continue;

                // Garde-fou 4 : double vérification chemin système sur l'exe extrait
                if (ContainsSystemPath(exe)) continue;
                if (SafetyGuard.IsSafeLocation(exe)) continue;

                // Fantôme : binaire réellement absent du disque
                if (File.Exists(exe)) continue; // fichier présent = service normal

                result.Add(new GhostService
                {
                    Name        = name,
                    DisplayName = displayName,
                    ImagePath   = exe
                });
            }

            Logger.Info($"Services fantômes : {result.Count} trouvé(s)");
            return result;
        }

        public (int ok, int err) Delete(IEnumerable<GhostService> services)
        {
            int ok = 0, err = 0;
            foreach (var svc in services)
            {
                // Vérification SafetyGuard au moment de la suppression (double filet)
                if (SafetyGuard.IsProtectedService(svc.Name))
                {
                    Logger.Warn($"Suppression refusée (protégé) : {svc.Name}");
                    err++;
                    continue;
                }

                try
                {
                    var psi = new ProcessStartInfo("sc.exe", $"delete \"{svc.Name}\"")
                    {
                        UseShellExecute        = false,
                        CreateNoWindow         = true,
                        RedirectStandardOutput = true
                    };
                    using var p = Process.Start(psi);
                    p?.WaitForExit(10_000);
                    Logger.Action($"Service fantôme supprimé : {svc.Name} ({svc.DisplayName})");
                    ok++;
                }
                catch (Exception ex)
                {
                    Logger.Error($"Suppression {svc.Name} : {ex.Message}");
                    err++;
                }
            }
            return (ok, err);
        }

        // ─── Helpers ───

        private static string ExtractExePath(string pathName)
        {
            pathName = pathName.Trim();
            if (pathName.StartsWith("\""))
            {
                var end = pathName.IndexOf('"', 1);
                return end > 1 ? pathName[1..end] : "";
            }
            // Sans guillemets : prendre jusqu'au premier espace suivi d'un tiret ou d'une barre
            var spaceIdx = pathName.IndexOf(' ');
            return spaceIdx < 0 ? pathName : pathName[..spaceIdx];
        }

        private static bool ContainsSystemPath(string path)
        {
            foreach (var fragment in SystemPathFragments)
                if (path.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            return false;
        }

        private static string RunPsOutput(string cmd)
        {
            var psi = new ProcessStartInfo("powershell",
                $"-NoProfile -NonInteractive -Command \"{cmd}\"")
            {
                RedirectStandardOutput = true,
                UseShellExecute        = false,
                CreateNoWindow         = true
            };
            using var p = Process.Start(psi);
            string o = p!.StandardOutput.ReadToEnd();
            p.WaitForExit();
            return o.Trim();
        }
    }
}
