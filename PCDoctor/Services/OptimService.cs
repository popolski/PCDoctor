using System;
using System.Diagnostics;

namespace PCDoctor.Services
{
    public class OptimService
    {
        // Hibernation : active si HibernateEnabled = 1
        public bool IsHibernationActive()
        {
            var v = RegistryHelper.GetDword(@"SYSTEM\CurrentControlSet\Control\Power", "HibernateEnabled");
            return v == 1;
        }
        public void SetHibernation(bool active)
        {
            RunCmd($"powercfg /h {(active ? "on" : "off")}");
            Logger.Action($"Hibernation {(active ? "activée" : "désactivée")}");
        }

        private void RunCmd(string cmd)
        {
            try
            {
                var psi = new ProcessStartInfo("cmd.exe", "/c " + cmd)
                { UseShellExecute = false, CreateNoWindow = true };
                using var p = Process.Start(psi);
                p!.WaitForExit();
            }
            catch (Exception e) { Logger.Warn($"OptimService : {e.Message}"); }
        }
    }
}