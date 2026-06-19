using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PCDoctor.Services;

namespace PCDoctor.ViewModels
{
    public partial class NetworkViewModel : ObservableObject
    {
        private readonly NetworkService _svc = new();
        private bool _loading;

        [ObservableProperty] private bool ipv6Active;
        [ObservableProperty] private bool dohActive;
        [ObservableProperty] private string statusText = "";
        [ObservableProperty] private string selectedDnsPreset = "";

        public ObservableCollection<string> DnsPresets { get; } = new(
            NetworkService.DnsPresets.Select(x => x.Label));

        public NetworkViewModel() { Sync(); }

        private void Sync()
        {
            _loading = true;
            Ipv6Active       = _svc.IsIpv6Active();
            DohActive        = _svc.IsDohActive();
            SelectedDnsPreset = _svc.GetCurrentDnsPreset();
            _loading = false;
        }

        partial void OnIpv6ActiveChanged(bool v)       { if (_loading) return; _svc.SetIpv6(v); StatusText = v ? "IPv6 activé" : "IPv6 désactivé (déconseillé sauf besoin précis)"; }
        partial void OnDohActiveChanged(bool v)         { if (_loading) return; _svc.SetDoh(v); StatusText = v ? "DNS chiffré (DoH Cloudflare) activé" : "DoH désactivé"; }
        partial void OnSelectedDnsPresetChanged(string v) { if (_loading || string.IsNullOrEmpty(v)) return; StatusText = _svc.SetDnsPreset(v); }

        [RelayCommand]
        private void FlushDns() => StatusText = _svc.FlushDns();

        [RelayCommand]
        private void ResetWinsock() => StatusText = _svc.ResetWinsock();
    }
}