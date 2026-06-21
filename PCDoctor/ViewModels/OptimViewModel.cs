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

        partial void OnHibernationActiveChanged(bool value)
        {
            if (_loading) return;
            _svc.SetHibernation(value);
            StatusText = value ? "Hibernation activée" : "Hibernation désactivée (espace disque libéré)";
        }

        partial void OnFastStartupActiveChanged(bool value)
        {
            if (_loading) return;
            _svc.SetFastStartup(value);
            StatusText = value ? "Démarrage rapide activé" : "Démarrage rapide désactivé (arrêt complet à chaque extinction)";
        }

        partial void OnPowerThrottlingDisabledChanged(bool value)
        {
            if (_loading) return;
            _svc.SetPowerThrottling(value);
            StatusText = value ? "Power Throttling désactivé - performances maximales" : "Power Throttling activé - économie d'énergie";
        }

        partial void OnMemoryCompressionActiveChanged(bool value)
        {
            if (_loading) return;
            _svc.SetMemoryCompression(value);
            StatusText = value ? "Compression mémoire activée" : "Compression mémoire désactivée";
        }

        partial void OnVerboseStatusActiveChanged(bool value)
        {
            if (_loading) return;
            _svc.SetVerboseStatus(value);
            StatusText = value ? "Messages détaillés au démarrage activés" : "Messages détaillés désactivés (démarrage silencieux)";
        }

        partial void OnUtcClockActiveChanged(bool value)
        {
            if (_loading) return;
            _svc.SetUtcClock(value);
            StatusText = value ? "Horloge UTC activée - redémarrage requis (dual-boot Linux)" : "Horloge locale restaurée (heure locale en RTC)";
        }

        partial void OnSysMainActiveChanged(bool value)
        {
            if (_loading) return;
            bool ok = _svc.SetSysMain(value);
            StatusText = ok
                ? (value ? "SysMain (Superfetch) activé" : "SysMain désactivé (conseillé sur SSD)")
                : "⚠️ Échec : état SysMain inchangé. Vérifiez les droits administrateur.";
            if (!ok) { _loading = true; SysMainActive = !value; _loading = false; }
        }

        partial void OnSearchIndexActiveChanged(bool value)
        {
            if (_loading) return;
            bool ok = _svc.SetSearchIndex(value);
            StatusText = ok
                ? (value ? "Indexation Windows Search activée" : "Indexation désactivée (conseillé sur HDD lent)")
                : "⚠️ Échec : état Windows Search inchangé. Vérifiez les droits administrateur.";
            if (!ok) { _loading = true; SearchIndexActive = !value; _loading = false; }
        }

        partial void OnWerActiveChanged(bool value)
        {
            if (_loading) return;
            bool ok = _svc.SetWer(value);
            StatusText = ok
                ? (value ? "Windows Error Reporting activé" : "Windows Error Reporting désactivé")
                : "⚠️ Échec : état WER inchangé. Vérifiez les droits administrateur.";
            if (!ok) { _loading = true; WerActive = !value; _loading = false; }
        }
    }
}
