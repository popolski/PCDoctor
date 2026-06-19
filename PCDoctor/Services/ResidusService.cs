using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;

namespace PCDoctor.Services
{
    public enum ResiduType { Folder, RegistryKey }

    public class ResiduItem
    {
        public string    Path        { get; set; } = "";
        public ResiduType Type       { get; set; }
        public string    SizeStr     { get; set; } = "";
        public bool      IsSelected  { get; set; } = false;
        public string    TypeLabel   => Type == ResiduType.Folder ? "Dossier" : "Registre";

        // Utilisés pour la suppression (meme assembly)
        public string RegHiveName   { get; set; } = "";   // "HKLM" ou "HKCU"
        public string RegParent     { get; set; } = "";   // ex : "SOFTWARE"
        public string RegKeyName    { get; set; } = "";   // ex : "Firefox"
    }

    public class ResidusService
    {
        private static readonly string[] FolderRoots =
        {
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        };

        // Scan : recherche par mot-clé dans les emplacements typiques
        public List<ResiduItem> Scan(string keyword)
        {
            var result = new List<ResiduItem>();
            if (string.IsNullOrWhiteSpace(keyword)) return result;

            // Dossiers
            foreach (var root in FolderRoots)
            {
                if (!Directory.Exists(root)) continue;
                try
                {
                    foreach (var dir in Directory.GetDirectories(root, $"*{keyword}*", SearchOption.TopDirectoryOnly))
                    {
                        if (SafetyGuard.IsProtectedResidu(dir)) continue;
                        result.Add(new ResiduItem
                        {
                            Path    = dir,
                            Type    = ResiduType.Folder,
                            SizeStr = GetFolderSizeStr(dir)
                        });
                    }
                }
                catch { }
            }

            // Registre
            ScanRegHive("HKLM", Registry.LocalMachine, @"SOFTWARE",                keyword, result);
            ScanRegHive("HKLM", Registry.LocalMachine, @"SOFTWARE\WOW6432Node",    keyword, result);
            ScanRegHive("HKCU", Registry.CurrentUser,  @"SOFTWARE",                keyword, result);

            Logger.Info($"Résidus '{keyword}' : {result.Count} élément(s) trouvé(s)");
            return result;
        }

        // Suppression des éléments sélectionnés
        public (int ok, int err) Delete(IEnumerable<ResiduItem> items)
        {
            int ok = 0, err = 0;
            foreach (var item in items)
            {
                try
                {
                    if (item.Type == ResiduType.Folder)
                    {
                        Directory.Delete(item.Path, recursive: true);
                        Logger.Action($"Résidu supprimé (dossier) : {item.Path}");
                        ok++;
                    }
                    else
                    {
                        var hive = item.RegHiveName == "HKCU"
                            ? Registry.CurrentUser
                            : Registry.LocalMachine;
                        using var parent = hive.OpenSubKey(item.RegParent, writable: true);
                        parent?.DeleteSubKeyTree(item.RegKeyName, throwOnMissingSubKey: false);
                        Logger.Action($"Résidu supprimé (registre) : {item.Path}");
                        ok++;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"Suppression résidu {item.Path} : {ex.Message}");
                    err++;
                }
            }
            return (ok, err);
        }

        // ─── Helpers ───
        private static void ScanRegHive(string hiveName, RegistryKey hive, string parent,
            string keyword, List<ResiduItem> result)
        {
            try
            {
                using var key = hive.OpenSubKey(parent);
                if (key == null) return;
                foreach (var sub in key.GetSubKeyNames())
                {
                    if (sub.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) < 0) continue;
                    var fullPath = $"{hiveName}\\{parent}\\{sub}";
                    if (SafetyGuard.IsProtectedResidu(fullPath)) continue;
                    result.Add(new ResiduItem
                    {
                        Path        = fullPath,
                        Type        = ResiduType.RegistryKey,
                        RegHiveName = hiveName,
                        RegParent   = parent,
                        RegKeyName  = sub
                    });
                }
            }
            catch { }
        }

        private static string GetFolderSizeStr(string path)
        {
            try
            {
                long total = 0;
                int  count = 0;
                foreach (var f in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                {
                    try { total += new FileInfo(f).Length; } catch { }
                    if (++count > 50_000) break; // cap pour les dossiers enormes
                }
                return FormatSize(total);
            }
            catch { return ""; }
        }

        private static string FormatSize(long bytes)
        {
            if (bytes < 1024)              return $"{bytes} o";
            if (bytes < 1024 * 1024)       return $"{bytes / 1024} Ko";
            if (bytes < 1024L * 1024 * 1024) return $"{bytes / (1024 * 1024)} Mo";
            return $"{bytes / (1024L * 1024 * 1024):F1} Go";
        }
    }
}
