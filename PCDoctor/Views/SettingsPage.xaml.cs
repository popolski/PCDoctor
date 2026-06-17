using System;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

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
            // Version
            VersionText.Text = "Version 3.0 — édition WinUI";

            // Statut admin
            var id = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(id);
            bool isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
            AdminStatus.Text = isAdmin
                ? "✅ L'application s'exécute avec les droits administrateur."
                : "⚠️ Droits administrateur absents : certaines actions système échoueront.";

            // Thème courant
            if (App.MainWindowRef?.Content is FrameworkElement root)
            {
                ThemeCombo.SelectedIndex = root.RequestedTheme switch
                {
                    ElementTheme.Light => 1,
                    ElementTheme.Dark => 2,
                    _ => 0
                };
            }
            _initialized = true;
        }

        private void ThemeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_initialized) return;
            if (App.MainWindowRef?.Content is FrameworkElement root)
            {
                root.RequestedTheme = ThemeCombo.SelectedIndex switch
                {
                    1 => ElementTheme.Light,
                    2 => ElementTheme.Dark,
                    _ => ElementTheme.Default
                };
            }
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
    }
}