using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PCDoctor.Services;

namespace PCDoctor.Views
{
    public sealed partial class SettingsPage : Page
    {
        private bool _initialized;

        public SettingsPage()
        {
            this.InitializeComponent();
            Loaded += SettingsPage_Loaded;
        }

        private void SettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            VersionText.Text = "Version 1.2 — édition WinUI";

            var id = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(id);
            bool isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
            AdminStatus.Text = isAdmin
                ? "✅ L'application s'exécute avec les droits administrateur."
                : "⚠️ Droits administrateur absents : certaines actions système échoueront.";

            if (App.MainWindowRef is MainWindow mw && mw.Content is FrameworkElement fe)
            {
                ThemeCombo.SelectedIndex = fe.RequestedTheme switch
                {
                    ElementTheme.Light => 1,
                    ElementTheme.Dark => 2,
                    _ => 0
                };
            }
            _initialized = true;
            RefreshHistory_Click(null!, null!);
        }

        private void ThemeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_initialized) return;
            var theme = ThemeCombo.SelectedIndex switch
            {
                1 => ElementTheme.Light,
                2 => ElementTheme.Dark,
                _ => ElementTheme.Default
            };
            if (App.MainWindowRef is MainWindow mw)
                mw.ApplyTheme(theme);
            SaveTheme(theme);
        }

        internal static void SaveTheme(ElementTheme theme)
        {
            try
            {
                var folder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PCDoctor");
                Directory.CreateDirectory(folder);
                File.WriteAllText(Path.Combine(folder, "theme.txt"), theme.ToString());
            }
            catch { }
        }

        internal static ElementTheme LoadTheme()
        {
            try
            {
                var path = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PCDoctor", "theme.txt");
                if (!File.Exists(path)) return ElementTheme.Default;
                return Enum.TryParse<ElementTheme>(File.ReadAllText(path).Trim(), out var t) ? t : ElementTheme.Default;
            }
            catch { return ElementTheme.Default; }
        }

        private void OpenLogs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string folder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PCDoctor");
                Directory.CreateDirectory(folder);
                Process.Start(new ProcessStartInfo("explorer.exe", folder) { UseShellExecute = true });
            }
            catch { }
        }

        // ─── Sauvegarde & Restauration ───────────────────────────────────────

        private async void ExportBackup_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var svc = new BackupService();
                string path = await Task.Run(() => svc.Export());
                ShowBackupStatus($"✅ Exporté sur le Bureau : {Path.GetFileName(path)}");
            }
            catch (Exception ex) { ShowBackupStatus($"❌ Erreur : {ex.Message}"); }
        }

        private async void ImportBackup_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var picker = new Windows.Storage.Pickers.FileOpenPicker();
                IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindowRef);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
                picker.FileTypeFilter.Add(".json");
                picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Desktop;
                var file = await picker.PickSingleFileAsync();
                if (file == null) return;

                ShowBackupStatus("⏳ Restauration en cours...");
                var svc = new BackupService();
                string msg = await Task.Run(() => svc.Import(file.Path));
                ShowBackupStatus($"✅ {msg}");
            }
            catch (Exception ex) { ShowBackupStatus($"❌ Erreur : {ex.Message}"); }
        }

        private void ShowBackupStatus(string msg)
        {
            BackupStatus.Text = msg;
            BackupStatus.Visibility = Visibility.Visible;
        }

        // ─── Historique des actions ───────────────────────────────────────────

        private void RefreshHistory_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string logPath = Logger.GetLogPath();
                if (!File.Exists(logPath))
                {
                    HistoryText.Text = "(Aucune action enregistrée aujourd'hui)";
                    return;
                }
                var lines = File.ReadLines(logPath)
                    .Where(l => l.Contains("[ACTION]"))
                    .TakeLast(30)
                    .ToList();
                HistoryText.Text = lines.Count == 0
                    ? "(Aucune action enregistrée aujourd'hui)"
                    : string.Join("\n", lines);
            }
            catch (Exception ex) { HistoryText.Text = $"Erreur : {ex.Message}"; }
        }
    }
}
