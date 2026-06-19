using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PCDoctor.Services;

namespace PCDoctor.ViewModels
{
    public partial class AppsViewModel : ObservableObject
    {
        private readonly AppsService    _svc     = new();
        private readonly ResidusService _residus = new();
        private List<InstalledApp> _all = new();

        // ─── Apps ───
        [ObservableProperty] private ObservableCollection<InstalledApp> apps = new();
        [ObservableProperty] private string statusText  = "";
        [ObservableProperty] private string searchText  = "";

        // ─── Résidus ───
        [ObservableProperty] private ObservableCollection<ResiduItem> residusItems = new();
        [ObservableProperty] private string residusSearchName  = "";
        [ObservableProperty] private string residusStatusText  = "Entrez un mot-clé (nom d'app ou éditeur) puis cliquez sur Scanner.";
        [ObservableProperty] private bool   residusIsLoading;
        [ObservableProperty] private bool   residusHasResults;

        // Deux événements de confirmation séparés pour les deux dialogs
        public event System.Func<string, Task<bool>>? ConfirmRequested;
        public event System.Func<string, Task<bool>>? ResidusConfirmRequested;

        public AppsViewModel() { Load(); }

        [RelayCommand]
        private void Load()
        {
            _all = _svc.GetApps();
            Filter();
            StatusText = $"{_all.Count} programmes installés.";
        }

        partial void OnSearchTextChanged(string value) => Filter();

        private void Filter()
        {
            Apps.Clear();
            var q = string.IsNullOrWhiteSpace(SearchText)
                ? _all
                : _all.Where(a => a.Name.Contains(SearchText, System.StringComparison.OrdinalIgnoreCase)).ToList();
            foreach (var a in q) Apps.Add(a);
        }

        [RelayCommand]
        private async Task Uninstall(InstalledApp app)
        {
            if (app == null) return;
            bool ok = true;
            if (ConfirmRequested != null)
                ok = await ConfirmRequested.Invoke($"Désinstaller \"{app.Name}\" ?\n\nLe programme d'installation va s'ouvrir.");
            if (!ok) return;

            if (_svc.Uninstall(app))
            {
                StatusText = $"Désinstallation de {app.Name} lancée.";
                // Pré-remplir le champ résidus avec le nom de l'app désinstallée
                ResidusSearchName  = app.Name;
                ResidusStatusText  = $"Désinstallation lancée. Une fois terminée, cliquez sur Scanner pour chercher les résidus de \"{app.Name}\".";
                ResidusHasResults  = false;
                ResidusItems.Clear();
            }
            else
            {
                StatusText = $"Impossible de désinstaller {app.Name} (pas de commande).";
            }
        }

        // ─── Résidus ───

        [RelayCommand]
        private async Task ScanResidus()
        {
            if (string.IsNullOrWhiteSpace(ResidusSearchName)) return;
            ResidusIsLoading  = true;
            ResidusHasResults = false;
            ResidusStatusText = $"Scan en cours pour \"{ResidusSearchName}\"...";
            ResidusItems.Clear();

            var found = await Task.Run(() => _residus.Scan(ResidusSearchName));
            foreach (var r in found) ResidusItems.Add(r);

            ResidusIsLoading  = false;
            ResidusHasResults = ResidusItems.Count > 0;
            ResidusStatusText = ResidusItems.Count == 0
                ? $"Aucun résidu trouvé pour \"{ResidusSearchName}\"."
                : $"{ResidusItems.Count} résidu(s) trouvé(s). Sélectionnez ce que vous voulez supprimer.";
        }

        [RelayCommand]
        private async Task CleanResidus()
        {
            var selected = ResidusItems.Where(r => r.IsSelected).ToList();
            if (selected.Count == 0) { ResidusStatusText = "Aucun élément sélectionné."; return; }

            string recap = string.Join("\n", selected.Select(r => $"• [{r.TypeLabel}] {r.Path}"));
            string msg   = $"Les éléments suivants seront SUPPRIMÉS définitivement :\n\n{recap}\n\nCette action est irréversible. Continuer ?";

            bool ok = true;
            if (ResidusConfirmRequested != null) ok = await ResidusConfirmRequested.Invoke(msg);
            if (!ok) { ResidusStatusText = "Suppression annulée."; return; }

            var (success, err) = _residus.Delete(selected);
            ResidusStatusText = $"Terminé : {success} supprimé(s)" + (err > 0 ? $", {err} échec(s)." : ".");

            // Rescan après suppression
            await ScanResidus();
        }
    }
}
