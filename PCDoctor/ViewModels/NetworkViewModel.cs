using CommunityToolkit.Mvvm.ComponentModel;
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

        public NetworkViewModel() { Sync(); }

        private void Sync()
        {
            _loading = true;
            Ipv6Active = _svc.IsIpv6Active();
            DohActive = _svc.IsDohActive();
            _loading = false;
        }

        partial void OnIpv6ActiveChanged(bool v) { if (_loading) return; _svc.SetIpv6(v); StatusText = v ? "IPv6 activé" : "IPv6 désactivé (déconseillé sauf besoin précis)"; }
        partial void OnDohActiveChanged(bool v) { if (_loading) return; _svc.SetDoh(v); StatusText = v ? "DNS chiffré (DoH Cloudflare) activé" : "DoH désactivé"; }
    }
}