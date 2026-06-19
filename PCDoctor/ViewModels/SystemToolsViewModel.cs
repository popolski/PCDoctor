using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using PCDoctor.Services;

namespace PCDoctor.ViewModels
{
    public partial class SystemToolsViewModel : ObservableObject
    {
        private readonly SystemToolsService _svc        = new();
        private readonly DispatcherQueue    _dispatcher = DispatcherQueue.GetForCurrentThread();
        private readonly StringBuilder      _log        = new();

        [ObservableProperty] private string statusText  = "Choisissez un outil à lancer.";
        [ObservableProperty] private string logText     = "";
        [ObservableProperty] private bool   isRunning;
        [ObservableProperty] private bool   canRun      = true;

        [RelayCommand]
        private async Task RunSfc()
        {
            Begin("SFC en cours... (5-10 min)");
            await Task.Run(() => _svc.RunSfc(AppendLog, code =>
                Done(code == 0 ? "SFC termine sans erreur." : $"SFC termine avec le code {code}.")));
        }

        [RelayCommand]
        private async Task RunDism()
        {
            Begin("DISM RestoreHealth en cours... (15-30 min)");
            await Task.Run(() => _svc.RunDism(AppendLog, code =>
                Done(code == 0 ? "DISM termine sans erreur." : $"DISM termine avec le code {code}.")));
        }

        [RelayCommand]
        private async Task CreateRestorePoint()
        {
            Begin("Creation du point de restauration...");
            var (ok, msg) = await Task.Run(() => _svc.CreateRestorePoint());
            AppendLog(msg);
            Done(msg);
        }

        // ─── Helpers ───

        private void Begin(string status)
        {
            IsRunning  = true;
            CanRun     = false;
            StatusText = status;
            _log.Clear();
            LogText    = "";
        }

        private void Done(string status)
        {
            _dispatcher.TryEnqueue(() =>
            {
                IsRunning  = false;
                CanRun     = true;
                StatusText = status;
            });
        }

        private void AppendLog(string line)
        {
            _dispatcher.TryEnqueue(() =>
            {
                _log.AppendLine(line);
                LogText = _log.ToString();
            });
        }
    }
}
