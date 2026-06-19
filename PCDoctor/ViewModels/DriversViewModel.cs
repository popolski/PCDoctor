using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PCDoctor.Services;

namespace PCDoctor.ViewModels
{
    public partial class DriversViewModel : ObservableObject
    {
        private readonly DriversService _svc = new();
        private List<DriverItem> _allDrivers = new();

        [ObservableProperty] private ObservableCollection<DriverItem> drivers = new();
        [ObservableProperty] private string searchText = "";
        [ObservableProperty] private string statusText = "Cliquez sur Analyser pour lister les pilotes installés.";
        [ObservableProperty] private bool   isLoading;

        partial void OnSearchTextChanged(string v) => ApplyFilter(v);

        [RelayCommand]
        private async Task Scan()
        {
            IsLoading  = true;
            StatusText = "Analyse en cours...";
            Drivers.Clear();
            _allDrivers.Clear();

            _allDrivers = await Task.Run(() => _svc.GetDrivers());
            ApplyFilter(SearchText);

            int old = _allDrivers.Count(d => d.IsOld);
            StatusText = $"{_allDrivers.Count} pilotes trouvés" +
                         (old > 0 ? $", dont {old} datant de plus de 3 ans." : ".");
            IsLoading = false;
        }

        private void ApplyFilter(string filter)
        {
            Drivers.Clear();
            var src = string.IsNullOrWhiteSpace(filter)
                ? _allDrivers
                : _allDrivers.Where(d =>
                    d.DeviceName.Contains(filter, System.StringComparison.OrdinalIgnoreCase) ||
                    d.Provider.Contains(filter, System.StringComparison.OrdinalIgnoreCase) ||
                    d.Version.Contains(filter, System.StringComparison.OrdinalIgnoreCase));
            foreach (var d in src) Drivers.Add(d);
        }

        [RelayCommand] private void OpenDeviceManager()  => _svc.OpenDeviceManager();
        [RelayCommand] private void OpenOptionalUpdates() => _svc.OpenOptionalUpdates();
    }
}
