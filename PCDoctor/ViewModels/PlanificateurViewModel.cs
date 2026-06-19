using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PCDoctor.Services;

namespace PCDoctor.ViewModels
{
    public partial class PlanificateurViewModel : ObservableObject
    {
        private readonly PlanificateurService _svc = new();

        [ObservableProperty] private ObservableCollection<ScheduledTaskItem> tasks = new();
        [ObservableProperty] private string statusText = "Cliquez sur Analyser pour lister les tâches tierces.";
        [ObservableProperty] private bool   hasScanned;
        [ObservableProperty] private bool   isLoading;

        public event System.Func<string, Task<bool>>? ConfirmRequested;

        [RelayCommand]
        private async Task Scan()
        {
            IsLoading  = true;
            StatusText = "Analyse en cours...";
            Tasks.Clear();

            var found = await Task.Run(() => _svc.GetThirdPartyTasks());
            foreach (var t in found) Tasks.Add(t);

            HasScanned = true;
            IsLoading  = false;
            StatusText = Tasks.Count == 0
                ? "Aucune tâche planifiée tierce trouvée."
                : $"{Tasks.Count} tâche(s) tierce(s) trouvée(s). Sélectionnez, puis choisissez une action.";
        }

        [RelayCommand]
        private void Enable()
        {
            var selected = Tasks.Where(t => t.IsSelected).ToList();
            if (selected.Count == 0) { StatusText = "Aucune tâche sélectionnée."; return; }
            foreach (var t in selected) _svc.EnableTask(t.Name, t.Path);
            StatusText = $"{selected.Count} tâche(s) activée(s).";
            ScanCommand.Execute(null);
        }

        [RelayCommand]
        private void Disable()
        {
            var selected = Tasks.Where(t => t.IsSelected).ToList();
            if (selected.Count == 0) { StatusText = "Aucune tâche sélectionnée."; return; }
            foreach (var t in selected) _svc.DisableTask(t.Name, t.Path);
            StatusText = $"{selected.Count} tâche(s) désactivée(s).";
            ScanCommand.Execute(null);
        }

        [RelayCommand]
        private async Task Delete()
        {
            var selected = Tasks.Where(t => t.IsSelected).ToList();
            if (selected.Count == 0) { StatusText = "Aucune tâche sélectionnée."; return; }

            string recap = string.Join("\n", selected.Select(t => $"- {t.Path}{t.Name}"));
            string msg   = $"Les tâches suivantes seront SUPPRIMÉES définitivement :\n\n{recap}\n\nContinuer ?";

            bool ok = true;
            if (ConfirmRequested != null) ok = await ConfirmRequested.Invoke(msg);
            if (!ok) { StatusText = "Suppression annulée."; return; }

            foreach (var t in selected) _svc.DeleteTask(t.Name, t.Path);
            StatusText = $"{selected.Count} tâche(s) supprimée(s).";
            ScanCommand.Execute(null);
        }
    }
}
