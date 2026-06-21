using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using PCDoctor.Services;

namespace PCDoctor.ViewModels
{
    public partial class AccueilViewModel : ObservableObject
    {
        private readonly SystemInfoService  _sysInfo  = new();
        private readonly HealthService      _health   = new();
        private readonly ReportService      _report   = new();
        private readonly UpdateCheckService _updater  = new();
        private readonly DispatcherQueue    _dispatcher = DispatcherQueue.GetForCurrentThread();

        [ObservableProperty] private string osName = "";
        [ObservableProperty] private string machineName = "";
        [ObservableProperty] private string ramText = "";
        [ObservableProperty] private int ramPercent;
        [ObservableProperty] private ObservableCollection<DiskInfo> disks = new();
        [ObservableProperty] private string defenderText = "";
        [ObservableProperty] private bool defenderOk;
        [ObservableProperty] private string uptimeText = "";

        // Mise à jour disponible
        [ObservableProperty] private bool updateAvailable;
        [ObservableProperty] private string updateVersion = "";
        [ObservableProperty] private string updateUrl = "";
        [ObservableProperty] private bool isCheckingUpdate;
        [ObservableProperty] private string updateCheckStatus = "";

        [RelayCommand]
        private void OpenUpdateUrl() => Process.Start(new ProcessStartInfo(UpdateUrl) { UseShellExecute = true });

        [RelayCommand(CanExecute = nameof(CanCheckUpdate))]
        private async Task CheckUpdate()
        {
            IsCheckingUpdate  = true;
            UpdateCheckStatus = "Vérification en cours...";
            UpdateAvailable   = false;
            CheckUpdateCommand.NotifyCanExecuteChanged();

            var info = await _updater.CheckAsync();

            _dispatcher.TryEnqueue(() =>
            {
                if (info is not null)
                {
                    UpdateVersion     = info.Version;
                    UpdateUrl         = info.Url;
                    UpdateAvailable   = true;
                    UpdateCheckStatus = $"Mise à jour v{info.Version} disponible !";
                }
                else
                {
                    UpdateCheckStatus = "PCDoctor est à jour.";
                }
                IsCheckingUpdate = false;
                CheckUpdateCommand.NotifyCanExecuteChanged();
            });
        }
        private bool CanCheckUpdate() => !IsCheckingUpdate;

        // Navigation depuis une recommandation
        [RelayCommand]
        private void GoToCheck(Services.HealthCheck check)
        {
            if (!string.IsNullOrEmpty(check.PageTag))
                AppState.RequestNavigate(check.PageTag);
        }

        // Rapport
        [ObservableProperty] private bool isGeneratingReport;
        [ObservableProperty] private string reportStatus = "";
        public bool CanGenerateReport => !IsGeneratingReport;
        partial void OnIsGeneratingReportChanged(bool value) => OnPropertyChanged(nameof(CanGenerateReport));

        [RelayCommand]
        private async Task GenerateReport()
        {
            IsGeneratingReport = true;
            ReportStatus = "Génération du rapport...";
            try
            {
                string path = await Task.Run(() => _report.Generate());
                ReportStatus = "Rapport créé sur le Bureau.";
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            }
            catch (Exception e)
            {
                ReportStatus = $"Erreur : {e.Message}";
            }
            finally { IsGeneratingReport = false; }
        }

        // Score de santé
        [ObservableProperty] private int scoreOk;
        [ObservableProperty] private int scoreTotal;
        [ObservableProperty] private string scoreText = "Analyse en cours...";
        [ObservableProperty] private ObservableCollection<HealthCheck> healthChecks = new();
        [ObservableProperty] private bool healthLoading = true;

        public Brush ScoreColor
        {
            get
            {
                if (ScoreTotal == 0) return new SolidColorBrush(Color.FromArgb(255, 128, 128, 128));
                double pct = (double)ScoreOk / ScoreTotal;
                return pct >= 1.0
                    ? new SolidColorBrush(Color.FromArgb(255, 76,  175, 80))
                    : pct >= 0.7
                    ? new SolidColorBrush(Color.FromArgb(255, 255, 152, 0))
                    : new SolidColorBrush(Color.FromArgb(255, 244, 67,  54));
            }
        }

        public AccueilViewModel()
        {
            LoadData();
            foreach (var d in _sysInfo.GetDisks()) Disks.Add(d);
            var (rtp, av, dtext) = _sysInfo.GetDefender();
            DefenderText = dtext;
            DefenderOk   = rtp && av;
            UptimeText   = _sysInfo.GetUptime();

            _ = LoadHealthAsync();
            _ = CheckUpdate();
        }

        private void LoadData()
        {
            OsName      = _sysInfo.GetOsName();
            MachineName = _sysInfo.GetMachineName();
            var (total, used, pct) = _sysInfo.GetRam();
            RamText    = $"{used} Go / {total} Go";
            RamPercent = pct;
        }

        private async Task LoadHealthAsync()
        {
            HealthLoading = true;
            var checks = await Task.Run(() => _health.GetChecks());

            HealthChecks.Clear();
            int ok = 0;
            foreach (var c in checks)
            {
                HealthChecks.Add(c);
                if (c.IsOk) ok++;
            }
            ScoreOk    = ok;
            ScoreTotal = checks.Count;
            ScoreText  = ok == checks.Count
                ? "Toutes les recommandations sont appliquées."
                : $"{checks.Count - ok} recommandation(s) en attente.";
            HealthLoading = false;
            OnPropertyChanged(nameof(ScoreColor));
            AppState.NotifyScore(ok, checks.Count);
        }
    }
}
