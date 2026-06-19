using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PCDoctor.Services;

namespace PCDoctor.ViewModels
{
    public partial class GhostServicesViewModel : ObservableObject
    {
        private readonly GhostServicesService _svc = new();

        [ObservableProperty] private ObservableCollection<GhostService> services = new();
        [ObservableProperty] private string statusText  = "Cliquez sur Analyser pour détecter les services fantômes.";
        [ObservableProperty] private bool   isScanning;
        [ObservableProperty] private bool   hasScanned;

        public event Func<string, Task<bool>>? ConfirmRequested;

        [RelayCommand]
        private async Task Scan()
        {
            IsScanning = true;
            HasScanned = false;
            StatusText = "Analyse des services en cours...";
            Services.Clear();

            var found = await Task.Run(() => _svc.Scan());
            foreach (var s in found) Services.Add(s);

            IsScanning = false;
            HasScanned = true;
            StatusText = Services.Count == 0
                ? "Aucun service fantôme détecté. Système propre."
                : $"{Services.Count} service(s) fantôme(s) détecté(s). Vérifiez avant de supprimer.";
        }

        [RelayCommand]
        private async Task Delete()
        {
            var selected = Services.Where(s => s.IsSelected).ToList();
            if (selected.Count == 0) { StatusText = "Aucun service sélectionné."; return; }

            string recap = string.Join("\n", selected.Select(s =>
                $"• {s.Name}  ({s.DisplayName})\n  Chemin : {s.ImagePath}"));
            string msg = $"Les services suivants seront SUPPRIMÉS définitivement :\n\n{recap}\n\n" +
                         $"Cette action est irréversible. Continuer ?";

            bool ok = true;
            if (ConfirmRequested != null) ok = await ConfirmRequested.Invoke(msg);
            if (!ok) { StatusText = "Suppression annulée."; return; }

            StatusText = "Suppression en cours...";
            var (success, err) = await Task.Run(() => _svc.Delete(selected));
            StatusText = $"Terminé : {success} supprimé(s)" + (err > 0 ? $", {err} échec(s)." : ".");

            // Rescan après suppression
            await Scan();
        }
    }
}
