using CommunityToolkit.Mvvm.ComponentModel;
using PCDoctor.Services;

namespace PCDoctor.ViewModels
{
    public partial class GamingViewModel : ObservableObject
    {
        private readonly GamingService _svc = new();
        private bool _loading;

        [ObservableProperty] private bool highPerfActive;
        [ObservableProperty] private bool gameModeActive;
        [ObservableProperty] private bool hagsActive;
        [ObservableProperty] private bool gameBarActive;
        [ObservableProperty] private bool gpuPriorityActive;
        [ObservableProperty] private bool mouseAccelActive;
        [ObservableProperty] private bool nagleDisabled;
        [ObservableProperty] private string statusText = "";

        public GamingViewModel() { Sync(); }

        private void Sync()
        {
            _loading = true;
            HighPerfActive    = _svc.IsHighPerfActive();
            GameModeActive    = _svc.IsGameModeActive();
            HagsActive        = _svc.IsHagsActive();
            GameBarActive     = _svc.IsGameBarActive();
            GpuPriorityActive = _svc.IsGpuPriorityActive();
            MouseAccelActive  = _svc.IsMouseAccelActive();
            NagleDisabled     = _svc.IsNagleDisabled();
            _loading = false;
        }

        partial void OnHighPerfActiveChanged(bool value)
        {
            if (_loading) return;
            _svc.SetHighPerf(value);
            StatusText = value ? "Plan Hautes performances active" : "Plan Equilibre restaure";
        }
        partial void OnGameModeActiveChanged(bool value)
        {
            if (_loading) return;
            _svc.SetGameMode(value);
            StatusText = value ? "Game Mode active" : "Game Mode desactive";
        }
        partial void OnHagsActiveChanged(bool value)
        {
            if (_loading) return;
            _svc.SetHags(value);
            StatusText = value
                ? "HAGS active - redemarrage necessaire pour prendre effet"
                : "HAGS desactive - redemarrage necessaire pour prendre effet";
        }
        partial void OnGameBarActiveChanged(bool value)
        {
            if (_loading) return;
            _svc.SetGameBar(value);
            StatusText = value ? "Xbox Game Bar active" : "Xbox Game Bar desactive";
        }
        partial void OnGpuPriorityActiveChanged(bool value)
        {
            if (_loading) return;
            _svc.SetGpuPriority(value);
            StatusText = value ? "Priorite GPU optimisee pour les jeux" : "Priorite GPU standard";
        }
        partial void OnMouseAccelActiveChanged(bool value)
        {
            if (_loading) return;
            _svc.SetMouseAccel(value);
            StatusText = value ? "Acceleration souris activee" : "Acceleration souris desactivee (visee directe)";
        }
        partial void OnNagleDisabledChanged(bool value)
        {
            if (_loading) return;
            _svc.SetNagle(value);
            StatusText = value ? "Algorithme de Nagle desactive - latence TCP reduite" : "Algorithme de Nagle reactivé (defaut Windows)";
        }
    }
}
