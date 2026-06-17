using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PCDoctor.Services;

namespace PCDoctor.ViewModels
{
    public partial class BloatwareViewModel : ObservableObject
    {
        private readonly BloatwareService _svc = new();

        [ObservableProperty] private ObservableCollection<BloatwareApp> apps = new();
        [ObservableProperty] private string statusText = "Cliquez sur Analyser pour détecter les bloatwares installés.";
        [ObservableProperty] private bool hasScanned;

        public event System.Func<string, Task<bool>>? ConfirmRequested;

        [RelayCommand]
        private void Scan()
        {
            Apps.Clear();
            foreach (var a in _svc.Scan()) Apps.Add(a);
            StatusText = Apps.Count == 0 ? "Aucun bloatware détecté. 🎉" : $"{Apps.Count} bloatware(s) détecté(s). Cochez ce que vous voulez désinstaller.";
            HasScanned = true;
        }

        [RelayCommand]
        private async Task Remove()
        {
            var selected = Apps.Where(a => a.IsSelected).ToList();
            if (selected.Count == 0) { StatusText = "Aucune app sélectionnée."; return; }

            string recap = string.Join("\n", selected.Select(a => $"• {a.Display}"));
            string msg = $"Les applications suivantes seront DÉSINSTALLÉES :\n\n{recap}\n\n{selected.Count} app(s). Continuer ?";

            bool ok = true;
            if (ConfirmRequested != null) ok = await ConfirmRequested.Invoke(msg);
            if (!ok) { StatusText = "Désinstallation annulée."; return; }

            var (success, err) = _svc.Remove(selected);
            StatusText = $"Terminé : {success} désinstallée(s)" + (err > 0 ? $", {err} échec(s)." : ".");
            Scan();
        }
    }
}