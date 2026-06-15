using CommunityToolkit.Mvvm.ComponentModel;
using PCDoctor.Services;

namespace PCDoctor.ViewModels
{
    public partial class OptimViewModel : ObservableObject
    {
        private readonly OptimService _svc = new();
        private bool _loading;

        [ObservableProperty] private bool hibernationActive;
        [ObservableProperty] private string statusText = "";

        public OptimViewModel() { Sync(); }

        private void Sync()
        {
            _loading = true;
            HibernationActive = _svc.IsHibernationActive();
            _loading = false;
        }

        partial void OnHibernationActiveChanged(bool v) { if (_loading) return; _svc.SetHibernation(v); StatusText = v ? "Hibernation activée" : "Hibernation désactivée (espace disque libéré)"; }
    }
}