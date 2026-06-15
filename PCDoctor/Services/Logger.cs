using System;
using System.IO;

namespace PCDoctor.Services
{
    // Logger simple : écrit les actions dans un fichier horodaté dans %APPDATA%\PCDoctor.
    public static class Logger
    {
        private static readonly string LogFolder =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PCDoctor");

        private static readonly string LogFile =
            Path.Combine(LogFolder, $"PCDoctor_{DateTime.Now:yyyyMMdd}.log");

        static Logger()
        {
            try { Directory.CreateDirectory(LogFolder); } catch { }
        }

        public static void Info(string message) => Write("INFO", message);
        public static void Warn(string message) => Write("WARN", message);
        public static void Error(string message) => Write("ERROR", message);

        // Action sensible (modification système) : tracée distinctement
        public static void Action(string message) => Write("ACTION", message);

        private static void Write(string level, string message)
        {
            try
            {
                string line = $"[{DateTime.Now:HH:mm:ss}] [{level}] {message}";
                File.AppendAllText(LogFile, line + Environment.NewLine);
            }
            catch { /* ne jamais planter l'app à cause du log */ }
        }

        public static string GetLogPath() => LogFile;
    }
}