using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

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