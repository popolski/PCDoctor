using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PCDoctor.Services;

namespace PCDoctor.ViewModels
{
    public partial class ExplorerViewModel : ObservableObject
    {
        private readonly ExplorerService _svc = new();
        private bool _loading;

        [ObservableProperty] private bool fileExtVisible;
        [ObservableProperty] private bool hiddenVisible;
        [ObservableProperty] private bool superHiddenVisible;
        [ObservableProperty] private bool fullPathInTitle;
        [ObservableProperty] private bool iconsOnly;
        [ObservableProperty] private bool endTaskEnabled;
        [ObservableProperty] private bool numLockOnBoot;
        [ObservableProperty] private string statusText = "";

        public ExplorerViewModel() { Sync(); }

        private void Sync()
        {
            _loading = true;
            FileExtVisible     = _svc.IsFileExtVisible();
            HiddenVisible      = _svc.IsHiddenVisible();
            SuperHiddenVisible = _svc.IsSuperHiddenVisible();
            FullPathInTitle    = _svc.IsFullPathInTitle();
            IconsOnly          = _svc.IsIconsOnly();
            EndTaskEnabled     = _svc.IsEndTaskEnabled();
            NumLockOnBoot      = _svc.IsNumLockOnBoot();
            _loading = false;
        }

        partial void OnFileExtVisibleChanged(bool v)     { if (_loading) return; _svc.SetFileExtVisible(v);     StatusText = v ? "Extensions de fichiers visibles" : "Extensions masquées"; }
        partial void OnHiddenVisibleChanged(bool v)      { if (_loading) return; _svc.SetHiddenVisible(v);      StatusText = v ? "Fichiers cachés visibles" : "Fichiers cachés masqués"; }
        partial void OnSuperHiddenVisibleChanged(bool v) { if (_loading) return; _svc.SetSuperHiddenVisible(v); StatusText = v ? "Fichiers système visibles" : "Fichiers système masqués"; }
        partial void OnFullPathInTitleChanged(bool v)    { if (_loading) return; _svc.SetFullPathInTitle(v);    StatusText = v ? "Chemin complet dans la barre de titre" : "Chemin complet désactivé"; }
        partial void OnIconsOnlyChanged(bool v)          { if (_loading) return; _svc.SetIconsOnly(v);          StatusText = v ? "Vignettes désactivées (icônes seules)" : "Vignettes activées"; }
        partial void OnEndTaskEnabledChanged(bool v)     { if (_loading) return; _svc.SetEndTask(v);            StatusText = v ? "\"Fin de tâche\" disponible dans la barre des tâches" : "\"Fin de tâche\" masqué"; }
        partial void OnNumLockOnBootChanged(bool v)      { if (_loading) return; _svc.SetNumLockOnBoot(v);      StatusText = v ? "NumLock activé au démarrage" : "NumLock désactivé au démarrage"; }

        [RelayCommand]
        private void RestartExplorer()
        {
            _svc.RestartExplorer();
            StatusText = "Explorateur Windows redémarré - changements appliqués.";
        }
    }
}
