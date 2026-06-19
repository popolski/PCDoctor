using System;
using System.Linq;

namespace PCDoctor.Services
{
    // Garde-fous pour les opérations destructrices (la leçon de l'incident Wellbia).
    public static class SafetyGuard
    {
        // Services anti-cheat / critiques : JAMAIS supprimables.
        private static readonly string[] ProtectedServices = new[]
        {
            "BEService", "BEDaisy",                      // BattlEye
            "EasyAntiCheat", "EasyAntiCheat_EOS",        // Easy Anti-Cheat
            "vgc", "vgk",                                // Riot Vanguard
            "FACEIT", "FaceitService",                   // FACEIT
            "ESEADriver2",                               // ESEA
            "xigncode", "xhunter1",                      // XIGNCODE3
            "GgcService", "GameGuard", "npggsvc",        // nProtect GameGuard
            "PnkBstrA", "PnkBstrB",                      // PunkBuster
            "mhyprot", "mhyprot2", "mhyprot3",           // miHoYo
            "ucldr_battlegrounds_gl", "ucsvc",           // Wellbia / Uncheater (PUBG) - l'incident
            "Zakynthos Service", "zksvc"                 // Zakynthos (PUBG)
        };

        // Emplacements éditeurs : un binaire ici ne doit jamais être traité comme "orphelin".
        private static readonly string[] SafeLocationPatterns = new[]
        {
            @"\Common Files\", @"\Program Files\", @"\Program Files (x86)\",
            "Wellbia", "BattlEye", "EasyAntiCheat", "AntiCheat", "Uncheater",
            @"\Steam\", @"\Epic Games\", "Riot", "GOG"
        };

        // True si le service est protégé (insensible à la casse).
        public static bool IsProtectedService(string serviceName)
        {
            if (string.IsNullOrWhiteSpace(serviceName)) return false;
            return ProtectedServices.Any(s => s.Equals(serviceName, StringComparison.OrdinalIgnoreCase));
        }

        // True si le chemin est dans un emplacement éditeur à protéger.
        public static bool IsSafeLocation(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return false;
            return SafeLocationPatterns.Any(p => path.IndexOf(p, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        // True si ce résidu est à ne jamais supprimer (anti-cheat, System32...).
        // Moins strict que IsSafeLocation : ne bloque pas tout Program Files,
        // seulement les emplacements vraiment dangereux.
        public static bool IsProtectedResidu(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return false;
            var dangerous = new[]
            {
                "Wellbia", "BattlEye", "EasyAntiCheat", "AntiCheat", "Uncheater",
                "vgc", "vgk", "Vanguard", "nProtect", "FACEIT", "PunkBuster",
                "mhyprot", "xigncode", "xhunter", "GameGuard"
            };
            if (dangerous.Any(d => path.IndexOf(d, StringComparison.OrdinalIgnoreCase) >= 0))
                return true;
            // Bloquer System32 et SysWOW64
            var win = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            return path.StartsWith(System.IO.Path.Combine(win, "System32"), StringComparison.OrdinalIgnoreCase)
                || path.StartsWith(System.IO.Path.Combine(win, "SysWOW64"), StringComparison.OrdinalIgnoreCase);
        }
    }
}