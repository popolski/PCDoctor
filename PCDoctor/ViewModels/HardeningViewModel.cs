using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PCDoctor.Services;

namespace PCDoctor.ViewModels
{
    public partial class HardeningViewModel : ObservableObject
    {
        private readonly HardeningService _svc = new();
        private bool _loading;

        public bool MpCmdRunAvailable { get; } = File.Exists(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                         "Windows Defender", "MpCmdRun.exe"));
        public bool MpCmdRunUnavailable => !MpCmdRunAvailable; // évite de déclencher les actions pendant la synchro initiale

        [ObservableProperty] private bool llmnrActive;
        [ObservableProperty] private bool smb1Active;
        [ObservableProperty] private bool puaActive;
        [ObservableProperty] private string statusText = "";
        [ObservableProperty] private string signatureInfo = "Chargement...";
        [ObservableProperty] private bool mdnsActive;
        [ObservableProperty] private bool bonjourInstalled;
        [ObservableProperty] private bool isDefenderBusy;
        public bool CanUseDefender => !IsDefenderBusy;
        partial void OnIsDefenderBusyChanged(bool v) => OnPropertyChanged(nameof(CanUseDefender));

        // BitLocker
        [ObservableProperty] private ObservableCollection<BitLockerDrive> bitLockerDrives = new();
        [ObservableProperty] private string bitLockerStatusText = "Chargement...";

        // ASR
        [ObservableProperty] private ObservableCollection<AsrRule> asrRules = new();
        [ObservableProperty] private bool isAsrBusy;
        public bool CanUseAsr => !IsAsrBusy;
        partial void OnIsAsrBusyChanged(bool v) => OnPropertyChanged(nameof(CanUseAsr));

        // Exploit Protection
        [ObservableProperty] private string exploitProtectionText = "Chargement...";
        [ObservableProperty] private bool exploitProtectionEnabled;
        partial void OnExploitProtectionEnabledChanged(bool v)
        {
            if (_loading) return;
            _ = ToggleExploitProtectionAsync(v);
        }
        private async Task ToggleExploitProtectionAsync(bool enable)
        {
            IsAsrBusy = true;
            StatusText = enable ? "Activation Exploit Protection..." : "Désactivation Exploit Protection...";
            await Task.Run(() => _svc.SetExploitProtection(enable));
            var ep = await Task.Run(() => _svc.GetExploitProtection());
            _loading = true;
            ExploitProtectionEnabled = ep.DepEnabled && ep.AslrEnabled && ep.SeheEnabled;
            _loading = false;
            ExploitProtectionText = $"DEP:{(ep.DepEnabled ? "ON" : "OFF")}  ASLR:{(ep.AslrEnabled ? "ON" : "OFF")}  SEHOP:{(ep.SeheEnabled ? "ON" : "OFF")}";
            StatusText = enable ? "Exploit Protection activée." : "Exploit Protection désactivée.";
            IsAsrBusy = false;
        }

        public HardeningViewModel()
        {
            Sync();
            _ = LoadSignatureInfoAsync();
            _ = LoadAdvancedAsync();
        }

        private void Sync()
        {
            _loading = true;
            LlmnrActive      = _svc.IsLlmnrActive();
            Smb1Active       = _svc.IsSmb1Active();
            PuaActive        = _svc.IsPuaActive();
            MdnsActive       = _svc.IsMdnsActive();
            BonjourInstalled = _svc.IsBonjourInstalled();
            _loading = false;
        }

        private async Task LoadAdvancedAsync()
        {
            // BitLocker
            var drives = await Task.Run(() => _svc.GetBitLockerStatus());
            BitLockerDrives.Clear();
            foreach (var d in drives) BitLockerDrives.Add(d);
            BitLockerStatusText = drives.Count == 0
                ? "BitLocker non disponible ou aucun volume détecté."
                : $"{drives.Count} volume(s) détecté(s).";

            // ASR
            var rules = await Task.Run(() => _svc.GetAsrRules());
            AsrRules.Clear();
            foreach (var r in rules) AsrRules.Add(r);

            // Exploit Protection
            var ep = await Task.Run(() => _svc.GetExploitProtection());
            _loading = true;
            ExploitProtectionEnabled = ep.DepEnabled && ep.AslrEnabled && ep.SeheEnabled;
            _loading = false;
            ExploitProtectionText = ep.DepEnabled && ep.AslrEnabled && ep.SeheEnabled
                ? "DEP, ASLR et SEHOP actifs."
                : $"DEP:{(ep.DepEnabled ? "ON" : "OFF")}  ASLR:{(ep.AslrEnabled ? "ON" : "OFF")}  SEHOP:{(ep.SeheEnabled ? "ON" : "OFF")}";
        }

        [RelayCommand]
        private async Task EnableAllAsr()
        {
            IsAsrBusy = true;
            StatusText = "Activation de toutes les règles ASR...";
            await Task.Run(() => _svc.EnableAllAsrRules());
            await LoadAdvancedAsync();
            StatusText = "Toutes les règles ASR activées.";
            IsAsrBusy = false;
        }

        private async Task LoadSignatureInfoAsync()
        {
            var (ver, date) = await Task.Run(() => _svc.GetDefenderSignatureInfo());
            SignatureInfo = $"v{ver}  —  {date}";
        }

        partial void OnMdnsActiveChanged(bool v)
        {
            if (_loading) return;
            _svc.SetMdns(v);
            StatusText = v ? "mDNS reactivé" : "mDNS desactive - redemarrage recommande pour prendre effet";
        }

        [RelayCommand]
        private void DisableBonjour()
        {
            StatusText = _svc.DisableBonjour();
            BonjourInstalled = _svc.IsBonjourInstalled();
        }

        [RelayCommand]
        private async Task UpdateSignatures()
        {
            IsDefenderBusy = true;
            StatusText = "Mise à jour des signatures en cours...";
            StatusText = await Task.Run(() => _svc.UpdateDefenderSignatures());
            await LoadSignatureInfoAsync();
            IsDefenderBusy = false;
        }

        [RelayCommand]
        private async Task QuickScan()
        {
            IsDefenderBusy = true;
            StatusText = "Scan rapide en cours...";
            StatusText = await Task.Run(() => _svc.StartQuickScan());
            IsDefenderBusy = false;
        }

        // Les méthodes On...Changed sont générées par le toolkit : appelées
        // automatiquement quand la propriété change (donc quand l'utilisateur bascule le toggle).
        partial void OnLlmnrActiveChanged(bool value)
        {
            if (_loading) return;
            _svc.SetLlmnr(value);
            StatusText = value ? "LLMNR réactivé (défaut)" : "LLMNR désactivé (sécurisé)";
        }

        partial void OnSmb1ActiveChanged(bool value)
        {
            if (_loading) return;
            _svc.SetSmb1(value);
            StatusText = value ? "SMBv1 réactivé (reboot requis, déconseillé)" : "SMBv1 désactivé (reboot requis)";
        }

        partial void OnPuaActiveChanged(bool value)
        {
            if (_loading) return;
            bool ok = _svc.SetPua(value);
            StatusText = ok
                ? (value ? "Protection PUA activée" : "Protection PUA désactivée")
                : "⚠️ Échec : état PUA inchangé. Windows Defender est peut-être géré par une stratégie de groupe.";
            if (!ok) { _loading = true; PuaActive = !value; _loading = false; }
        }
    }
}