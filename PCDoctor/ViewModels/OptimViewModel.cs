using CommunityToolkit.Mvvm.ComponentModel;
using PCDoctor.Services;

namespace PCDoctor.ViewModels
{
    public partial class OptimViewModel : ObservableObject
    {
        private readonly OptimService _svc = new();
        private bool _loading;

        [ObservableProperty] private bool hibernationActive;
        [ObservableProperty] private bool fastStartupActive;
        [ObservableProperty] private bool powerThrottlingDisabled;
        [ObservableProperty] private bool memoryCompressionActive;
        [ObservableProperty] private string statusText = "";

        public OptimViewModel() { Sync(); }

        private void Sync()
        {
            _loading = true;
            HibernationActive       = _svc.IsHibernationActive();
            FastStartupActive       = _svc.IsFastStartupActive();
            PowerThrottlingDisabled = _svc.IsPowerThrottlingDisabled();
            MemoryCompressionActive = _svc.IsMemoryCompressionActive();
            _loading = false;
        }

        partial void OnHibernationActiveChanged(bool v)
        {
            if (_loading) return;
            _svc.SetHibernation(v);
            StatusText = v ? "Hibernation activée" : "Hibernation désactivée (espace disque libéré)";
        }

        partial void OnFastStartupActiveChanged(bool v)
        {
            if (_loading) return;
            _svc.SetFastStartup(v);
            StatusText = v ? "Démarrage rapide activé" : "Démarrage rapide désactivé (arrêt complet à chaque extinction)";
        }

        partial void OnPowerThrottlingDisabledChanged(bool v)
        {
            if (_loading) return;
            _svc.SetPowerThrottling(v);
            StatusText = v ? "Power Throttling désactivé - performances maximales" : "Power Throttling activé - économie d'énergie";
        }

        partial void OnMemoryCompressionActiveChanged(bool v)
        {
            if (_loading) return;
            _svc.SetMemoryCompression(v);
            StatusText = v ? "Compression mémoire activée" : "Compression mémoire désactivée";
        }
    }
}
