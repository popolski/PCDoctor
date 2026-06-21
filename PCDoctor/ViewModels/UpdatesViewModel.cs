using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PCDoctor.Services;

namespace PCDoctor.ViewModels
{
    public partial class UpdatesViewModel : ObservableObject
    {
        private readonly UpdatesService _svc = new();

        [ObservableProperty] private string lastUpdate     = "Chargement...";
        [ObservableProperty] private string rebootText     = "";
        [ObservableProperty] private string pendingText    = "Cliquez sur Vérifier pour lancer la recherche (peut prendre 20-30 secondes).";
        [ObservableProperty] private bool   isChecking;
        [ObservableProperty] private bool   canCheck       = true;
        [ObservableProperty] private bool   featureUpdatesBlocked;
        [ObservableProperty] private bool   updatesPaused;
        private bool _loading;

        public UpdatesViewModel()
        {
            LastUpdate  = _svc.GetLastInstalledUpdate();
            bool reboot = _svc.IsRebootRequired();
            RebootText  = reboot
                ? "Oui - un redémarrage est en attente pour appliquer une mise à jour."
                : "Non - aucun redémarrage nécessaire.";
            _loading = true;
            FeatureUpdatesBlocked = _svc.IsFeatureUpdatesBlocked();
            UpdatesPaused         = _svc.IsUpdatesPaused();
            _loading = false;
        }

        partial void OnFeatureUpdatesBlockedChanged(bool value) { if (_loading) return; _svc.SetBlockFeatureUpdates(value); }
        partial void OnUpdatesPausedChanged(bool value)         { if (_loading) return; _svc.SetPauseUpdates(value); }

        [RelayCommand]
        private async Task CheckPending()
        {
            IsChecking  = true;
            CanCheck    = false;
            PendingText = "Recherche en cours...";

            int count   = await _svc.GetPendingUpdatesCountAsync();
            PendingText = count switch
            {
                -1  => "Impossible de vérifier (service Windows Update inaccessible).",
                0   => "Aucune mise à jour en attente. Système à jour.",
                1   => "1 mise à jour en attente.",
                var n => $"{n} mises à jour en attente."
            };

            IsChecking = false;
            CanCheck   = true;
        }

        [RelayCommand] private void OpenWindowsUpdate() => _svc.OpenWindowsUpdate();
        [RelayCommand] private void OpenHistory()        => _svc.OpenHistory();
    }
}
