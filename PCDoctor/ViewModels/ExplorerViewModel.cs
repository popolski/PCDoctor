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
        // Barre des tâches
        [ObservableProperty] private bool taskbarCentered;
        [ObservableProperty] private int  searchBarMode;
        [ObservableProperty] private bool widgetsEnabled;
        [ObservableProperty] private bool classicContextMenu;
        [ObservableProperty] private string statusText = "";

        public ExplorerViewModel() { Sync(); }

        private void Sync()
        {
            _loading = true;
            FileExtVisible      = _svc.IsFileExtVisible();
            HiddenVisible       = _svc.IsHiddenVisible();
            SuperHiddenVisible  = _svc.IsSuperHiddenVisible();
            FullPathInTitle     = _svc.IsFullPathInTitle();
            IconsOnly           = _svc.IsIconsOnly();
            EndTaskEnabled      = _svc.IsEndTaskEnabled();
            NumLockOnBoot       = _svc.IsNumLockOnBoot();
            TaskbarCentered     = _svc.IsTaskbarCentered();
            SearchBarMode       = _svc.GetSearchBarMode();
            WidgetsEnabled      = _svc.IsWidgetsEnabled();
            ClassicContextMenu  = _svc.IsClassicContextMenu();
            _loading = false;
        }

        partial void OnFileExtVisibleChanged(bool v)     { if (_loading) return; _svc.SetFileExtVisible(v);     StatusText = v ? "Extensions de fichiers visibles" : "Extensions masquées"; }
        partial void OnHiddenVisibleChanged(bool v)      { if (_loading) return; _svc.SetHiddenVisible(v);      StatusText = v ? "Fichiers cachés visibles" : "Fichiers cachés masqués"; }
        partial void OnSuperHiddenVisibleChanged(bool v) { if (_loading) return; _svc.SetSuperHiddenVisible(v); StatusText = v ? "Fichiers système visibles" : "Fichiers système masqués"; }
        partial void OnFullPathInTitleChanged(bool v)    { if (_loading) return; _svc.SetFullPathInTitle(v);    StatusText = v ? "Chemin complet dans la barre de titre" : "Chemin complet désactivé"; }
        partial void OnIconsOnlyChanged(bool v)          { if (_loading) return; _svc.SetIconsOnly(v);          StatusText = v ? "Vignettes désactivées (icônes seules)" : "Vignettes activées"; }
        partial void OnEndTaskEnabledChanged(bool v)     { if (_loading) return; _svc.SetEndTask(v);            StatusText = v ? "\"Fin de tâche\" disponible dans la barre des tâches" : "\"Fin de tâche\" masqué"; }
        partial void OnNumLockOnBootChanged(bool v)       { if (_loading) return; _svc.SetNumLockOnBoot(v);        StatusText = v ? "NumLock activé au démarrage" : "NumLock désactivé au démarrage"; }
        partial void OnTaskbarCenteredChanged(bool v)     { if (_loading) return; _svc.SetTaskbarCentered(v);     StatusText = v ? "Barre des tâches centrée" : "Barre des tâches à gauche"; }
        partial void OnSearchBarModeChanged(int v)        { if (_loading) return; _svc.SetSearchBarMode(v);       StatusText = v == 0 ? "Recherche masquée" : v == 1 ? "Recherche : icône" : "Barre de recherche complète"; }
        partial void OnWidgetsEnabledChanged(bool v)      { if (_loading) return; _svc.SetWidgets(v);             StatusText = v ? "Widgets activés" : "Widgets masqués"; }
        partial void OnClassicContextMenuChanged(bool v)  { if (_loading) return; _svc.SetClassicContextMenu(v);  StatusText = v ? "Menu contextuel classique activé (redémarrage Explorateur requis)" : "Menu contextuel Windows 11 restauré"; }

        [RelayCommand]
        private void RestartExplorer()
        {
            _svc.RestartExplorer();
            StatusText = "Explorateur Windows redémarré - changements appliqués.";
        }
    }
}
