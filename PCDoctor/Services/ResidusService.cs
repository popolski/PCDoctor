using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using Microsoft.Win32;

namespace PCDoctor.Services
{
    public enum ResiduType { Folder, File, RegistryKey }

    public class ResiduItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public string    Path        { get; set; } = "";
        public ResiduType Type       { get; set; }
        public string    SizeStr     { get; set; } = "";
        public string    TypeLabel   => Type switch { ResiduType.Folder => "Dossier", ResiduType.File => "Fichier", _ => "Registre" };

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected))); }
        }

        // Utilisés pour la suppression (meme assembly)
        public string RegHiveName    { get; set; } = "";   // "HKLM" ou "HKCU"
        public string RegParent     { get; set; } = "";   // ex : "SOFTWARE"
        public string RegKeyName    { get; set; } = "";   // ex : "Firefox" (sous-clé) ou "72" (valeur)
        public bool   IsRegValue    { get; set; } = false; // true = valeur à supprimer, false = sous-clé
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
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "LocalLow"),
        };

        // Emplacements de raccourcis et menus Démarrer
        private static readonly string[] ShortcutRoots =
        {
            Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms),   // Start Menu\Programs (ProgramData)
            Environment.GetFolderPath(Environment.SpecialFolder.Programs),          // Start Menu\Programs (User)
            Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup),
            Environment.GetFolderPath(Environment.SpecialFolder.Startup),
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory),
        };

        // Scan : recherche par mot-clé dans les emplacements typiques
        public List<ResiduItem> Scan(string keyword)
        {
            var result  = new List<ResiduItem>();
            var seen    = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(keyword)) return result;

            // Dossiers — niveau 1 direct + niveau 2 (publisher\appname)
            foreach (var root in FolderRoots)
            {
                if (!Directory.Exists(root)) continue;
                ScanFolderLevel(root, keyword, seen, result);

                // Un niveau supplémentaire : cherche dans chaque sous-dossier de root
                try
                {
                    foreach (var sub in Directory.GetDirectories(root, "*", SearchOption.TopDirectoryOnly))
                    {
                        ScanFolderLevel(sub, keyword, seen, result);
                    }
                }
                catch { }
            }

            // Raccourcis .lnk dans Start Menu, Bureau, Démarrage
            foreach (var root in ShortcutRoots)
            {
                if (!Directory.Exists(root)) continue;
                try
                {
                    // Dossier dans le Start Menu dont le nom matche le mot-clé
                    foreach (var dir in Directory.GetDirectories(root, "*", SearchOption.TopDirectoryOnly))
                    {
                        if (!Matches(System.IO.Path.GetFileName(dir), keyword)) continue;
                        if (!seen.Add(dir)) continue;
                        result.Add(new ResiduItem { Path = dir, Type = ResiduType.Folder, SizeStr = "" });
                    }
                    // Fichiers .lnk dont le nom matche le mot-clé
                    foreach (var lnk in Directory.GetFiles(root, "*.lnk", SearchOption.AllDirectories))
                    {
                        if (!Matches(System.IO.Path.GetFileNameWithoutExtension(lnk), keyword)) continue;
                        if (!seen.Add(lnk)) continue;
                        result.Add(new ResiduItem { Path = lnk, Type = ResiduType.File, SizeStr = "Raccourci" });
                    }
                }
                catch { }
            }

            // Registre — SOFTWARE (sous-clés directes)
            ScanRegHive("HKLM", Registry.LocalMachine, @"SOFTWARE",                         keyword, result);
            ScanRegHive("HKLM", Registry.LocalMachine, @"SOFTWARE\WOW6432Node",             keyword, result);
            ScanRegHive("HKCU", Registry.CurrentUser,  @"SOFTWARE",                         keyword, result);

            // Registre — clés Uninstall (résidus très courants)
            ScanRegHive("HKLM", Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",             keyword, result);
            ScanRegHive("HKLM", Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall", keyword, result);
            ScanRegHive("HKCU", Registry.CurrentUser,  @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",             keyword, result);

            // Registre — Run / RunOnce
            ScanRegValues("HKLM", Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run",      keyword, result);
            ScanRegValues("HKCU", Registry.CurrentUser,  @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run",      keyword, result);

            // Registre — UFH (Shell History Cache : raccourcis récents)
            ScanRegValues("HKCU", Registry.CurrentUser,  @"SOFTWARE\Microsoft\Windows\CurrentVersion\UFH\SHC",  keyword, result);
            ScanRegValues("HKLM", Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\UFH\ARP",  keyword, result);

            Logger.Info($"Résidus '{keyword}' : {result.Count} élément(s) trouvé(s)");
            return result;
        }

        // Correspondance dans les deux sens : "7-Zip" matche "7-Zip 26.01 (x64)" et vice-versa
        private static bool Matches(string name, string keyword) =>
            name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0
            || keyword.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0;

        private static void ScanFolderLevel(string root, string keyword,
            HashSet<string> seen, List<ResiduItem> result)
        {
            if (!Directory.Exists(root)) return;
            try
            {
                foreach (var dir in Directory.GetDirectories(root, "*", SearchOption.TopDirectoryOnly))
                {
                    if (!Matches(System.IO.Path.GetFileName(dir), keyword)) continue;
                    if (!seen.Add(dir)) continue;
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
                    else if (item.Type == ResiduType.File)
                    {
                        File.Delete(item.Path);
                        Logger.Action($"Résidu supprimé (fichier) : {item.Path}");
                        ok++;
                    }
                    else
                    {
                        var hive = item.RegHiveName == "HKCU"
                            ? Registry.CurrentUser
                            : Registry.LocalMachine;
                        using var parent = hive.OpenSubKey(item.RegParent, writable: true);
                        if (item.IsRegValue)
                            parent?.DeleteValue(item.RegKeyName, throwOnMissingValue: false);
                        else
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
                    if (!Matches(sub, keyword)) continue;
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

        // Cherche le mot-clé dans les valeurs (nom ou données) d'une clé registre
        private static void ScanRegValues(string hiveName, RegistryKey hive, string parent,
            string keyword, List<ResiduItem> result)
        {
            try
            {
                using var key = hive.OpenSubKey(parent);
                if (key == null) return;
                foreach (var valueName in key.GetValueNames())
                {
                    bool nameMatch = valueName.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
                    var raw = key.GetValue(valueName);
                    // REG_MULTI_SZ est un string[] — ToString() donne "System.String[]", il faut joindre
                    string dataStr = raw is string[] arr ? string.Join(" ", arr) : raw?.ToString() ?? "";
                    bool dataMatch = dataStr.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
                    if (!nameMatch && !dataMatch) continue;
                    var fullPath = $"{hiveName}\\{parent}\\[{valueName}]";
                    result.Add(new ResiduItem
                    {
                        Path        = fullPath,
                        Type        = ResiduType.RegistryKey,
                        RegHiveName = hiveName,
                        RegParent   = parent,
                        RegKeyName  = valueName,
                        IsRegValue  = true
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
