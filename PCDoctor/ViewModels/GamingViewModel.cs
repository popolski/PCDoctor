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

        partial void OnHighPerfActiveChanged(bool v)
        {
            if (_loading) return;
            _svc.SetHighPerf(v);
            StatusText = v ? "Plan Hautes performances active" : "Plan Equilibre restaure";
        }
        partial void OnGameModeActiveChanged(bool v)
        {
            if (_loading) return;
            _svc.SetGameMode(v);
            StatusText = v ? "Game Mode active" : "Game Mode desactive";
        }
        partial void OnHagsActiveChanged(bool v)
        {
            if (_loading) return;
            _svc.SetHags(v);
            StatusText = v
                ? "HAGS active - redemarrage necessaire pour prendre effet"
                : "HAGS desactive - redemarrage necessaire pour prendre effet";
        }
        partial void OnGameBarActiveChanged(bool v)
        {
            if (_loading) return;
            _svc.SetGameBar(v);
            StatusText = v ? "Xbox Game Bar active" : "Xbox Game Bar desactive";
        }
        partial void OnGpuPriorityActiveChanged(bool v)
        {
            if (_loading) return;
            _svc.SetGpuPriority(v);
            StatusText = v ? "Priorite GPU optimisee pour les jeux" : "Priorite GPU standard";
        }
        partial void OnMouseAccelActiveChanged(bool v)
        {
            if (_loading) return;
            _svc.SetMouseAccel(v);
            StatusText = v ? "Acceleration souris activee" : "Acceleration souris desactivee (visee directe)";
        }
        partial void OnNagleDisabledChanged(bool v)
        {
            if (_loading) return;
            _svc.SetNagle(v);
            StatusText = v ? "Algorithme de Nagle desactive - latence TCP reduite" : "Algorithme de Nagle reactivé (defaut Windows)";
        }
    }
}
