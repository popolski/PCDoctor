using CommunityToolkit.Mvvm.ComponentModel;
using PCDoctor.Services;

namespace PCDoctor.ViewModels
{
    public partial class HardeningViewModel : ObservableObject
    {
        private readonly HardeningService _svc = new();
        private bool _loading; // évite de déclencher les actions pendant la synchro initiale

        [ObservableProperty] private bool llmnrActive;
        [ObservableProperty] private bool smb1Active;
        [ObservableProperty] private bool puaActive;
        [ObservableProperty] private string statusText = "";

        public HardeningViewModel()
        {
            Sync();
        }

        // Lit l'état réel et met à jour les toggles (sans déclencher d'action)
        private void Sync()
        {
            _loading = true;
            LlmnrActive = _svc.IsLlmnrActive();
            Smb1Active = _svc.IsSmb1Active();
            PuaActive = _svc.IsPuaActive();
            _loading = false;
        }

        // Les méthodes On...Changed sont générées par le toolkit : appelées
        // automatiquement quand la propriété change (donc quand l'utilisateur bascule le toggle).
        partial void OnLlmnrActiveChanged(bool value)
        {
            if (_loading) return;
            _svc.SetLlmnr(value);
            StatusText = value ? "LLMNR réactivé (défaut)" : "LLMNR désactivé (sécurisé)";
        }

        partial void OnSmb1ActiveChanged(bool value)
        {
            if (_loading) return;
            _svc.SetSmb1(value);
            StatusText = value ? "SMBv1 réactivé (reboot requis, déconseillé)" : "SMBv1 désactivé (reboot requis)";
        }

        partial void OnPuaActiveChanged(bool value)
        {
            if (_loading) return;
            _svc.SetPua(value);
            StatusText = value ? "Protection PUA activée" : "Protection PUA désactivée";
        }
    }
}