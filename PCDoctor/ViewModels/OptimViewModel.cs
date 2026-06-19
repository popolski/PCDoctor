using CommunityToolkit.Mvvm.ComponentModel;
using PCDoctor.Services;

namespace PCDoctor.ViewModels
{
    public partial class OptimViewModel : ObservableObject
    {
        private readonly OptimService _svc = new();
        private bool _loading;

        [ObservableProperty] private bool hibernationActive;
        [ObservableProperty] private bool fastStartupActive;
        [ObservableProperty] private bool powerThrottlingDisabled;
        [ObservableProperty] private bool memoryCompressionActive;
        [ObservableProperty] private bool verboseStatusActive;
        [ObservableProperty] private bool utcClockActive;
        [ObservableProperty] private bool sysMainActive;
        [ObservableProperty] private bool searchIndexActive;
        [ObservableProperty] private bool werActive;
        [ObservableProperty] private string statusText = "";

        public OptimViewModel() { Sync(); }

        private void Sync()
        {
            _loading = true;
            HibernationActive       = _svc.IsHibernationActive();
            FastStartupActive       = _svc.IsFastStartupActive();
            PowerThrottlingDisabled = _svc.IsPowerThrottlingDisabled();
            MemoryCompressionActive = _svc.IsMemoryCompressionActive();
            VerboseStatusActive     = _svc.IsVerboseStatusActive();
            UtcClockActive          = _svc.IsUtcClockActive();
            SysMainActive           = _svc.IsSysMainActive();
            SearchIndexActive       = _svc.IsSearchIndexActive();
            WerActive               = _svc.IsWerActive();
            _loading = false;
        }

        partial void OnHibernationActiveChanged(bool v)
        {
            if (_loading) return;
            _svc.SetHibernation(v);
            StatusText = v ? "Hibernation activée" : "Hibernation désactivée (espace disque libéré)";
        }

        partial void OnFastStartupActiveChanged(bool v)
        {
            if (_loading) return;
            _svc.SetFastStartup(v);
            StatusText = v ? "Démarrage rapide activé" : "Démarrage rapide désactivé (arrêt complet à chaque extinction)";
        }

        partial void OnPowerThrottlingDisabledChanged(bool v)
        {
            if (_loading) return;
            _svc.SetPowerThrottling(v);
            StatusText = v ? "Power Throttling désactivé - performances maximales" : "Power Throttling activé - économie d'énergie";
        }

        partial void OnMemoryCompressionActiveChanged(bool v)
        {
            if (_loading) return;
            _svc.SetMemoryCompression(v);
            StatusText = v ? "Compression mémoire activée" : "Compression mémoire désactivée";
        }

        partial void OnVerboseStatusActiveChanged(bool v)
        {
            if (_loading) return;
            _svc.SetVerboseStatus(v);
            StatusText = v ? "Messages détaillés au démarrage activés" : "Messages détaillés désactivés (démarrage silencieux)";
        }

        partial void OnUtcClockActiveChanged(bool v)
        {
            if (_loading) return;
            _svc.SetUtcClock(v);
            StatusText = v ? "Horloge UTC activée - redémarrage requis (dual-boot Linux)" : "Horloge locale restaurée (heure locale en RTC)";
        }

        partial void OnSysMainActiveChanged(bool v)
        {
            if (_loading) return;
            _svc.SetSysMain(v);
            StatusText = v ? "SysMain (Superfetch) activé" : "SysMain désactivé (conseillé sur SSD)";
        }

        partial void OnSearchIndexActiveChanged(bool v)
        {
            if (_loading) return;
            _svc.SetSearchIndex(v);
            StatusText = v ? "Indexation Windows Search activée" : "Indexation désactivée (conseillé sur HDD lent)";
        }

        partial void OnWerActiveChanged(bool v)
        {
            if (_loading) return;
            _svc.SetWer(v);
            StatusText = v ? "Windows Error Reporting activé" : "Windows Error Reporting désactivé";
        }
    }
}
