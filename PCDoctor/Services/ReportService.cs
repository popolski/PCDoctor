using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Win32;

namespace PCDoctor.Services
{
    public class ReportService
    {
        public string Generate()
        {
            var sb = new StringBuilder();
            var now = DateTime.Now;

            // ─── Collecte données ─────────────────────────────────────────────
            var sysInfo = new SystemInfoService();
            var health  = new HealthService();
            var optim   = new OptimService();
            var privacy = new PrivacyService();
            var network = new NetworkService();
            var harden  = new HardeningService();
            var gaming  = new GamingService();
            var explorer= new ExplorerService();

            string osName    = sysInfo.GetOsName();
            string machine   = sysInfo.GetMachineName();
            string uptime    = sysInfo.GetUptime();
            var (total, used, ramPct) = sysInfo.GetRam();
            var disks        = sysInfo.GetDisks();
            var (rtp, av, _) = sysInfo.GetDefender();
            var checks       = health.GetChecks();

            // ─── HTML ─────────────────────────────────────────────────────────
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang=\"fr\"><head><meta charset=\"UTF-8\">");
            sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width,initial-scale=1\">");
            sb.AppendLine($"<title>PCDoctor - Rapport du {now:dd/MM/yyyy HH:mm}</title>");
            sb.AppendLine(Css());
            sb.AppendLine("</head><body>");
            sb.AppendLine($"<div class=\"header\"><h1>PCDoctor</h1><div class=\"sub\">Rapport généré le {now:dddd d MMMM yyyy à HH:mm:ss}</div></div>");
            sb.AppendLine("<div class=\"container\">");

            // -- Infos système
            sb.AppendLine(SectionTitle("Informations système"));
            sb.AppendLine("<div class=\"cards\">");
            sb.AppendLine(Card("Système",   $"<b>{osName}</b><br>{machine}"));
            sb.AppendLine(Card("RAM",        $"{used} Go / {total} Go ({ramPct}%)"));
            sb.AppendLine(Card("Uptime",     uptime));
            sb.AppendLine(Card("Defender",   rtp && av ? "Protection active" : "⚠️ Protection incomplète", rtp && av ? "ok" : "warn"));
            sb.AppendLine("</div>");

            // -- Disques
            sb.AppendLine(SectionTitle("Disques"));
            sb.AppendLine("<table><thead><tr><th>Lettre</th><th>Espace</th><th>Utilisé</th></tr></thead><tbody>");
            foreach (var d in disks)
            {
                var barClass = d.Percent > 90 ? "bad" : d.Percent > 70 ? "warn" : "ok";
                sb.AppendLine($"<tr><td><b>{d.Letter}</b></td><td>{Esc(d.Text)}</td>" +
                              $"<td><div class=\"bar-bg\"><div class=\"bar {barClass}\" style=\"width:{d.Percent}%\"></div></div> {d.Percent}%</td></tr>");
            }
            sb.AppendLine("</tbody></table>");

            // -- Score de santé
            int scoreOk = 0;
            foreach (var c in checks) if (c.IsOk) scoreOk++;
            int scoreTotal = checks.Count;
            double pct2 = scoreTotal > 0 ? scoreOk * 100.0 / scoreTotal : 0;
            string scoreClass = pct2 >= 80 ? "ok" : pct2 >= 50 ? "warn" : "bad";

            sb.AppendLine(SectionTitle("Score de santé"));
            sb.AppendLine($"<div class=\"score {scoreClass}\">{scoreOk} / {scoreTotal}</div>");
            sb.AppendLine("<table><thead><tr><th>Statut</th><th>Vérification</th><th>Catégorie</th><th>Conseil</th></tr></thead><tbody>");
            foreach (var c in checks)
                sb.AppendLine($"<tr class=\"{(c.IsOk ? "ok-row" : "warn-row")}\"><td>{c.Icon}</td><td>{Esc(c.Label)}</td><td>{Esc(c.Category)}</td><td>{Esc(c.Advice)}</td></tr>");
            sb.AppendLine("</tbody></table>");

            // -- Optimisations
            sb.AppendLine(SectionTitle("Optimisations"));
            var optimItems = new List<(string Label, bool Active, string Conseil)>
            {
                ("Hibernation",               optim.IsHibernationActive(),       "OFF recommandé sur SSD pour libérer hiberfil.sys"),
                ("Fast Startup",              optim.IsFastStartupActive(),        "OFF recommandé pour les MAJ et stabilité"),
                ("Power Throttling désactivé",optim.IsPowerThrottlingDisabled(),  "ON = CPU non bridé (meilleures perfs)"),
                ("Compression mémoire",       optim.IsMemoryCompressionActive(),  "ON si RAM < 16 Go"),
                ("Messages boot détaillés",   optim.IsVerboseStatusActive(),      "ON utile pour diagnostic"),
                ("Horloge UTC (dual-boot)",   optim.IsUtcClockActive(),           "ON si dual-boot Linux"),
                ("SysMain (Superfetch)",      optim.IsSysMainActive(),            "OFF conseillé sur SSD"),
                ("Indexation Windows Search", optim.IsSearchIndexActive(),         "OFF sur HDD lent"),
                ("Windows Error Reporting",   optim.IsWerActive(),                "OFF réduit l'activité post-crash"),
            };
            sb.AppendLine(ToggleTable(optimItems));

            // -- Confidentialité
            sb.AppendLine(SectionTitle("Confidentialité"));
            var privItems = new List<(string Label, bool Active, string Conseil)>
            {
                ("Télémétrie",            privacy.IsTelemetryActive(),           "OFF recommandé"),
                ("Cortana",               privacy.IsCortanaActive(),             "OFF recommandé"),
                ("Activity History",      privacy.IsActivityActive(),            "OFF recommandé"),
                ("Publicités ciblées",    privacy.IsAdsActive(),                 "OFF recommandé"),
                ("Advertising ID",        privacy.IsAdIdActive(),                "OFF recommandé"),
                ("Données Office",        privacy.IsOfficeActive(),              "OFF recommandé"),
                ("Wi-Fi Sense",           privacy.IsWifiSenseActive(),           "OFF recommandé"),
                ("Apps arrière-plan",     privacy.IsBackgroundAppsActive(),      "OFF économise la batterie"),
                ("Windows Recall",        privacy.IsRecallActive(),              "OFF recommandé (surveillance IA)"),
                ("Localisation",          privacy.IsLocationActive(),            "OFF si non nécessaire"),
                ("Copilot",               privacy.IsCopilotActive(),             "OFF si non utilisé"),
                ("Recherche IA",          privacy.IsAiSearchActive(),            "OFF si non utilisé"),
                ("Suggestions paramètres",privacy.IsSettingsSuggestionsActive(), "OFF réduit les traces"),
            };
            sb.AppendLine(ToggleTable(privItems));

            // -- Réseau
            sb.AppendLine(SectionTitle("Réseau"));
            var netItems = new List<(string Label, bool Active, string Conseil)>
            {
                ("IPv6 actif",      network.IsIpv6Active(),  "Désactiver si non utilisé"),
                ("DNS over HTTPS",  network.IsDohActive(),   "ON recommandé pour confidentialité DNS"),
            };
            sb.AppendLine(ToggleTable(netItems));

            // -- Hardening
            sb.AppendLine(SectionTitle("Durcissement (Hardening)"));
            var hardItems = new List<(string Label, bool Active, string Conseil)>
            {
                ("LLMNR actif",      harden.IsLlmnrActive(), "OFF recommandé"),
                ("SMBv1 actif",      harden.IsSmb1Active(),  "OFF obligatoire"),
                ("Protection PUA",   harden.IsPuaActive(),   "ON recommandé"),
                ("mDNS actif",       harden.IsMdnsActive(),  "OFF recommandé sur réseau public"),
                ("Bonjour installé", harden.IsBonjourInstalled(), "Désactiver si non utilisé"),
            };
            sb.AppendLine(ToggleTable(hardItems));

            // -- Gaming
            sb.AppendLine(SectionTitle("Gaming"));
            var gameItems = new List<(string Label, bool Active, string Conseil)>
            {
                ("Game Mode",                 gaming.IsGameModeActive(),              "ON pour les jeux"),
                ("HAGS (GPU Scheduling)",     gaming.IsHagsActive(),                 "ON sur GPU récent"),
                ("Xbox Game Bar",             gaming.IsGameBarActive(),               "OFF si non utilisé"),
                ("Algorithme de Nagle (TCP)", !gaming.IsNagleDisabled(),              "OFF réduit la latence réseau"),
            };
            sb.AppendLine(ToggleTable(gameItems));

            // -- Explorateur Windows
            sb.AppendLine(SectionTitle("Explorateur Windows"));
            var explorerItems = new List<(string Label, bool Active, string Conseil)>
            {
                ("Extensions visibles",        explorer.IsFileExtVisible(),    "ON recommandé"),
                ("Fichiers cachés visibles",    explorer.IsHiddenVisible(),     "ON pour admin"),
                ("Fichiers système visibles",   explorer.IsSuperHiddenVisible(),"ON pour admin"),
                ("Chemin complet dans barre",   explorer.IsFullPathInTitle(),   "Utile pour navigation"),
                ("Icones seulement (no vign.)", explorer.IsIconsOnly(),         "ON accélère l'affichage sur HDD"),
            };
            sb.AppendLine(ToggleTable(explorerItems));

            sb.AppendLine("</div>"); // container
            sb.AppendLine($"<div class=\"footer\">Généré par PCDoctor - {now:yyyy}</div>");
            sb.AppendLine("</body></html>");

            // ─── Sauvegarde ───────────────────────────────────────────────────
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string path = Path.Combine(desktop, $"PCDoctor_Rapport_{now:yyyyMMdd_HHmm}.html");
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            Logger.Action($"Rapport HTML genere : {path}");
            return path;
        }

        // ─── Helpers HTML ─────────────────────────────────────────────────────

        private static string SectionTitle(string t) =>
            $"<h2>{t}</h2>";

        private static string Card(string title, string body, string cls = "")
        {
            string c = string.IsNullOrEmpty(cls) ? "" : $" {cls}";
            return $"<div class=\"card{c}\"><div class=\"card-title\">{title}</div>{body}</div>";
        }

        private static string ToggleTable(List<(string Label, bool Active, string Conseil)> items)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<table><thead><tr><th>Fonctionnalité</th><th>État</th><th>Conseil</th></tr></thead><tbody>");
            foreach (var (label, active, conseil) in items)
                sb.AppendLine($"<tr><td>{Esc(label)}</td><td>{(active ? "<span class=\"badge-on\">ON</span>" : "<span class=\"badge-off\">OFF</span>")}</td><td>{Esc(conseil)}</td></tr>");
            sb.AppendLine("</tbody></table>");
            return sb.ToString();
        }

        private static string Esc(string s) =>
            s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

        private static string Css() => @"<style>
*{box-sizing:border-box;margin:0;padding:0}
body{font-family:'Segoe UI',sans-serif;background:#0f0f0f;color:#e8e8e8;font-size:14px}
.header{background:linear-gradient(135deg,#1a73e8,#0d47a1);padding:32px 48px;color:#fff}
.header h1{font-size:2em;letter-spacing:2px}
.header .sub{opacity:.8;margin-top:4px}
.container{padding:32px 48px;max-width:1200px}
h2{font-size:1.2em;font-weight:600;color:#90caf9;margin:28px 0 10px;border-bottom:1px solid #333;padding-bottom:6px}
table{width:100%;border-collapse:collapse;margin-bottom:16px}
th{background:#1e1e1e;color:#90caf9;text-align:left;padding:8px 12px;font-weight:600;font-size:12px;text-transform:uppercase;letter-spacing:.5px}
td{padding:7px 12px;border-bottom:1px solid #2a2a2a}
tr:last-child td{border-bottom:none}
.ok-row td:first-child{color:#4caf50}
.warn-row td:first-child{color:#ff9800}
.badge-on{background:#1b5e20;color:#a5d6a7;padding:2px 8px;border-radius:12px;font-size:12px;font-weight:600}
.badge-off{background:#2a2a2a;color:#757575;padding:2px 8px;border-radius:12px;font-size:12px;font-weight:600}
.cards{display:flex;flex-wrap:wrap;gap:12px;margin-bottom:16px}
.card{background:#1e1e1e;border:1px solid #333;border-radius:8px;padding:16px 20px;min-width:180px;flex:1}
.card.ok{border-color:#2e7d32}
.card.warn{border-color:#e65100}
.card-title{font-size:11px;text-transform:uppercase;letter-spacing:.5px;color:#90caf9;margin-bottom:6px;font-weight:600}
.score{display:inline-block;font-size:2.5em;font-weight:700;padding:8px 24px;border-radius:8px;margin-bottom:12px}
.score.ok{background:#1b5e20;color:#a5d6a7}
.score.warn{background:#e65100;color:#ffcc02}
.score.bad{background:#7f0000;color:#ef9a9a}
.bar-bg{background:#2a2a2a;border-radius:4px;height:8px;display:inline-block;width:80px;vertical-align:middle;margin-right:6px}
.bar{height:8px;border-radius:4px}
.bar.ok{background:#4caf50}
.bar.warn{background:#ff9800}
.bar.bad{background:#f44336}
.footer{text-align:center;padding:24px;color:#444;font-size:12px}
</style>";
    }
}
