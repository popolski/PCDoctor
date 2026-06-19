using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Win32;

namespace PCDoctor.Services
{
    public class BloatwareApp
    {
        public string PackageName { get; set; } = "";
        public string Display { get; set; } = "";
        public bool IsSelected { get; set; } = false;
    }

    public class BloatwareService
    {
        // Liste des bloatwares connus (portée de l'ancien PCDoctor)
        private static readonly (string pkg, string display)[] KnownBloatware = new[]
        {
            ("Microsoft.BingNews", "Bing News"),
            ("Microsoft.BingWeather", "Bing Weather"),
            ("Microsoft.GamingApp", "Xbox App"),
            ("Microsoft.GetHelp", "Get Help"),
            ("Microsoft.Getstarted", "Get Started"),
            ("Microsoft.MicrosoftOfficeHub", "Office Hub"),
            ("Microsoft.MicrosoftSolitaireCollection", "Solitaire Collection"),
            ("Microsoft.MicrosoftStickyNotes", "Sticky Notes"),
            ("Microsoft.MixedReality.Portal", "Mixed Reality Portal"),
            ("Microsoft.People", "People"),
            ("Microsoft.SkypeApp", "Skype"),
            ("Microsoft.WindowsAlarms", "Alarms & Clock"),
            ("Microsoft.WindowsFeedbackHub", "Feedback Hub"),
            ("Microsoft.WindowsMaps", "Maps"),
            ("Microsoft.YourPhone", "Your Phone / Mobile"),
            ("Microsoft.ZuneMusic", "Groove Music"),
            ("Microsoft.ZuneVideo", "Movies & TV"),
            ("Microsoft.Xbox.TCUI", "Xbox TCUI"),
            ("Microsoft.XboxGameOverlay", "Xbox Game Overlay"),
            ("Microsoft.XboxGamingOverlay", "Xbox Gaming Overlay"),
            ("Microsoft.XboxSpeechToTextOverlay", "Xbox Speech to Text"),
            ("Clipchamp.Clipchamp", "Clipchamp"),
            ("MicrosoftTeams", "Teams Personal"),
            ("SpotifyAB.SpotifyMusic", "Spotify"),
            ("Disney.37853FC22B2CE", "Disney+"),
            ("Microsoft.MicrosoftSolitaire", "Solitaire"),
        };

        // SCAN : ne retourne que les bloatwares RÉELLEMENT installés
        public List<BloatwareApp> Scan()
        {
            var installed = GetInstalledPackages();
            var found = new List<BloatwareApp>();
            foreach (var (pkg, display) in KnownBloatware)
            {
                if (installed.Any(p => p.IndexOf(pkg, StringComparison.OrdinalIgnoreCase) >= 0))
                    found.Add(new BloatwareApp { PackageName = pkg, Display = display });
            }
            Logger.Info($"Scan bloatware : {found.Count} app(s) détectée(s)");
            return found;
        }

        private List<string> GetInstalledPackages()
        {
            var list = new List<string>();
            try
            {
                var o = RunPs("Get-AppxPackage | Select-Object -ExpandProperty Name");
                list.AddRange(o.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()));
            }
            catch (Exception e) { Logger.Warn($"GetInstalledPackages : {e.Message}"); }
            return list;
        }

        // SUPPRESSION : désinstalle les apps sélectionnées
        public (int ok, int err) Remove(IEnumerable<BloatwareApp> apps)
        {
            int ok = 0, err = 0;
            foreach (var a in apps)
            {
                try
                {
                    RunPs($"Get-AppxPackage *{a.PackageName}* | Remove-AppxPackage -ErrorAction SilentlyContinue");
                    Logger.Action($"Bloatware désinstallé : {a.Display} ({a.PackageName})");
                    ok++;
                }
                catch (Exception e) { Logger.Error($"Suppression {a.Display} : {e.Message}"); err++; }
            }
            return (ok, err);
        }

        // Suppression complète de OneDrive
        public string RemoveOneDrive()
        {
            var log = new System.Text.StringBuilder();

            // Arrêt du processus
            RunPs("Stop-Process -Name OneDrive -Force -ErrorAction SilentlyContinue");
            log.AppendLine("Processus OneDrive arrêté.");

            // Désinstallation via le setup officiel
            var uninstallers = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),    "OneDriveSetup.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.SystemX86), "OneDriveSetup.exe"),
            };
            bool uninstalled = false;
            foreach (var setup in uninstallers)
            {
                if (!File.Exists(setup)) continue;
                try
                {
                    var psi = new ProcessStartInfo(setup, "/uninstall")
                        { UseShellExecute = false, CreateNoWindow = true };
                    using var p = Process.Start(psi);
                    p?.WaitForExit(30_000);
                    log.AppendLine("Désinstallation via setup lancée.");
                    uninstalled = true;
                    break;
                }
                catch (Exception ex) { log.AppendLine($"Erreur setup : {ex.Message}"); }
            }
            if (!uninstalled)
                log.AppendLine("OneDriveSetup.exe introuvable (peut-être déjà absent).");

            // Suppression des dossiers résiduels
            var folders = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "OneDrive"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "OneDrive"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Microsoft OneDrive"),
                @"C:\OneDriveTemp"
            };
            int foldersDel = 0;
            foreach (var f in folders)
            {
                try { if (Directory.Exists(f)) { Directory.Delete(f, true); foldersDel++; } }
                catch { }
            }
            if (foldersDel > 0) log.AppendLine($"{foldersDel} dossier(s) résiduel(s) supprimé(s).");

            // Nettoyage CLSID (masquer OneDrive de l'Explorateur)
            try
            {
                using var k = Registry.ClassesRoot.OpenSubKey(@"CLSID", writable: true);
                k?.DeleteSubKeyTree("{018D5C66-4533-4307-9B53-224DE2ED1FE6}", throwOnMissingSubKey: false);
            }
            catch { }
            try
            {
                using var k = Registry.ClassesRoot.OpenSubKey(@"Wow6432Node\CLSID", writable: true);
                k?.DeleteSubKeyTree("{018D5C66-4533-4307-9B53-224DE2ED1FE6}", throwOnMissingSubKey: false);
            }
            catch { }
            log.AppendLine("Clés CLSID supprimées.");

            Logger.Action("OneDrive : suppression terminée.");
            return log.ToString().Trim();
        }

        // Bloque Edge de recréer ses raccourcis à chaque MAJ
        public void DisableEdgeShortcuts()
        {
            using var key = Registry.LocalMachine.CreateSubKey(
                @"SOFTWARE\Policies\Microsoft\EdgeUpdate", writable: true);
            key.SetValue("CreateDesktopShortcutDefault", 0, RegistryValueKind.DWord);
            key.SetValue("RemoveDesktopShortcutDefault", 1, RegistryValueKind.DWord);
            Logger.Action("Raccourcis Edge : bloqués via stratégie.");
        }

        public bool IsEdgeShortcutsDisabled()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\EdgeUpdate");
                return key != null && (int)(key.GetValue("CreateDesktopShortcutDefault", 1) ?? 1) == 0;
            }
            catch { return false; }
        }

        private string RunPs(string cmd)
        {
            var psi = new ProcessStartInfo("powershell", $"-NoProfile -Command \"{cmd}\"")
            { RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true };
            using var p = Process.Start(psi);
            string o = p!.StandardOutput.ReadToEnd();
            p.WaitForExit();
            return o;
        }
    }
}