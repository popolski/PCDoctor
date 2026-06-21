using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
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

        private static readonly string ProfileFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PCDoctor", "activeprofile.txt");

        private string _activeProfile = "";
        private string ActiveProfile
        {
            get => _activeProfile;
            set
            {
                _activeProfile = value;
                OnPropertyChanged(nameof(GamingButtonStyle));
                OnPropertyChanged(nameof(PrivacyButtonStyle));
                OnPropertyChanged(nameof(ProductivityButtonStyle));
                OnPropertyChanged(nameof(BalancedButtonStyle));
                SaveProfile(value);
            }
        }

        // Retourne AccentButtonStyle si actif, null (= style par défaut) sinon
        private static Style? Accent => Application.Current.Resources.TryGetValue(
            "AccentButtonStyle", out var s) ? s as Style : null;

        public Style? GamingButtonStyle       => _activeProfile == "Gaming"       ? Accent : null;
        public Style? PrivacyButtonStyle      => _activeProfile == "Privacy"      ? Accent : null;
        public Style? ProductivityButtonStyle => _activeProfile == "Productivity" ? Accent : null;
        public Style? BalancedButtonStyle     => _activeProfile == "Balanced"     ? Accent : null;

        private static void SaveProfile(string name)
        {
            try { Directory.CreateDirectory(Path.GetDirectoryName(ProfileFile)!); File.WriteAllText(ProfileFile, name); }
            catch { }
        }
        private static string LoadProfile()
        {
            try { return File.Exists(ProfileFile) ? File.ReadAllText(ProfileFile).Trim() : ""; }
            catch { return ""; }
        }

        public bool CanApply => !IsApplying;
        partial void OnIsApplyingChanged(bool value) => OnPropertyChanged(nameof(CanApply));

        public ProfilesViewModel()
        {
            _loading = true;
            SelectedPlan = _svc.GetActivePlanLabel();
            if (!_svc.IsUltimatePerfAvailable())
                PowerPlanLabels.Remove("Performances optimales");
            _activeProfile = LoadProfile();
            _loading = false;
        }

        partial void OnSelectedPlanChanged(string value)
        {
            if (_loading || string.IsNullOrEmpty(value) || value == "Personnalisé" || value == "Inconnu") return;
            _svc.SetPowerPlan(value);
            StatusText = $"Plan appliqué : {value}";
        }

        [RelayCommand]
        private async Task ApplyGaming()
        {
            IsApplying = true;
            StatusText = "Application du profil Gaming...";
            var log = await Task.Run(() => _svc.ApplyGamingProfile());
            LogText = string.Join("\n", log);
            StatusText = "Profil Gaming appliqué.";
            ActiveProfile = "Gaming";
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
            ActiveProfile = "Privacy";
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
            ActiveProfile = "Productivity";
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
            ActiveProfile = "Balanced";
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
