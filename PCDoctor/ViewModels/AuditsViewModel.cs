using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PCDoctor.Services;

namespace PCDoctor.ViewModels
{
    public partial class AuditsViewModel : ObservableObject
    {
        private readonly AuditService _audit = new();

        [ObservableProperty] private string currentTitle = "Sélectionnez un audit";
        [ObservableProperty] private string currentSubtitle = "";
        [ObservableProperty] private string h1 = "";
        [ObservableProperty] private string h2 = "";
        [ObservableProperty] private string h3 = "";
        [ObservableProperty] private string h4 = "";
        [ObservableProperty] private ObservableCollection<AuditRow> rows = new();

        // [RelayCommand] génère automatiquement une commande "RunServicesCommand" etc.
        // que le bouton XAML appelle.
        [RelayCommand] private void RunServices() => Apply(_audit.AuditServices());
        [RelayCommand] private void RunDrivers() => Apply(_audit.AuditDrivers());
        [RelayCommand] private void RunDefender() => Apply(_audit.AuditDefender());
        [RelayCommand] private void RunSystemInfo() => Apply(_audit.AuditSystemInfo());

        private void Apply(AuditResult res)
        {
            CurrentTitle = res.Title;
            CurrentSubtitle = res.Subtitle;
            H1 = res.H1; H2 = res.H2; H3 = res.H3; H4 = res.H4;
            Rows.Clear();
            foreach (var row in res.Rows) Rows.Add(row);
        }
    }
}