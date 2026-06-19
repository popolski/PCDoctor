using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PCDoctor.Services;

namespace PCDoctor.ViewModels
{
    public partial class ProfilesViewModel : ObservableObject
    {
        private readonly ProfilesService _svc = new();
        private bool _loading;

        public ObservableCollection<string> PowerPlanLabels { get; } = new(
            ProfilesService.PowerPlans.Select(x => x.Label));

        [ObservableProperty] private string selectedPlan = "";
        [ObservableProperty] private bool isApplying;
        [ObservableProperty] private string logText = "";
        [ObservableProperty] private string statusText = "";

        public bool CanApply => !IsApplying;
        partial void OnIsApplyingChanged(bool v) => OnPropertyChanged(nameof(CanApply));

        public ProfilesViewModel()
        {
            _loading = true;
            SelectedPlan = _svc.GetActivePlanLabel();
            // Masquer Ultimate si non disponible
            if (!_svc.IsUltimatePerfAvailable())
                PowerPlanLabels.Remove("Performances optimales");
            _loading = false;
        }

        partial void OnSelectedPlanChanged(string v)
        {
            if (_loading || string.IsNullOrEmpty(v) || v == "Personnalisé" || v == "Inconnu") return;
            _svc.SetPowerPlan(v);
            StatusText = $"Plan appliqué : {v}";
        }

        [RelayCommand]
        private async Task ApplyGaming()
        {
            IsApplying = true;
            StatusText = "Application du profil Gaming...";
            var log = await Task.Run(() => _svc.ApplyGamingProfile());
            LogText = string.Join("\n", log);
            StatusText = "Profil Gaming appliqué.";
            RefreshPlan();
            IsApplying = false;
        }

        [RelayCommand]
        private async Task ApplyPrivacy()
        {
            IsApplying = true;
            StatusText = "Application du profil Vie privée...";
            var log = await Task.Run(() => _svc.ApplyPrivacyProfile());
            LogText = string.Join("\n", log);
            StatusText = "Profil Vie privée appliqué.";
            IsApplying = false;
        }

        [RelayCommand]
        private async Task ApplyProductivity()
        {
            IsApplying = true;
            StatusText = "Application du profil Productivité...";
            var log = await Task.Run(() => _svc.ApplyProductivityProfile());
            LogText = string.Join("\n", log);
            StatusText = "Profil Productivité appliqué.";
            RefreshPlan();
            IsApplying = false;
        }

        [RelayCommand]
        private async Task ApplyBalanced()
        {
            IsApplying = true;
            StatusText = "Application du profil Équilibré...";
            var log = await Task.Run(() => _svc.ApplyBalancedProfile());
            LogText = string.Join("\n", log);
            StatusText = "Profil Équilibré appliqué.";
            RefreshPlan();
            IsApplying = false;
        }

        private void RefreshPlan()
        {
            _loading = true;
            SelectedPlan = _svc.GetActivePlanLabel();
            _loading = false;
        }
    }
}
