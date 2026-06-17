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
        private readonly AppsService _svc = new();
        private List<InstalledApp> _all = new();

        [ObservableProperty] private ObservableCollection<InstalledApp> apps = new();
        [ObservableProperty] private string statusText = "";
        [ObservableProperty] private string searchText = "";

        public event System.Func<string, Task<bool>>? ConfirmRequested;

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

            if (_svc.Uninstall(app)) StatusText = $"Désinstallation de {app.Name} lancée.";
            else StatusText = $"Impossible de désinstaller {app.Name} (pas de commande).";
        }
    }
}