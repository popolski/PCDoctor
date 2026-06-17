using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using PCDoctor.Services;

namespace PCDoctor.ViewModels
{
    public partial class EtatViewModel : ObservableObject
    {
        private readonly MonitorService _svc = new();
        private DispatcherTimer? _timer;

        [ObservableProperty] private int cpuPercent;
        [ObservableProperty] private string cpuText = "";
        [ObservableProperty] private int ramPercent;
        [ObservableProperty] private string ramText = "";
        [ObservableProperty] private string uptimeText = "";
        [ObservableProperty] private ObservableCollection<DetailRow> details = new();
        [ObservableProperty] private ObservableCollection<ProcessInfo> processes = new();

        public EtatViewModel()
        {
            LoadStatic();
            StartTimer();
        }

        private void LoadStatic()
        {
            UptimeText = _svc.GetUptime();
            Details.Clear();
            foreach (var kv in _svc.GetDetails())
                Details.Add(new DetailRow { Key = kv.Key, Value = kv.Value });
            RefreshProcesses();
        }

        private void StartTimer()
        {
            _timer = new DispatcherTimer { Interval = System.TimeSpan.FromSeconds(2) };
            _timer.Tick += (s, e) => Tick();
            _timer.Start();
            Tick(); // première lecture immédiate
        }

        private int _tickCount;
        private void Tick()
        {
            CpuPercent = _svc.GetCpuPercent();
            CpuText = $"{CpuPercent} %";
            var (used, total, pct) = _svc.GetRam();
            RamPercent = pct;
            RamText = $"{used} Go / {total} Go";
            // Rafraîchir les process toutes les ~10s (5 ticks) pour ne pas surcharger
            if (++_tickCount % 5 == 0) RefreshProcesses();
        }

        private void RefreshProcesses()
        {
            Processes.Clear();
            foreach (var p in _svc.GetTopProcesses(10)) Processes.Add(p);
        }
    }

    public class DetailRow
    {
        public string Key { get; set; } = "";
        public string Value { get; set; } = "";
    }
}