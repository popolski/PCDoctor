using CommunityToolkit.Mvvm.ComponentModel;
using PCDoctor.Services;

namespace PCDoctor.ViewModels
{
    public partial class PrivacyViewModel : ObservableObject
    {
        private readonly PrivacyService _svc = new();
        private bool _loading;

        [ObservableProperty] private bool telemetryActive;
        [ObservableProperty] private bool cortanaActive;
        [ObservableProperty] private bool activityActive;
        [ObservableProperty] private bool adsActive;
        [ObservableProperty] private bool adIdActive;
        [ObservableProperty] private bool officeActive;
        [ObservableProperty] private string statusText = "";

        public PrivacyViewModel() { Sync(); }

        private void Sync()
        {
            _loading = true;
            TelemetryActive = _svc.IsTelemetryActive();
            CortanaActive = _svc.IsCortanaActive();
            ActivityActive = _svc.IsActivityActive();
            AdsActive = _svc.IsAdsActive();
            AdIdActive = _svc.IsAdIdActive();
            OfficeActive = _svc.IsOfficeActive();
            _loading = false;
        }

        partial void OnTelemetryActiveChanged(bool v) { if (_loading) return; _svc.SetTelemetry(v); StatusText = v ? "Télémétrie réactivée" : "Télémétrie désactivée"; }
        partial void OnCortanaActiveChanged(bool v) { if (_loading) return; _svc.SetCortana(v); StatusText = v ? "Cortana réactivé" : "Cortana désactivé"; }
        partial void OnActivityActiveChanged(bool v) { if (_loading) return; _svc.SetActivity(v); StatusText = v ? "Activity History réactivé" : "Activity History désactivé"; }
        partial void OnAdsActiveChanged(bool v) { if (_loading) return; _svc.SetAds(v); StatusText = v ? "Pubs réactivées" : "Pubs désactivées"; }
        partial void OnAdIdActiveChanged(bool v) { if (_loading) return; _svc.SetAdId(v); StatusText = v ? "Advertising ID réactivé" : "Advertising ID désactivé"; }
        partial void OnOfficeActiveChanged(bool v) { if (_loading) return; _svc.SetOffice(v); StatusText = v ? "Télémétrie Office réactivée" : "Télémétrie Office désactivée"; }
    }
}