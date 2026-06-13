using System;
using System.Diagnostics;

namespace PCDoctor.Services
{
    public class HardeningService
    {
        private const string LlmnrKey = @"SOFTWARE\Policies\Microsoft\Windows NT\DNSClient";

        // ─── LLMNR ─── (active = EnableMulticast absent ou !=0 ; sécurisé = 0)
        public bool IsLlmnrActive()
        {
            var v = RegistryHelper.GetDword(LlmnrKey, "EnableMulticast");
            return v != 0; // null (absent) ou !=0 => actif
        }

        public void SetLlmnr(bool active)
        {
            if (active)
                RegistryHelper.DeleteValueHklm(LlmnrKey, "EnableMulticast"); // retour défaut = actif
            else
                RegistryHelper.SetDwordHklm(LlmnrKey, "EnableMulticast", 0); // 0 = désactivé (sécurisé)
        }

        // ─── SMBv1 ─── via DISM (feature Windows). On lance la commande.
        public bool IsSmb1Active()
        {
            try
            {
                var psi = new ProcessStartInfo("powershell",
                    "-NoProfile -Command \"(Get-WindowsOptionalFeature -Online -FeatureName SMB1Protocol).State\"")
                { RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true };
                using var p = Process.Start(psi);
                string outp = p!.StandardOutput.ReadToEnd();
                p.WaitForExit();
                return outp.Trim().Equals("Enabled", StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        }

        public void SetSmb1(bool active)
        {
            string verb = active ? "Enable" : "Disable";
            var psi = new ProcessStartInfo("powershell",
                $"-NoProfile -Command \"{verb}-WindowsOptionalFeature -Online -FeatureName SMB1Protocol -NoRestart\"")
            { UseShellExecute = false, CreateNoWindow = true };
            using var p = Process.Start(psi);
            p!.WaitForExit();
        }

        // ─── SmartScreen / PUA Protection ─── via Defender
        public bool IsPuaActive()
        {
            try
            {
                var psi = new ProcessStartInfo("powershell",
                    "-NoProfile -Command \"(Get-MpPreference).PUAProtection\"")
                { RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true };
                using var p = Process.Start(psi);
                string outp = p!.StandardOutput.ReadToEnd();
                p.WaitForExit();
                return outp.Trim() == "1";
            }
            catch { return false; }
        }

        public void SetPua(bool active)
        {
            string val = active ? "Enabled" : "Disabled";
            var psi = new ProcessStartInfo("powershell",
                $"-NoProfile -Command \"Set-MpPreference -PUAProtection {val}\"")
            { UseShellExecute = false, CreateNoWindow = true };
            using var p = Process.Start(psi);
            p!.WaitForExit();
        }
    }
}