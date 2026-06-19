using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Win32;

namespace PCDoctor.Services
{
    // ─── Plans d'alimentation ────────────────────────────────────────────────

    public record PowerPlan(string Label, string Guid);

    public class ProfilesService
    {
        public static readonly PowerPlan[] PowerPlans =
        {
            new("Économie d'énergie",      "a1841308-3541-4fab-bc81-f71556f20b4a"),
            new("Équilibré",               "381b4222-f694-41f0-9685-ff5bb260df2e"),
            new("Hautes performances",     "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c"),
            new("Performances optimales",  "e9a42b02-d5df-448d-aa00-03f14749eb61"),
        };

        public string GetActivePlanLabel()
        {
            try
            {
                var psi = new ProcessStartInfo("powercfg.exe", "/getactivescheme")
                { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true };
                using var p = Process.Start(psi)!;
                string o = p.StandardOutput.ReadToEnd().ToLowerInvariant();
                p.WaitForExit();
                foreach (var plan in PowerPlans)
                    if (o.Contains(plan.Guid.ToLowerInvariant())) return plan.Label;
                return "Personnalisé";
            }
            catch { return "Inconnu"; }
        }

        public bool IsUltimatePerfAvailable()
        {
            try
            {
                // Tente de dupliquer le schéma Ultimate ; si pas disponible, powercfg retourne une erreur
                var psi = new ProcessStartInfo("powercfg.exe",
                    $"/duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61")
                { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
                using var p = Process.Start(psi)!;
                string o = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
                p.WaitForExit();
                return p.ExitCode == 0 && !o.Contains("introuvable", StringComparison.OrdinalIgnoreCase)
                                       && !o.Contains("not found",   StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        }

        public void SetPowerPlan(string label)
        {
            var plan = Array.Find(PowerPlans, x => x.Label == label);
            if (plan == null) return;
            try
            {
                // Pour Ultimate : dupliquer d'abord si absent
                if (plan.Guid == "e9a42b02-d5df-448d-aa00-03f14749eb61")
                {
                    var dup = new ProcessStartInfo("powercfg.exe", $"/duplicatescheme {plan.Guid}")
                    { UseShellExecute = false, CreateNoWindow = true };
                    using var pd = Process.Start(dup)!;
                    pd.WaitForExit();
                }
                var psi = new ProcessStartInfo("powercfg.exe", $"/setactive {plan.Guid}")
                { UseShellExecute = false, CreateNoWindow = true };
                using var p = Process.Start(psi)!;
                p.WaitForExit();
                Logger.Action($"Plan alimentation : {label}");
            }
            catch (Exception e) { Logger.Warn($"SetPowerPlan : {e.Message}"); }
        }

        // ─── Profils rapides ────────────────────────────────────────────────

        public List<string> ApplyGamingProfile()
        {
            var log = new List<string>();
            Apply(log, "Plan alimentation",       () => SetPowerPlan("Hautes performances"));
            Apply(log, "Nagle desactive",          () => new GamingService().SetNagle(true));
            Apply(log, "Game Mode ON",             () => new GamingService().SetGameMode(true));
            Apply(log, "HAGS ON",                  () => new GamingService().SetHags(true));
            Apply(log, "Xbox Game Bar OFF",        () => new GamingService().SetGameBar(false));
            Apply(log, "Acceleration souris OFF",  () => new GamingService().SetMouseAccel(false));
            Apply(log, "SysMain OFF",              () => new OptimService().SetSysMain(false));
            Apply(log, "Power Throttling OFF",     () => new OptimService().SetPowerThrottling(true));
            Apply(log, "Hibernation OFF",          () => new OptimService().SetHibernation(false));
            Logger.Action("Profil Gaming applique");
            return log;
        }

        public List<string> ApplyPrivacyProfile()
        {
            var log = new List<string>();
            var priv = new PrivacyService();
            Apply(log, "Telemetrie OFF",            () => priv.SetTelemetry(false));
            Apply(log, "Cortana OFF",               () => priv.SetCortana(false));
            Apply(log, "Activity History OFF",      () => priv.SetActivity(false));
            Apply(log, "Advertising ID OFF",        () => priv.SetAdId(false));
            Apply(log, "Wi-Fi Sense OFF",           () => priv.SetWifiSense(false));
            Apply(log, "Apps arriere-plan OFF",     () => priv.SetBackgroundApps(false));
            Apply(log, "Recall OFF",                () => priv.SetRecall(false));
            Apply(log, "Localisation OFF",          () => priv.SetLocation(false));
            Apply(log, "Copilot OFF",               () => priv.SetCopilot(false));
            Apply(log, "Suggestions IA OFF",        () => priv.SetAiSearch(false));
            Apply(log, "Publicites OFF",            () => priv.SetAds(false));
            Logger.Action("Profil Vie privee applique");
            return log;
        }

        public List<string> ApplyProductivityProfile()
        {
            var log = new List<string>();
            Apply(log, "Plan alimentation Equilibre", () => SetPowerPlan("Équilibré"));
            Apply(log, "Extensions fichiers ON",    () => new ExplorerService().SetFileExtVisible(true));
            Apply(log, "Fichiers caches ON",        () => new ExplorerService().SetHiddenVisible(true));
            Apply(log, "Chemin complet ON",         () => new ExplorerService().SetFullPathInTitle(true));
            Apply(log, "Fin de tache ON",           () => new ExplorerService().SetEndTask(true));
            Apply(log, "NumLock ON",                () => new ExplorerService().SetNumLockOnBoot(true));
            Apply(log, "Fast Startup OFF",          () => new OptimService().SetFastStartup(false));
            Apply(log, "Game Bar OFF",              () => new GamingService().SetGameBar(false));
            Logger.Action("Profil Productivite applique");
            return log;
        }

        public List<string> ApplyBalancedProfile()
        {
            var log = new List<string>();
            Apply(log, "Plan alimentation Equilibre", () => SetPowerPlan("Équilibré"));
            Apply(log, "SysMain ON",               () => new OptimService().SetSysMain(true));
            Apply(log, "Power Throttling ON",      () => new OptimService().SetPowerThrottling(false));
            Apply(log, "Hibernation ON",           () => new OptimService().SetHibernation(true));
            Apply(log, "Game Mode ON",             () => new GamingService().SetGameMode(true));
            Apply(log, "Acceleration souris ON",   () => new GamingService().SetMouseAccel(true));
            Apply(log, "Nagle ON",                 () => new GamingService().SetNagle(false));
            Logger.Action("Profil Equilibre applique");
            return log;
        }

        private static void Apply(List<string> log, string label, Action action)
        {
            try { action(); log.Add($"+ {label}"); }
            catch (Exception e) { log.Add($"! {label} : {e.Message}"); }
        }
    }
}
