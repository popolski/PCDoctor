using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using PCDoctor.Services;

namespace PCDoctor.ViewModels
{
    public partial class AccueilViewModel : ObservableObject
    {
        private readonly SystemInfoService _sysInfo = new();
        private readonly HealthService     _health  = new();

        [ObservableProperty] private string osName = "";
        [ObservableProperty] private string machineName = "";
        [ObservableProperty] private string ramText = "";
        [ObservableProperty] private int ramPercent;
        [ObservableProperty] private ObservableCollection<DiskInfo> disks = new();
        [ObservableProperty] private string defenderText = "";
        [ObservableProperty] private bool defenderOk;
        [ObservableProperty] private string uptimeText = "";

        // Score de santé
        [ObservableProperty] private int scoreOk;
        [ObservableProperty] private int scoreTotal;
        [ObservableProperty] private string scoreText = "Analyse en cours...";
        [ObservableProperty] private ObservableCollection<HealthCheck> healthChecks = new();
        [ObservableProperty] private bool healthLoading = true;

        public AccueilViewModel()
        {
            LoadData();
            foreach (var d in _sysInfo.GetDisks()) Disks.Add(d);
            var (rtp, av, dtext) = _sysInfo.GetDefender();
            DefenderText = dtext;
            DefenderOk   = rtp && av;
            UptimeText   = _sysInfo.GetUptime();

            _ = LoadHealthAsync();
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
        }
    }
}
