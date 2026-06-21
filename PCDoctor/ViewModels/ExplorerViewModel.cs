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

        partial void OnFileExtVisibleChanged(bool value)     { if (_loading) return; _svc.SetFileExtVisible(value);     StatusText = value ? "Extensions de fichiers visibles" : "Extensions masquées"; }
        partial void OnHiddenVisibleChanged(bool value)      { if (_loading) return; _svc.SetHiddenVisible(value);      StatusText = value ? "Fichiers cachés visibles" : "Fichiers cachés masqués"; }
        partial void OnSuperHiddenVisibleChanged(bool value) { if (_loading) return; _svc.SetSuperHiddenVisible(value); StatusText = value ? "Fichiers système visibles" : "Fichiers système masqués"; }
        partial void OnFullPathInTitleChanged(bool value)    { if (_loading) return; _svc.SetFullPathInTitle(value);    StatusText = value ? "Chemin complet dans la barre de titre" : "Chemin complet désactivé"; }
        partial void OnIconsOnlyChanged(bool value)          { if (_loading) return; _svc.SetIconsOnly(value);          StatusText = value ? "Vignettes désactivées (icônes seules)" : "Vignettes activées"; }
        partial void OnEndTaskEnabledChanged(bool value)     { if (_loading) return; _svc.SetEndTask(value);            StatusText = value ? "\"Fin de tâche\" disponible dans la barre des tâches" : "\"Fin de tâche\" masqué"; }
        partial void OnNumLockOnBootChanged(bool value)       { if (_loading) return; _svc.SetNumLockOnBoot(value);        StatusText = value ? "NumLock activé au démarrage" : "NumLock désactivé au démarrage"; }
        partial void OnTaskbarCenteredChanged(bool value)     { if (_loading) return; _svc.SetTaskbarCentered(value);     StatusText = value ? "Barre des tâches centrée" : "Barre des tâches à gauche"; }
        partial void OnSearchBarModeChanged(int value)        { if (_loading) return; _svc.SetSearchBarMode(value);       StatusText = value == 0 ? "Recherche masquée" : value == 1 ? "Recherche : icône" : "Barre de recherche complète"; }
        partial void OnWidgetsEnabledChanged(bool value)      { if (_loading) return; _svc.SetWidgets(value);             StatusText = value ? "Widgets activés" : "Widgets masqués"; }
        partial void OnClassicContextMenuChanged(bool value)  { if (_loading) return; _svc.SetClassicContextMenu(value);  StatusText = value ? "Menu contextuel classique activé (redémarrage Explorateur requis)" : "Menu contextuel Windows 11 restauré"; }

        [RelayCommand]
        private void RestartExplorer()
        {
            _svc.RestartExplorer();
            StatusText = "Explorateur Windows redémarré - changements appliqués.";
        }
    }
}
