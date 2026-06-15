using System;
using System.Collections.Generic;
using Microsoft.Win32;

namespace PCDoctor.Services
{
    public class StartupEntry
    {
        public string Name { get; set; } = "";
        public string Command { get; set; } = "";
        public string Location { get; set; } = "";  // HKCU ou HKLM
        public string Status { get; set; } = "Activé";
    }

    public class StartupService
    {
        private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string DisabledKey = @"Software\Microsoft\Windows\CurrentVersion\Run-PCDoctorDisabled";

        public List<StartupEntry> GetEntries()
        {
            var list = new List<StartupEntry>();
            ReadFrom(Registry.CurrentUser, RunKey, "HKCU", "Activé", list);
            ReadFrom(Registry.LocalMachine, RunKey, "HKLM", "Activé", list);
            ReadFrom(Registry.CurrentUser, DisabledKey, "HKCU", "Désactivé", list);
            ReadFrom(Registry.LocalMachine, DisabledKey, "HKLM", "Désactivé", list);
            Logger.Info($"Startup : {list.Count} entrées trouvées");
            return list;
        }

        private void ReadFrom(RegistryKey root, string path, string loc, string status, List<StartupEntry> list)
        {
            try
            {
                using var key = root.OpenSubKey(path);
                if (key == null) return;
                foreach (var name in key.GetValueNames())
                {
                    list.Add(new StartupEntry
                    {
                        Name = name,
                        Command = key.GetValue(name)?.ToString() ?? "",
                        Location = loc,
                        Status = status
                    });
                }
            }
            catch (Exception e) { Logger.Warn($"Lecture startup {loc} : {e.Message}"); }
        }

        // Désactiver = déplacer de Run vers Run-PCDoctorDisabled (réversible)
        public bool Disable(StartupEntry entry)
        {
            return Move(entry, RunKey, DisabledKey, "désactivée");
        }

        // Réactiver = remettre de Run-PCDoctorDisabled vers Run
        public bool Enable(StartupEntry entry)
        {
            return Move(entry, DisabledKey, RunKey, "réactivée");
        }

        private bool Move(StartupEntry entry, string fromPath, string toPath, string verb)
        {
            try
            {
                var root = entry.Location == "HKLM" ? Registry.LocalMachine : Registry.CurrentUser;
                using var from = root.OpenSubKey(fromPath, true);
                if (from == null) return false;
                var val = from.GetValue(entry.Name);
                if (val == null) return false;

                using var to = root.CreateSubKey(toPath);
                to?.SetValue(entry.Name, val);
                from.DeleteValue(entry.Name, false);

                Logger.Action($"Entrée démarrage {verb} : {entry.Name} ({entry.Location})");
                return true;
            }
            catch (Exception e) { Logger.Error($"Move startup {entry.Name} : {e.Message}"); return false; }
        }
    }
}