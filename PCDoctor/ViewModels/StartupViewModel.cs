using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PCDoctor.Services;

namespace PCDoctor.ViewModels
{
    public partial class StartupViewModel : ObservableObject
    {
        private readonly StartupService _svc = new();

        [ObservableProperty] private ObservableCollection<StartupEntry> entries = new();
        [ObservableProperty] private string statusText = "";

        public StartupViewModel() { Load(); }

        [RelayCommand]
        private void Load()
        {
            Entries.Clear();
            foreach (var e in _svc.GetEntries()) Entries.Add(e);
            StatusText = $"{Entries.Count} entrées au démarrage.";
        }

        [RelayCommand]
        private void Toggle(StartupEntry entry)
        {
            if (entry == null) return;
            bool ok;
            if (entry.Status == "Activé") ok = _svc.Disable(entry);
            else ok = _svc.Enable(entry);
            StatusText = ok ? $"{entry.Name} : modifié." : $"Échec sur {entry.Name}.";
            Load(); // rafraîchir
        }
    }
}