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
        [ObservableProperty] private bool recallActive;
        [ObservableProperty] private bool locationActive;
        [ObservableProperty] private bool wifiSenseActive;
        [ObservableProperty] private bool backgroundAppsActive;
        [ObservableProperty] private bool copilotActive;
        [ObservableProperty] private bool aiSearchActive;
        [ObservableProperty] private bool settingsSuggestionsActive;
        [ObservableProperty] private string statusText = "";

        public PrivacyViewModel() { Sync(); }

        private void Sync()
        {
            _loading = true;
            TelemetryActive            = _svc.IsTelemetryActive();
            CortanaActive              = _svc.IsCortanaActive();
            ActivityActive             = _svc.IsActivityActive();
            AdsActive                  = _svc.IsAdsActive();
            AdIdActive                 = _svc.IsAdIdActive();
            OfficeActive               = _svc.IsOfficeActive();
            RecallActive               = _svc.IsRecallActive();
            LocationActive             = _svc.IsLocationActive();
            WifiSenseActive            = _svc.IsWifiSenseActive();
            BackgroundAppsActive       = _svc.IsBackgroundAppsActive();
            CopilotActive              = _svc.IsCopilotActive();
            AiSearchActive             = _svc.IsAiSearchActive();
            SettingsSuggestionsActive  = _svc.IsSettingsSuggestionsActive();
            _loading = false;
        }

        partial void OnTelemetryActiveChanged(bool value)           { if (_loading) return; _svc.SetTelemetry(value);            StatusText = value ? "T�l�m�trie r�activ�e" : "T�l�m�trie d�sactiv�e"; }
        partial void OnCortanaActiveChanged(bool value)             { if (_loading) return; _svc.SetCortana(value);              StatusText = value ? "Cortana r�activ�" : "Cortana d�sactiv�"; }
        partial void OnActivityActiveChanged(bool value)            { if (_loading) return; _svc.SetActivity(value);             StatusText = value ? "Activity History r�activ�" : "Activity History d�sactiv�"; }
        partial void OnAdsActiveChanged(bool value)                 { if (_loading) return; _svc.SetAds(value);                  StatusText = value ? "Pubs r�activ�es" : "Pubs d�sactiv�es"; }
        partial void OnAdIdActiveChanged(bool value)                { if (_loading) return; _svc.SetAdId(value);                 StatusText = value ? "Advertising ID r�activ�" : "Advertising ID d�sactiv�"; }
        partial void OnOfficeActiveChanged(bool value)              { if (_loading) return; _svc.SetOffice(value);               StatusText = value ? "T�l�m�trie Office r�activ�e" : "T�l�m�trie Office d�sactiv�e"; }
        partial void OnWifiSenseActiveChanged(bool value)          { if (_loading) return; _svc.SetWifiSense(value);          StatusText = value ? "Wi-Fi Sense activ�" : "Wi-Fi Sense d�sactiv� (partage de r�seaux bloqu�)"; }
        partial void OnBackgroundAppsActiveChanged(bool value)     { if (_loading) return; _svc.SetBackgroundApps(value);     StatusText = value ? "Applications en arri�re-plan autoris�es" : "Applications en arri�re-plan bloqu�es"; }
        partial void OnRecallActiveChanged(bool value)              { if (_loading) return; _svc.SetRecall(value);               StatusText = value ? "Windows Recall r�activ�" : "Windows Recall d�sactiv� (screenshots IA stopp�s)"; }
        partial void OnLocationActiveChanged(bool value)            { if (_loading) return; _svc.SetLocation(value);             StatusText = value ? "Localisation activ�e" : "Localisation d�sactiv�e"; }
        partial void OnCopilotActiveChanged(bool value)             { if (_loading) return; _svc.SetCopilot(value);              StatusText = value ? "Copilot r�activ�" : "Copilot d�sactiv�"; }
        partial void OnAiSearchActiveChanged(bool value)            { if (_loading) return; _svc.SetAiSearch(value);             StatusText = value ? "Suggestions IA activ�es" : "Suggestions IA dans la recherche d�sactiv�es"; }
        partial void OnSettingsSuggestionsActiveChanged(bool value) { if (_loading) return; _svc.SetSettingsSuggestions(value);  StatusText = value ? "Suggestions Param�tres activ�es" : "Suggestions dans les Param�tres d�sactiv�es"; }
    }
}