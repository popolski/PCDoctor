using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace PCDoctor.Services
{
    public class WingetPackage
    {
        public string Name      { get; set; } = "";
        public string Id        { get; set; } = "";
        public string Version   { get; set; } = "";
        public string Available { get; set; } = "";
        public string Source    { get; set; } = "";
        public bool   IsSelected { get; set; } = false;
    }

    public class WingetService
    {
public bool IsAvailable()
        {
            try
            {
                var psi = new ProcessStartInfo("winget", "--version")
                    { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true };
                using var p = Process.Start(psi);
                p?.WaitForExit(5_000);
                return p?.ExitCode == 0;
            }
            catch { return false; }
        }

        // Retourne la liste des mises à jour disponibles
        public List<WingetPackage> GetUpdates()
        {
            var result = new List<WingetPackage>();
            try
            {
                var output = RunWinget("upgrade --include-unknown --accept-source-agreements");
                result = ParseTable(output);
                Logger.Info($"Winget updates : {result.Count} disponible(s)");
            }
            catch (Exception ex) { Logger.Error($"Winget GetUpdates : {ex.Message}"); }
            return result;
        }

        // Met à jour un seul paquet avec feedback ligne par ligne
        public int UpdatePackage(string id, Action<string> onLine)
        {
            return RunWingetLive(
                $"upgrade --id \"{id}\" --accept-source-agreements --accept-package-agreements --silent",
                onLine);
        }

        // Met à jour tous les paquets avec feedback ligne par ligne
        public int UpdateAll(Action<string> onLine)
        {
            return RunWingetLive(
                "upgrade --all --accept-source-agreements --accept-package-agreements",
                onLine);
        }

        // ─── Parseur ───

        private static List<WingetPackage> ParseTable(string output)
        {
            var result  = new List<WingetPackage>();
            int[] cols  = Array.Empty<int>(); // positions de début de chaque colonne
            bool inData = false;

            foreach (var raw in output.Split('\n'))
            {
                // Nettoyer les \r et les caractères de progression du spinner (\r ... \r)
                var line = StripCarriageReturns(raw).TrimEnd();

                // Ignorer les lignes vides ou trop courtes
                if (string.IsNullOrWhiteSpace(line)) continue;

                // Détecter l'en-tête : contient "Nom" ou "Name" ET "Source"
                if (!inData && (line.Contains("Nom ") || line.Contains("Name ")) && line.Contains("Source"))
                {
                    cols = DetectColumnPositions(line);
                    continue;
                }

                // Début des données : ligne de tirets
                if (!inData && cols.Length > 0 && Regex.IsMatch(line, @"^-{10,}"))
                {
                    inData = true;
                    continue;
                }
                if (!inData) continue;

                // Lignes à ignorer après le début
                if (Regex.IsMatch(line, @"^\s*\d+\s+")) continue; // "1 mises à niveau..."
                if (line.Length < 10) continue;

                var parts = ExtractColumns(line, cols);
                if (parts.Length < 4) continue;

                var name = parts[0].Trim();
                var id   = parts[1].Trim();
                if (string.IsNullOrWhiteSpace(id) || id is "Id" or "ID") continue;

                result.Add(new WingetPackage
                {
                    Name      = name,
                    Id        = id,
                    Version   = parts[2].Trim(),
                    Available = parts[3].Trim(),
                    Source    = parts.Length > 4 ? parts[4].Trim() : ""
                });
            }

            return result;
        }

        // Détecte les positions de colonnes depuis la ligne d'en-tête
        private static int[] DetectColumnPositions(string header)
        {
            // Cherche les indices des mots-clés de colonnes
            var keywords = new[] { "Nom", "Name", "Id", "ID", "Version", "Disponible", "Available", "Source" };
            var positions = new System.Collections.Generic.List<int>();
            int i = 0;
            while (i < header.Length)
            {
                if (header[i] != ' ')
                {
                    // Début d'un mot
                    int start = i;
                    while (i < header.Length && header[i] != ' ') i++;
                    var word = header[start..i];
                    foreach (var kw in keywords)
                    {
                        if (word.Equals(kw, StringComparison.OrdinalIgnoreCase))
                        {
                            positions.Add(start);
                            break;
                        }
                    }
                }
                else i++;
            }
            return positions.ToArray();
        }

        // Extrait les valeurs de colonnes depuis une ligne selon les positions détectées
        private static string[] ExtractColumns(string line, int[] colStarts)
        {
            if (colStarts.Length == 0) return Array.Empty<string>();
            var result = new string[colStarts.Length];
            for (int i = 0; i < colStarts.Length; i++)
            {
                int start = Math.Min(colStarts[i], line.Length);
                int end   = i + 1 < colStarts.Length ? Math.Min(colStarts[i + 1], line.Length) : line.Length;
                result[i] = start < end ? line[start..end] : "";
            }
            return result;
        }

        // Le spinner winget écrit plusieurs frames sur la même ligne \n via \r.
        // On split sur \r et on prend le DERNIER segment non-vide.
        private static string StripCarriageReturns(string line)
        {
            var segments = line.Split('\r');
            for (int i = segments.Length - 1; i >= 0; i--)
            {
                if (!string.IsNullOrWhiteSpace(segments[i]))
                    return segments[i];
            }
            return "";
        }

        // ─── Helpers process ───

        private static string RunWinget(string args)
        {
            var psi = new ProcessStartInfo("winget", args)
            {
                UseShellExecute        = false,
                CreateNoWindow         = true,
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8
            };
            using var p = Process.Start(psi)!;
            string o = p.StandardOutput.ReadToEnd();
            p.WaitForExit(120_000);
            return o;
        }

        private static int RunWingetLive(string args, Action<string> onLine)
        {
            var psi = new ProcessStartInfo("winget", args)
            {
                UseShellExecute        = false,
                CreateNoWindow         = true,
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8
            };
            using var p = Process.Start(psi)!;
            p.OutputDataReceived += (_, e) => { if (e.Data != null) onLine(e.Data); };
            p.ErrorDataReceived  += (_, e) => { if (e.Data != null) onLine(e.Data); };
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();
            p.WaitForExit(300_000); // 5 min max
            return p.ExitCode;
        }
    }
}
