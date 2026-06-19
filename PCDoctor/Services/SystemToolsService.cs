using System;
using System.Diagnostics;

namespace PCDoctor.Services
{
    public class SystemToolsService
    {
        // SFC /scannow — vérifie et répare les fichiers système
        public void RunSfc(Action<string> onLine, Action<int> onExit)
            => RunLive("sfc", "/scannow", onLine, onExit);

        // DISM RestoreHealth — répare l'image Windows
        public void RunDism(Action<string> onLine, Action<int> onExit)
            => RunLive("dism", "/online /cleanup-image /restorehealth", onLine, onExit);

        // Point de restauration système
        public (bool ok, string message) CreateRestorePoint()
        {
            try
            {
                var psi = new ProcessStartInfo("powershell",
                    "-NoProfile -NonInteractive -Command \"" +
                    "Enable-ComputerRestore -Drive 'C:\\' -ErrorAction SilentlyContinue; " +
                    "Checkpoint-Computer -Description 'PCDoctor - point avant maintenance' " +
                    "-RestorePointType MODIFY_SETTINGS -ErrorAction Stop\"")
                {
                    UseShellExecute        = false,
                    CreateNoWindow         = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true
                };
                using var p = Process.Start(psi)!;
                string err = p.StandardError.ReadToEnd();
                p.WaitForExit(60_000);
                return p.ExitCode == 0
                    ? (true,  "Point de restauration créé avec succès.")
                    : (false, $"Echec (code {p.ExitCode}){(string.IsNullOrWhiteSpace(err) ? "" : " : " + err.Trim())}");
            }
            catch (Exception ex) { return (false, $"Erreur : {ex.Message}"); }
        }

        private static void RunLive(string exe, string args, Action<string> onLine, Action<int> onExit)
        {
            var psi = new ProcessStartInfo(exe, args)
            {
                UseShellExecute        = false,
                CreateNoWindow         = true,
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                StandardOutputEncoding = System.Text.Encoding.Unicode
            };
            using var p = Process.Start(psi)!;
            p.OutputDataReceived += (_, e) => { if (e.Data != null) onLine(e.Data); };
            p.ErrorDataReceived  += (_, e) => { if (e.Data != null) onLine(e.Data); };
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();
            p.WaitForExit(3_600_000); // 1h max
            onExit(p.ExitCode);
        }
    }
}
