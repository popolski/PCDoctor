using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Win32;

namespace PCDoctor.Services
{
    public class InstalledApp
    {
        public string Name { get; set; } = "";
        public string Version { get; set; } = "";
        public string Publisher { get; set; } = "";
        public string UninstallCmd { get; set; } = "";
    }

    public class AppsService
    {
        private static readonly string[] UninstallKeys = new[]
        {
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
            @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
        };

        public List<InstalledApp> GetApps()
        {
            var list = new List<InstalledApp>();
            ReadFrom(Registry.LocalMachine, UninstallKeys[0], list);
            ReadFrom(Registry.LocalMachine, UninstallKeys[1], list);
            ReadFrom(Registry.CurrentUser, UninstallKeys[0], list);

            // Tri alphabétique + dédoublonnage par nom
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var result = new List<InstalledApp>();
            foreach (var a in list)
            {
                if (string.IsNullOrWhiteSpace(a.Name)) continue;
                if (seen.Add(a.Name)) result.Add(a);
            }
            result.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase));
            Logger.Info($"Applications : {result.Count} programmes listés");
            return result;
        }

        private void ReadFrom(RegistryKey root, string path, List<InstalledApp> list)
        {
            try
            {
                using var key = root.OpenSubKey(path);
                if (key == null) return;
                foreach (var sub in key.GetSubKeyNames())
                {
                    try
                    {
                        using var app = key.OpenSubKey(sub);
                        if (app == null) continue;
                        var name = app.GetValue("DisplayName")?.ToString();
                        if (string.IsNullOrWhiteSpace(name)) continue;
                        // Ignorer les mises à jour système et composants cachés
                        if (app.GetValue("SystemComponent") is int sc && sc == 1) continue;
                        if (name.StartsWith("KB") && name.Contains("Update")) continue;

                        list.Add(new InstalledApp
                        {
                            Name = name,
                            Version = app.GetValue("DisplayVersion")?.ToString() ?? "",
                            Publisher = app.GetValue("Publisher")?.ToString() ?? "",
                            UninstallCmd = app.GetValue("UninstallString")?.ToString() ?? ""
                        });
                    }
                    catch { }
                }
            }
            catch (Exception e) { Logger.Warn($"Lecture apps {path} : {e.Message}"); }
        }

        public bool Uninstall(InstalledApp app)
        {
            if (string.IsNullOrWhiteSpace(app.UninstallCmd)) return false;
            try
            {
                // La commande d'uninstall peut être msiexec ou un .exe. On lance via cmd.
                var psi = new ProcessStartInfo("cmd.exe", "/c " + app.UninstallCmd)
                { UseShellExecute = false, CreateNoWindow = false };
                using var p = Process.Start(psi);
                Logger.Action($"Désinstallation lancée : {app.Name}");
                return true;
            }
            catch (Exception e) { Logger.Error($"Uninstall {app.Name} : {e.Message}"); return false; }
        }
    }
}