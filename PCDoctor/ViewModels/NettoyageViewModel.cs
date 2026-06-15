using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PCDoctor.Services;

namespace PCDoctor.ViewModels
{
    public partial class NettoyageViewModel : ObservableObject
    {
        private readonly CleanupService _svc = new();

        [ObservableProperty] private ObservableCollection<CleanupCategory> categories = new();
        [ObservableProperty] private string statusText = "Cliquez sur Analyser pour commencer.";
        [ObservableProperty] private bool hasScanned;

        [RelayCommand]
        private void Scan()
        {
            Categories.Clear();
            foreach (var c in _svc.Scan()) Categories.Add(c);
            long total = Categories.Sum(c => c.SizeBytes);
            StatusText = $"Analyse terminée : {CleanupCategory.FormatSize(total)} récupérables.";
            HasScanned = true;
        }

        // Événement déclenché pour demander confirmation à la View (qui affiche le dialog)
        public event System.Func<string, System.Threading.Tasks.Task<bool>>? ConfirmRequested;

        [RelayCommand]
        private async System.Threading.Tasks.Task Clean()
        {
            var selected = Categories.Where(c => c.IsSelected).ToList();
            if (selected.Count == 0) { StatusText = "Aucune catégorie sélectionnée."; return; }

            // Récap pour la confirmation
            long totalSel = selected.Sum(c => c.SizeBytes);
            string recap = string.Join("\n", selected.Select(c => $"• {c.Name} ({c.SizeText})"));
            string msg = $"Les éléments suivants seront nettoyés :\n\n{recap}\n\nTotal : {CleanupCategory.FormatSize(totalSel)}\n\nContinuer ?";

            // Demande confirmation à la View
            bool ok = true;
            if (ConfirmRequested != null)
                ok = await ConfirmRequested.Invoke(msg);
            if (!ok) { StatusText = "Nettoyage annulé."; return; }

            var (files, bytes) = _svc.Clean(selected);
            StatusText = $"Nettoyage terminé : {files} fichiers supprimés, {CleanupCategory.FormatSize(bytes)} libérés.";
            Scan();
        }
    }
}