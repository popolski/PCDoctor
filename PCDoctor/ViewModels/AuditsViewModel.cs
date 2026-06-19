using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PCDoctor.Services;

namespace PCDoctor.ViewModels
{
    public partial class AuditsViewModel : ObservableObject
    {
        private readonly AuditService _audit = new();

        [ObservableProperty] private string currentTitle    = "Sélectionnez un audit";
        [ObservableProperty] private string currentSubtitle = "";
        [ObservableProperty] private string h1 = "";
        [ObservableProperty] private string h2 = "";
        [ObservableProperty] private string h3 = "";
        [ObservableProperty] private string h4 = "";
        [ObservableProperty] private ObservableCollection<AuditRow> rows = new();
        [ObservableProperty] private bool isBusy;
        public bool CanRun => !IsBusy;
        partial void OnIsBusyChanged(bool v) => OnPropertyChanged(nameof(CanRun));

        // Audits rapides (registre / PS court) - sync ok
        [RelayCommand(CanExecute = nameof(CanRun))] private void RunDefender()   => Apply(_audit.AuditDefender());
        [RelayCommand(CanExecute = nameof(CanRun))] private void RunSystemInfo() => Apply(_audit.AuditSystemInfo());

        // Audits lents (scan fichiers / PS lourd) - async
        [RelayCommand(CanExecute = nameof(CanRun))] private Task RunServices()         => RunAsync(_audit.AuditServices);
        [RelayCommand(CanExecute = nameof(CanRun))] private Task RunDrivers()          => RunAsync(_audit.AuditDrivers);
        [RelayCommand(CanExecute = nameof(CanRun))] private Task RunShellExtensions()  => RunAsync(_audit.AuditShellExtensions);
        [RelayCommand(CanExecute = nameof(CanRun))] private Task RunLargeFiles()       => RunAsync(_audit.AuditLargeFiles);
        [RelayCommand(CanExecute = nameof(CanRun))] private Task RunBrowserExtensions()=> RunAsync(_audit.AuditBrowserExtensions);

        private async Task RunAsync(System.Func<AuditResult> work)
        {
            IsBusy = true;
            CurrentSubtitle = "Analyse en cours...";
            var res = await Task.Run(work);
            Apply(res);
            IsBusy = false;
        }

        private void Apply(AuditResult res)
        {
            CurrentTitle    = res.Title;
            CurrentSubtitle = res.Subtitle;
            H1 = res.H1; H2 = res.H2; H3 = res.H3; H4 = res.H4;
            Rows.Clear();
            foreach (var row in res.Rows) Rows.Add(row);
        }
    }
}
