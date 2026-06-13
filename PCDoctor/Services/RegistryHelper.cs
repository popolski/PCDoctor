using Microsoft.Win32;

namespace PCDoctor.Services
{
    // Helper centralisé pour lire/écrire le registre proprement.
    public static class RegistryHelper
    {
        // Lit une valeur DWORD (int). Retourne null si absente.
        public static int? GetDword(string keyPath, string valueName)
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(keyPath)
                    ?? Registry.CurrentUser.OpenSubKey(keyPath);
                var v = key?.GetValue(valueName);
                if (v == null) return null;
                return (int)v;
            }
            catch { return null; }
        }

        // Écrit une valeur DWORD sous HKLM (crée la clé si besoin).
        public static void SetDwordHklm(string keyPath, string valueName, int value)
        {
            using var key = Registry.LocalMachine.CreateSubKey(keyPath);
            key?.SetValue(valueName, value, RegistryValueKind.DWord);
        }

        // Supprime une valeur sous HKLM (sans erreur si absente).
        public static void DeleteValueHklm(string keyPath, string valueName)
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(keyPath, true);
                key?.DeleteValue(valueName, false);
            }
            catch { }
        }
    }
}