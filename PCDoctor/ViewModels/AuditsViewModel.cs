using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PCDoctor.Services;
using Windows.ApplicationModel.DataTransfer;

namespace PCDoctor.ViewModels
{
    public partial class AuditsViewModel : ObservableObject
    {
        private readonly AuditService _audit = new();

        [ObservableProperty] private string currentTitle    = "Sélectionnez un audit";
        [ObservableProperty] private string currentSubtitle = "";
        [ObservableProperty] private string h1 = "";
        [ObservableProperty] private string h2 = "";
        [ObservableProperty] private string h3 = "";
        [ObservableProperty] private string h4 = "";
        [ObservableProperty] private ObservableCollection<AuditRow> rows = new();
        [ObservableProperty] private bool isBusy;
        public bool CanRun => !IsBusy;
        partial void OnIsBusyChanged(bool value) => OnPropertyChanged(nameof(CanRun));

        // Audits rapides (registre / PS court) - sync ok
        [RelayCommand(CanExecute = nameof(CanRun))] private void RunDefender()   => Apply(_audit.AuditDefender());
        [RelayCommand(CanExecute = nameof(CanRun))] private void RunSystemInfo() => Apply(_audit.AuditSystemInfo());

        // Audits lents (scan fichiers / PS lourd) - async
        [RelayCommand(CanExecute = nameof(CanRun))] private Task RunServices()         => RunAsync(_audit.AuditServices);
        [RelayCommand(CanExecute = nameof(CanRun))] private Task RunDrivers()          => RunAsync(_audit.AuditDrivers);
        [RelayCommand(CanExecute = nameof(CanRun))] private Task RunShellExtensions()  => RunAsync(_audit.AuditShellExtensions,  notify: true);
        [RelayCommand(CanExecute = nameof(CanRun))] private Task RunLargeFiles()       => RunAsync(_audit.AuditLargeFiles,        notify: true);
        [RelayCommand(CanExecute = nameof(CanRun))] private Task RunBrowserExtensions()=> RunAsync(_audit.AuditBrowserExtensions, notify: true);

        private async Task RunAsync(System.Func<AuditResult> work, bool notify = false)
        {
            IsBusy = true;
            CurrentSubtitle = "Analyse en cours...";
            var res = await Task.Run(work);
            Apply(res);
            IsBusy = false;
            if (notify) SendToast(res.Title, $"{Rows.Count} élément(s) trouvé(s)");
        }

        private static bool _notifRegistered;
        private static void SendToast(string title, string body)
        {
            try
            {
                if (!_notifRegistered)
                {
                    Microsoft.Windows.AppNotifications.AppNotificationManager.Default.Register();
                    _notifRegistered = true;
                }
                var xml = $@"<toast><visual><binding template=""ToastGeneric"">
                    <text>🩺 PCDoctor — {System.Security.SecurityElement.Escape(title)}</text>
                    <text>{System.Security.SecurityElement.Escape(body)}</text>
                    </binding></visual></toast>";
                var notif = new Microsoft.Windows.AppNotifications.AppNotification(xml);
                Microsoft.Windows.AppNotifications.AppNotificationManager.Default.Show(notif);
            }
            catch { }
        }

        [RelayCommand]
        private void ExportCsv()
        {
            if (Rows.Count == 0) return;
            var sb = new StringBuilder();
            var heads = new[] { H1, H2, H3, H4 }.Where(h => !string.IsNullOrEmpty(h));
            if (heads.Any()) sb.AppendLine(string.Join(";", heads));
            foreach (var r in Rows)
            {
                var cols = new[] { r.Col1, r.Col2, r.Col3, r.Col4 }
                    .Select(c => c.Contains(';') || c.Contains('\n')
                        ? $"\"{c.Replace("\"", "\"\"")}\"" : c);
                sb.AppendLine(string.Join(";", cols).TrimEnd(';'));
            }
            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                $"PCDoctor_Audit_{DateTime.Now:yyyyMMdd_HHmm}.csv");
            File.WriteAllText(path, sb.ToString(), System.Text.Encoding.UTF8);
            Logger.Action($"Audit exporté CSV : {Path.GetFileName(path)}");
        }

        [RelayCommand]
        private void CopyResults()
        {
            if (Rows.Count == 0) return;
            var sb = new StringBuilder();
            sb.AppendLine(CurrentTitle);
            sb.AppendLine(CurrentSubtitle);
            sb.AppendLine(new string('-', 60));
            foreach (var r in Rows)
                sb.AppendLine($"{r.Col1}\t{r.Col2}\t{r.Col3}\t{r.Col4}".TrimEnd());
            var pkg = new DataPackage();
            pkg.SetText(sb.ToString());
            Clipboard.SetContent(pkg);
        }

        private void Apply(AuditResult res)
        {
            CurrentTitle    = res.Title;
            CurrentSubtitle = res.Subtitle;
            H1 = res.H1; H2 = res.H2; H3 = res.H3; H4 = res.H4;
            Rows.Clear();
            foreach (var row in res.Rows) Rows.Add(row);
        }
    }
}
