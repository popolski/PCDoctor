using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace PCDoctor.Services
{
    public class BackupService
    {
        private readonly OptimService    _optim    = new();
        private readonly PrivacyService  _privacy  = new();
        private readonly GamingService   _gaming   = new();
        private readonly ExplorerService _explorer = new();

        public string Export()
        {
            var data = new Dictionary<string, object>
            {
                ["version"] = "1.0",
                ["date"]    = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                ["optim"]   = new Dictionary<string, object>
                {
                    ["hibernation"]       = _optim.IsHibernationActive(),
                    ["fastStartup"]       = _optim.IsFastStartupActive(),
                    ["powerThrottling"]   = _optim.IsPowerThrottlingDisabled(),
                    ["memoryCompression"] = _optim.IsMemoryCompressionActive(),
                    ["verboseStatus"]     = _optim.IsVerboseStatusActive(),
                    ["sysMain"]           = _optim.IsSysMainActive(),
                    ["searchIndex"]       = _optim.IsSearchIndexActive(),
                    ["wer"]               = _optim.IsWerActive(),
                },
                ["privacy"] = new Dictionary<string, object>
                {
                    ["telemetry"]           = _privacy.IsTelemetryActive(),
                    ["cortana"]             = _privacy.IsCortanaActive(),
                    ["activity"]            = _privacy.IsActivityActive(),
                    ["ads"]                 = _privacy.IsAdsActive(),
                    ["adId"]                = _privacy.IsAdIdActive(),
                    ["office"]              = _privacy.IsOfficeActive(),
                    ["wifiSense"]           = _privacy.IsWifiSenseActive(),
                    ["backgroundApps"]      = _privacy.IsBackgroundAppsActive(),
                    ["recall"]              = _privacy.IsRecallActive(),
                    ["location"]            = _privacy.IsLocationActive(),
                    ["copilot"]             = _privacy.IsCopilotActive(),
                    ["aiSearch"]            = _privacy.IsAiSearchActive(),
                    ["settingsSuggestions"] = _privacy.IsSettingsSuggestionsActive(),
                },
                ["gaming"] = new Dictionary<string, object>
                {
                    ["highPerf"]    = _gaming.IsHighPerfActive(),
                    ["gameMode"]    = _gaming.IsGameModeActive(),
                    ["hags"]        = _gaming.IsHagsActive(),
                    ["gameBar"]     = _gaming.IsGameBarActive(),
                    ["gpuPriority"] = _gaming.IsGpuPriorityActive(),
                    ["mouseAccel"]  = _gaming.IsMouseAccelActive(),
                    ["nagle"]       = _gaming.IsNagleDisabled(),
                },
                ["explorer"] = new Dictionary<string, object>
                {
                    ["fileExtVisible"]  = _explorer.IsFileExtVisible(),
                    ["hiddenVisible"]   = _explorer.IsHiddenVisible(),
                    ["superHidden"]     = _explorer.IsSuperHiddenVisible(),
                    ["fullPathInTitle"] = _explorer.IsFullPathInTitle(),
                    ["iconsOnly"]       = _explorer.IsIconsOnly(),
                    ["endTask"]         = _explorer.IsEndTaskEnabled(),
                    ["numLock"]         = _explorer.IsNumLockOnBoot(),
                    ["taskbarCentered"] = _explorer.IsTaskbarCentered(),
                    ["searchBarMode"]   = _explorer.GetSearchBarMode(),
                    ["widgets"]         = _explorer.IsWidgetsEnabled(),
                    ["classicMenu"]     = _explorer.IsClassicContextMenu(),
                },
            };

            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                $"PCDoctor_backup_{DateTime.Now:yyyyMMdd_HHmm}.json");
            File.WriteAllText(path, json, System.Text.Encoding.UTF8);
            Logger.Action($"Backup exporté : {Path.GetFileName(path)}");
            return path;
        }

        public string Import(string path)
        {
            string json = File.ReadAllText(path);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            int applied = 0;

            if (root.TryGetProperty("optim", out var optim))
            {
                Bool(optim, "hibernation",       v => _optim.SetHibernation(v));
                Bool(optim, "fastStartup",       v => _optim.SetFastStartup(v));
                Bool(optim, "powerThrottling",   v => _optim.SetPowerThrottling(v));
                Bool(optim, "memoryCompression", v => _optim.SetMemoryCompression(v));
                Bool(optim, "verboseStatus",     v => _optim.SetVerboseStatus(v));
                Bool(optim, "sysMain",           v => _optim.SetSysMain(v));
                Bool(optim, "searchIndex",       v => _optim.SetSearchIndex(v));
                Bool(optim, "wer",               v => _optim.SetWer(v));
                applied++;
            }
            if (root.TryGetProperty("privacy", out var privacy))
            {
                Bool(privacy, "telemetry",           v => _privacy.SetTelemetry(v));
                Bool(privacy, "cortana",             v => _privacy.SetCortana(v));
                Bool(privacy, "activity",            v => _privacy.SetActivity(v));
                Bool(privacy, "ads",                 v => _privacy.SetAds(v));
                Bool(privacy, "adId",                v => _privacy.SetAdId(v));
                Bool(privacy, "office",              v => _privacy.SetOffice(v));
                Bool(privacy, "wifiSense",           v => _privacy.SetWifiSense(v));
                Bool(privacy, "backgroundApps",      v => _privacy.SetBackgroundApps(v));
                Bool(privacy, "recall",              v => _privacy.SetRecall(v));
                Bool(privacy, "location",            v => _privacy.SetLocation(v));
                Bool(privacy, "copilot",             v => _privacy.SetCopilot(v));
                Bool(privacy, "aiSearch",            v => _privacy.SetAiSearch(v));
                Bool(privacy, "settingsSuggestions", v => _privacy.SetSettingsSuggestions(v));
                applied++;
            }
            if (root.TryGetProperty("gaming", out var gaming))
            {
                Bool(gaming, "highPerf",    v => _gaming.SetHighPerf(v));
                Bool(gaming, "gameMode",    v => _gaming.SetGameMode(v));
                Bool(gaming, "hags",        v => _gaming.SetHags(v));
                Bool(gaming, "gameBar",     v => _gaming.SetGameBar(v));
                Bool(gaming, "gpuPriority", v => _gaming.SetGpuPriority(v));
                Bool(gaming, "mouseAccel",  v => _gaming.SetMouseAccel(v));
                Bool(gaming, "nagle",       v => _gaming.SetNagle(v));
                applied++;
            }
            if (root.TryGetProperty("explorer", out var explorer))
            {
                Bool(explorer, "fileExtVisible",  v => _explorer.SetFileExtVisible(v));
                Bool(explorer, "hiddenVisible",   v => _explorer.SetHiddenVisible(v));
                Bool(explorer, "superHidden",     v => _explorer.SetSuperHiddenVisible(v));
                Bool(explorer, "fullPathInTitle", v => _explorer.SetFullPathInTitle(v));
                Bool(explorer, "iconsOnly",       v => _explorer.SetIconsOnly(v));
                Bool(explorer, "endTask",         v => _explorer.SetEndTask(v));
                Bool(explorer, "numLock",         v => _explorer.SetNumLockOnBoot(v));
                Bool(explorer, "taskbarCentered", v => _explorer.SetTaskbarCentered(v));
                Bool(explorer, "widgets",         v => _explorer.SetWidgets(v));
                Bool(explorer, "classicMenu",     v => _explorer.SetClassicContextMenu(v));
                if (explorer.TryGetProperty("searchBarMode", out var mode))
                    _explorer.SetSearchBarMode(mode.GetInt32());
                applied++;
            }

            Logger.Action($"Backup restauré depuis {Path.GetFileName(path)} : {applied} sections appliquées");
            return $"{applied} sections restaurées avec succès.";
        }

        private static void Bool(JsonElement obj, string key, Action<bool> setter)
        {
            if (obj.TryGetProperty(key, out var v) &&
                (v.ValueKind == JsonValueKind.True || v.ValueKind == JsonValueKind.False))
                setter(v.GetBoolean());
        }
    }
}
