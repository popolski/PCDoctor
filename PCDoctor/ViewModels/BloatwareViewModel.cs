using System;
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
        [ObservableProperty] private string extraStatusText = "";

        public event Func<string, Task<bool>>? ConfirmRequested;
        public event Func<string, Task<bool>>? OneDriveConfirmRequested;
        public event Func<string, Task<bool>>? EdgeConfirmRequested;

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

        [RelayCommand]
        private async Task RemoveOneDrive()
        {
            bool ok = true;
            if (OneDriveConfirmRequested != null)
                ok = await OneDriveConfirmRequested.Invoke(
                    "Désinstaller OneDrive complètement ?\n\n" +
                    "Cette action :\n" +
                    "• Arrête le processus OneDrive\n" +
                    "• Lance le désinstalleur officiel\n" +
                    "• Supprime les dossiers résiduels\n" +
                    "• Retire l'icône de l'Explorateur\n\n" +
                    "Sauvegardez d'abord vos fichiers OneDrive locaux si nécessaire.");
            if (!ok) { ExtraStatusText = "Suppression OneDrive annulée."; return; }

            ExtraStatusText = "Suppression OneDrive en cours...";
            var result = await Task.Run(() => _svc.RemoveOneDrive());
            ExtraStatusText = "OneDrive supprimé. " + result.Replace("\n", " ");
        }

        [RelayCommand]
        private async Task DisableEdgeShortcuts()
        {
            bool ok = true;
            if (EdgeConfirmRequested != null)
                ok = await EdgeConfirmRequested.Invoke(
                    "Empêcher Edge de recréer ses raccourcis bureau et barre des tâches à chaque mise à jour ?\n\n" +
                    "Applique une stratégie locale (HKLM\\SOFTWARE\\Policies\\Microsoft\\EdgeUpdate).");
            if (!ok) { ExtraStatusText = "Action annulée."; return; }

            try
            {
                _svc.DisableEdgeShortcuts();
                ExtraStatusText = _svc.IsEdgeShortcutsDisabled()
                    ? "Raccourcis Edge bloqués avec succès."
                    : "Paramètre appliqué.";
            }
            catch (Exception ex)
            {
                ExtraStatusText = $"Erreur : {ex.Message}";
            }
        }
    }
}