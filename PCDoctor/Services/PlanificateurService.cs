using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;

namespace PCDoctor.Services
{
    public class ScheduledTaskItem
    {
        public string Name     { get; set; } = "";
        public string Path     { get; set; } = "";
        public string Author   { get; set; } = "";
        public string State    { get; set; } = "";
        public bool   IsSelected { get; set; } = false;

        // Etat lisible pour l'UI
        public bool IsDisabled => State is "Desactive" or "Disabled";
    }

    public class PlanificateurService
    {
        public List<ScheduledTaskItem> GetThirdPartyTasks()
        {
            var result = new List<ScheduledTaskItem>();
            try
            {
                // Force tableau JSON meme pour un seul element
                var cmd = @"@(Get-ScheduledTask | Where-Object { $_.TaskPath -notlike '\Microsoft\*' } | Select-Object TaskName, TaskPath, State, Author) | ConvertTo-Json -Compress";
                var json = RunPsOutput(cmd);
                if (string.IsNullOrWhiteSpace(json)) return result;

                using var doc = JsonDocument.Parse(json);
                var elements = doc.RootElement.ValueKind == JsonValueKind.Array
                    ? doc.RootElement.EnumerateArray()
                    : (IEnumerable<JsonElement>)new[] { doc.RootElement };

                foreach (var el in elements)
                {
                    var name = GetString(el, "TaskName");
                    if (string.IsNullOrWhiteSpace(name)) continue;
                    result.Add(new ScheduledTaskItem
                    {
                        Name   = name,
                        Path   = GetString(el, "TaskPath"),
                        Author = GetString(el, "Author"),
                        State  = ParseState(el)
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"PlanificateurService.GetThirdPartyTasks : {ex.Message}");
            }
            return result;
        }

        public void EnableTask(string taskName, string taskPath)
        {
            RunPs($"Enable-ScheduledTask -TaskName '{EscapePs(taskName)}' -TaskPath '{EscapePs(taskPath)}'");
            Logger.Action($"Tache activee : {taskPath}{taskName}");
        }

        public void DisableTask(string taskName, string taskPath)
        {
            RunPs($"Disable-ScheduledTask -TaskName '{EscapePs(taskName)}' -TaskPath '{EscapePs(taskPath)}'");
            Logger.Action($"Tache desactivee : {taskPath}{taskName}");
        }

        public void DeleteTask(string taskName, string taskPath)
        {
            RunPs($"Unregister-ScheduledTask -TaskName '{EscapePs(taskName)}' -TaskPath '{EscapePs(taskPath)}' -Confirm:$false");
            Logger.Action($"Tache supprimee : {taskPath}{taskName}");
        }

        // ─── Helpers ───
        private static string GetString(JsonElement el, string prop)
        {
            if (!el.TryGetProperty(prop, out var v)) return "";
            return v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";
        }

        private static string ParseState(JsonElement el)
        {
            if (!el.TryGetProperty("State", out var s)) return "?";
            if (s.ValueKind == JsonValueKind.Number)
            {
                return s.GetInt32() switch
                {
                    1 => "Desactive",
                    2 => "En attente",
                    3 => "Pret",
                    4 => "En cours",
                    _ => s.GetInt32().ToString()
                };
            }
            if (s.ValueKind == JsonValueKind.String)
            {
                return s.GetString() switch
                {
                    "Disabled" => "Desactive",
                    "Ready"    => "Pret",
                    "Running"  => "En cours",
                    "Queued"   => "En attente",
                    var other  => other ?? "?"
                };
            }
            return "?";
        }

        // Echappe les apostrophes pour les arguments PowerShell entre guillemets simples
        private static string EscapePs(string s) => s.Replace("'", "''");

        private static string RunPsOutput(string cmd)
        {
            try
            {
                var psi = new ProcessStartInfo("powershell", $"-NoProfile -Command \"{cmd}\"")
                    { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true };
                using var p = Process.Start(psi)!;
                var output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
                return output;
            }
            catch { return ""; }
        }

        private static void RunPs(string cmd)
        {
            try
            {
                var psi = new ProcessStartInfo("powershell", $"-NoProfile -Command \"{cmd}\"")
                    { UseShellExecute = false, CreateNoWindow = true };
                using var p = Process.Start(psi);
                p?.WaitForExit();
            }
            catch { }
        }
    }
}
