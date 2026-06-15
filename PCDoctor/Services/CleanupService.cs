using System;
using System.Collections.Generic;
using System.IO;

namespace PCDoctor.Services
{
    // Une catégorie de nettoyage : nom, dossiers concernés, taille calculée
    public class CleanupCategory
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public List<string> Paths { get; set; } = new();
        public long SizeBytes { get; set; }
        public string SizeText => FormatSize(SizeBytes);
        public bool IsSelected { get; set; } = true;
        public bool IsRecycleBin { get; set; } = false;

        public static string FormatSize(long bytes)
        {
            if (bytes <= 0) return "0 o";
            string[] u = { "o", "Ko", "Mo", "Go" };
            double s = bytes; int i = 0;
            while (s >= 1024 && i < u.Length - 1) { s /= 1024; i++; }
            return $"{Math.Round(s, 1)} {u[i]}";
        }
    }

    public class CleanupService
    {
        // Construit la liste des catégories avec leurs chemins
        private List<CleanupCategory> BuildCategories()
        {
            string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string win = Environment.GetFolderPath(Environment.SpecialFolder.Windows);

            return new List<CleanupCategory>
            {
                new CleanupCategory {
                    Name = "Fichiers temporaires (utilisateur)",
                    Description = "Dossier Temp de votre profil",
                    Paths = new() { Path.GetTempPath() }
                },
                new CleanupCategory {
                    Name = "Fichiers temporaires (Windows)",
                    Description = "C:\\Windows\\Temp",
                    Paths = new() { Path.Combine(win, "Temp") }
                },
                new CleanupCategory {
                    Name = "Cache des miniatures",
                    Description = "Vignettes de l'explorateur",
                    Paths = new() { Path.Combine(local, "Microsoft", "Windows", "Explorer") }
                },
                new CleanupCategory {
                    Name = "Corbeille",
                    Description = "Vider la corbeille",
                    Paths = new(),
                    IsRecycleBin = true
                }
            };
        }

        // SCAN : calcule la taille de chaque catégorie (ne supprime rien)
        public List<CleanupCategory> Scan()
        {
            var cats = BuildCategories();
            foreach (var c in cats)
            {
                if (c.IsRecycleBin)
                {
                    c.SizeBytes = GetRecycleBinSize();
                }
                else
                {
                    long total = 0;
                    foreach (var p in c.Paths) total += GetFolderSize(p);
                    c.SizeBytes = total;
                }
            }
            Logger.Info($"Scan nettoyage : {cats.Count} catégories analysées");
            return cats;
        }

        // NETTOYAGE : supprime le contenu des catégories sélectionnées
        public (int filesDeleted, long bytesFreed) Clean(IEnumerable<CleanupCategory> selected)
        {
            int files = 0; long bytes = 0;
            foreach (var c in selected)
            {
                if (c.IsRecycleBin)
                {
                    bytes += EmptyRecycleBin();
                    Logger.Action("Corbeille vidée");
                    continue;
                }
                foreach (var p in c.Paths)
                {
                    var (f, b) = CleanFolder(p);
                    files += f; bytes += b;
                }
                Logger.Action($"Nettoyé : {c.Name} ({CleanupCategory.FormatSize(c.SizeBytes)})");
            }
            return (files, bytes);
        }

        // ─ Helpers ─
        private long GetFolderSize(string path)
        {
            long size = 0;
            try
            {
                if (!Directory.Exists(path)) return 0;
                foreach (var f in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                {
                    try { size += new FileInfo(f).Length; } catch { }
                }
            }
            catch (Exception e) { Logger.Warn($"Calcul taille {path} : {e.Message}"); }
            return size;
        }

        private (int, long) CleanFolder(string path)
        {
            int files = 0; long bytes = 0;
            try
            {
                if (!Directory.Exists(path)) return (0, 0);
                foreach (var f in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                {
                    try
                    {
                        long sz = new FileInfo(f).Length;
                        File.Delete(f);
                        files++; bytes += sz;
                    }
                    catch { /* fichier verrouillé : on ignore */ }
                }
            }
            catch (Exception e) { Logger.Warn($"Nettoyage {path} : {e.Message}"); }
            return (files, bytes);
        }

        // Corbeille via Shell API
        private long GetRecycleBinSize()
        {
            try
            {
                var info = new SHQUERYRBINFO { cbSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(SHQUERYRBINFO)) };
                SHQueryRecycleBin(null!, ref info);
                return info.i64Size;
            }
            catch { return 0; }
        }

        private long EmptyRecycleBin()
        {
            long before = GetRecycleBinSize();
            try { SHEmptyRecycleBin(IntPtr.Zero, null!, 0x1 | 0x2 | 0x4); } // no confirm/progress/sound
            catch (Exception e) { Logger.Warn($"Vidage corbeille : {e.Message}"); }
            return before;
        }

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct SHQUERYRBINFO { public int cbSize; public long i64Size; public long i64NumItems; }

        [System.Runtime.InteropServices.DllImport("shell32.dll")]
        private static extern int SHQueryRecycleBin(string pszRootPath, ref SHQUERYRBINFO pSHQueryRBInfo);

        [System.Runtime.InteropServices.DllImport("shell32.dll")]
        private static extern int SHEmptyRecycleBin(IntPtr hwnd, string pszRootPath, uint dwFlags);
    }
}