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

        partial void OnIpv6ActiveChanged(bool value)       { if (_loading) return; _svc.SetIpv6(value); StatusText = value ? "IPv6 activ�" : "IPv6 d�sactiv� (d�conseill� sauf besoin pr�cis)"; }
        partial void OnDohActiveChanged(bool value)         { if (_loading) return; _svc.SetDoh(value); StatusText = value ? "DNS chiffr� (DoH Cloudflare) activ�" : "DoH d�sactiv�"; }
        partial void OnSelectedDnsPresetChanged(string value) { if (_loading || string.IsNullOrEmpty(value)) return; StatusText = _svc.SetDnsPreset(value); }

        [RelayCommand]
        private void FlushDns() => StatusText = _svc.FlushDns();

        [RelayCommand]
        private void ResetWinsock() => StatusText = _svc.ResetWinsock();
    }
}