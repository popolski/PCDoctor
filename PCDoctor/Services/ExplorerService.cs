using System;
using System.Diagnostics;
using Microsoft.Win32;

namespace PCDoctor.Services
{
    public class ExplorerService
    {
        private const string Advanced = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
        private const string Cabinet  = @"Software\Microsoft\Windows\CurrentVersion\Explorer\CabinetState";

        // ─── Extensions de fichiers visibles ───
        // HideFileExt = 0 -> affiche les extensions
        public bool IsFileExtVisible()     => GetHkcu(Advanced, "HideFileExt") == 0;
        public void SetFileExtVisible(bool v) => SetHkcu(Advanced, "HideFileExt", v ? 0 : 1);

        // ─── Fichiers cachés ───
        // Hidden = 1 -> affiche les fichiers cachés
        public bool IsHiddenVisible()      => GetHkcu(Advanced, "Hidden") == 1;
        public void SetHiddenVisible(bool v)  => SetHkcu(Advanced, "Hidden", v ? 1 : 2);

        // ─── Fichiers système protégés ───
        // ShowSuperHidden = 1 -> affiche les fichiers système (ex: pagefile.sys)
        public bool IsSuperHiddenVisible()    => GetHkcu(Advanced, "ShowSuperHidden") == 1;
        public void SetSuperHiddenVisible(bool v) => SetHkcu(Advanced, "ShowSuperHidden", v ? 1 : 0);

        // ─── Chemin complet dans la barre de titre ───
        public bool IsFullPathInTitle()    => GetHkcu(Cabinet, "FullPath") == 1;
        public void SetFullPathInTitle(bool v) => SetHkcu(Cabinet, "FullPath", v ? 1 : 0);

        // ─── Vignettes désactivées ───
        // IconsOnly = 1 -> icônes seules (plus rapide sur HDD)
        public bool IsIconsOnly()          => GetHkcu(Advanced, "IconsOnly") == 1;
        public void SetIconsOnly(bool v)      => SetHkcu(Advanced, "IconsOnly", v ? 1 : 0);

        // ─── "Fin de tâche" dans la barre des tâches (Win11) ───
        public bool IsEndTaskEnabled()     => GetHkcu(Advanced, "TaskbarEndTask") == 1;
        public void SetEndTask(bool v)        => SetHkcu(Advanced, "TaskbarEndTask", v ? 1 : 0);

        // ─── NumLock au démarrage ───
        // HKEY_USERS\.DEFAULT\Control Panel\Keyboard\InitialKeyboardIndicators
        // "2" = NumLock ON, "0" = OFF
        public bool IsNumLockOnBoot()
        {
            try
            {
                using var key = Registry.Users.OpenSubKey(@".DEFAULT\Control Panel\Keyboard");
                return (key?.GetValue("InitialKeyboardIndicators") as string) == "2";
            }
            catch { return false; }
        }
        public void SetNumLockOnBoot(bool v)
        {
            try
            {
                using var key = Registry.Users.OpenSubKey(@".DEFAULT\Control Panel\Keyboard", writable: true);
                key?.SetValue("InitialKeyboardIndicators", v ? "2" : "0");
                Logger.Action($"NumLock au demarrage : {(v ? "ON" : "OFF")}");
            }
            catch (Exception e) { Logger.Warn($"SetNumLockOnBoot : {e.Message}"); }
        }

        // ─── Alignement barre des tâches (Win 11) ───
        // TaskbarAl : 0 = gauche, 1 = centre (défaut Win11)
        public bool IsTaskbarCentered() => GetHkcu(Advanced, "TaskbarAl") != 0;
        public void SetTaskbarCentered(bool v) => SetHkcu(Advanced, "TaskbarAl", v ? 1 : 0);

        // ─── Barre de recherche ───
        // SearchboxTaskbarMode : 0 = masquée, 1 = icône, 2 = barre complète
        private const string SearchKey = @"Software\Microsoft\Windows\CurrentVersion\Search";
        public int GetSearchBarMode() => GetHkcu(SearchKey, "SearchboxTaskbarMode") ?? 2;
        public void SetSearchBarMode(int mode) => SetHkcu(SearchKey, "SearchboxTaskbarMode", mode);

        // ─── Widgets ───
        // TaskbarDa : 1 = visible (défaut), 0 = masqué
        public bool IsWidgetsEnabled() => GetHkcu(Advanced, "TaskbarDa") != 0;
        public void SetWidgets(bool v) => SetHkcu(Advanced, "TaskbarDa", v ? 1 : 0);

        // ─── Menu contextuel classique (Win 11) ───
        // Créer la clé avec une valeur par défaut vide = ancien menu ; supprimer = nouveau menu
        private const string ClassicMenuKey = @"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32";
        public bool IsClassicContextMenu()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(ClassicMenuKey);
                return key != null;
            }
            catch { return false; }
        }
        public void SetClassicContextMenu(bool v)
        {
            try
            {
                if (v)
                {
                    using var key = Registry.CurrentUser.CreateSubKey(ClassicMenuKey);
                    key?.SetValue("", "", RegistryValueKind.String);
                }
                else
                {
                    Registry.CurrentUser.DeleteSubKeyTree(
                        @"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}",
                        throwOnMissingSubKey: false);
                }
            }
            catch (Exception e) { Logger.Warn($"SetClassicContextMenu : {e.Message}"); }
        }

        // ─── Redémarrer l'Explorateur (pour appliquer les changements) ───
        public void RestartExplorer()
        {
            try
            {
                foreach (var p in Process.GetProcessesByName("explorer"))
                    p.Kill();
                // Windows relance explorer.exe automatiquement
            }
            catch (Exception e) { Logger.Warn($"RestartExplorer : {e.Message}"); }
        }

        // ─── Helpers HKCU ───
        private static int? GetHkcu(string keyPath, string name)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(keyPath);
                var v = key?.GetValue(name);
                return v == null ? (int?)null : (int)v;
            }
            catch { return null; }
        }
        private static void SetHkcu(string keyPath, string name, int value)
        {
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(keyPath);
                key?.SetValue(name, value, RegistryValueKind.DWord);
            }
            catch { }
        }
    }
}
