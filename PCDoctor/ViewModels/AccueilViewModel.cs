using CommunityToolkit.Mvvm.ComponentModel;
using PCDoctor.Services;

namespace PCDoctor.ViewModels
{
    // [ObservableObject] + [ObservableProperty] = le toolkit génère
    // automatiquement le code de notification UI. Tu écris juste les propriétés.
    public partial class AccueilViewModel : ObservableObject
    {
        private readonly SystemInfoService _sysInfo = new();

        [ObservableProperty] private string osName = "";
        [ObservableProperty] private string machineName = "";
        [ObservableProperty] private string ramText = "";
        [ObservableProperty] private int ramPercent;
        [ObservableProperty] private System.Collections.ObjectModel.ObservableCollection<Services.DiskInfo> disks = new();
        [ObservableProperty] private string defenderText = "";
        [ObservableProperty] private bool defenderOk;
        [ObservableProperty] private string uptimeText = "";

        public AccueilViewModel()
        {
            LoadData();
            foreach (var d in _sysInfo.GetDisks()) Disks.Add(d);
            var (rtp, av, dtext) = _sysInfo.GetDefender();
            DefenderText = dtext;
            DefenderOk = rtp && av;
            UptimeText = _sysInfo.GetUptime();
        }

        private void LoadData()
        {
            OsName = _sysInfo.GetOsName();
            MachineName = _sysInfo.GetMachineName();
            var (total, used, pct) = _sysInfo.GetRam();
            RamText = $"{used} Go / {total} Go";
            RamPercent = pct;
        }
    }
}