using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using PCDoctor.Services;

namespace PCDoctor.ViewModels
{
    public partial class WingetViewModel : ObservableObject
    {
        private readonly WingetService    _svc        = new();
        private readonly DispatcherQueue  _dispatcher = DispatcherQueue.GetForCurrentThread();
        private readonly StringBuilder   _logBuilder = new();

        [ObservableProperty] private ObservableCollection<WingetPackage> updates = new();
        [ObservableProperty] private string statusText = "Cliquez sur Analyser pour rechercher les mises à jour disponibles.";
        [ObservableProperty] private bool   isScanning;
        [ObservableProperty] private bool   isUpdating;
        [ObservableProperty] private bool   hasScanned;
        [ObservableProperty] private bool   canScan    = true;
        [ObservableProperty] private string logText    = "";

        public WingetViewModel()
        {
            if (!_svc.IsAvailable())
            {
                CanScan    = false;
                StatusText = "Winget (Gestionnaire de package Windows) n'est pas disponible sur ce systeme.";
            }
        }

        [RelayCommand]
        private async Task Scan()
        {
            IsScanning = true;
            CanScan    = false;
            HasScanned = false;
            StatusText = "Recherche des mises à jour en cours... (peut prendre 30-60s)";
            Updates.Clear();
            ClearLog();

            var found = await Task.Run(() => _svc.GetUpdates());
            foreach (var p in found) Updates.Add(p);

            IsScanning = false;
            CanScan    = true;
            HasScanned = true;
            StatusText = Updates.Count == 0
                ? "Tout est à jour."
                : $"{Updates.Count} mise(s) à jour disponible(s). Cochez ce que vous voulez installer.";
        }

        [RelayCommand]
        private async Task UpdateSelected()
        {
            var selected = Updates.Where(u => u.IsSelected).ToList();
            if (selected.Count == 0) { StatusText = "Aucun paquet sélectionné."; return; }

            IsUpdating = true;
            CanScan    = false;
            HasScanned = false;
            ClearLog();
            AppendLog($"Mise à jour de {selected.Count} paquet(s)...\n");

            int ok = 0, err = 0;
            foreach (var pkg in selected)
            {
                AppendLog($"\n--- {pkg.Name} ({pkg.Id}) ---");
                int exit = await Task.Run(() => _svc.UpdatePackage(pkg.Id, line => AppendLog(line)));
                if (exit == 0) ok++;
                else           err++;
            }

            IsUpdating = false;
            StatusText = $"Terminé : {ok} mis à jour" + (err > 0 ? $", {err} échec(s)." : ".");
            AppendLog($"\n=== Terminé : {ok} OK, {err} erreur(s) ===");

            // Rescan
            await Scan();
        }

        [RelayCommand]
        private async Task UpdateAll()
        {
            IsUpdating = true;
            CanScan    = false;
            HasScanned = false;
            ClearLog();
            AppendLog("Mise à jour de tous les paquets...\n");

            int exit = await Task.Run(() => _svc.UpdateAll(line => AppendLog(line)));

            IsUpdating = false;
            StatusText = exit == 0
                ? "Toutes les mises à jour installées."
                : $"Terminé avec code {exit}. Vérifiez le journal.";
            AppendLog($"\n=== Terminé (code {exit}) ===");

            await Scan();
        }

        // ─── Helpers ───

        private void AppendLog(string line)
        {
            _dispatcher.TryEnqueue(() =>
            {
                _logBuilder.AppendLine(line);
                LogText = _logBuilder.ToString();
            });
        }

        private void ClearLog()
        {
            _logBuilder.Clear();
            LogText = "";
        }
    }
}
