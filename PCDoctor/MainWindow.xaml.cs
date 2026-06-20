using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.IO;

namespace PCDoctor
{
    public sealed partial class MainWindow : Window
    {
        private static readonly string LastPageFile =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                         "PCDoctor", "lastpage.txt");

        public MainWindow()
        {
            this.InitializeComponent();

            // Icône dans la barre de titre
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var winId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
            var icoPath = Path.Combine(AppContext.BaseDirectory, "Assets", "pcdoctor.ico");
            Microsoft.UI.Windowing.AppWindow.GetFromWindowId(winId).SetIcon(icoPath);

            // Mica backdrop (effet transparence/profondeur Windows 11)
            if (MicaController.IsSupported())
                this.SystemBackdrop = new MicaBackdrop();

            // Badge score et navigation depuis les recommandations
            AppState.ScoreChanged      += UpdateScoreBadge;
            AppState.NavigateRequested += tag => DispatcherQueue.TryEnqueue(() =>
            {
                SelectItemByTag(tag);
                Navigate(tag);
            });
        }

        public void ApplyTheme(ElementTheme theme)
        {
            if (this.Content is FrameworkElement root)
                root.RequestedTheme = theme;
        }

        private void NavView_Loaded(object sender, RoutedEventArgs e)
        {
            NavView.SelectedItem = NavView.MenuItems[0];
        }

        private void NavView_SelectionChanged(NavigationView sender,
            NavigationViewSelectionChangedEventArgs args)
        {
            string pageTag;

            if (args.IsSettingsSelected)
            {
                pageTag = "AProposPage";
            }
            else
            {
                var item = args.SelectedItem as NavigationViewItem;
                if (item?.Tag == null) return;
                pageTag = item.Tag.ToString()!;
            }

            // Groupes sans page propre
            if (pageTag.StartsWith("_")) return;

            Navigate(pageTag);
            SaveLastPage(pageTag);
        }

        private void Navigate(string pageTag)
        {
            Type? page = pageTag switch
            {
                "AccueilPage"      => typeof(Views.AccueilPage),
                "ProfilesPage"     => typeof(Views.ProfilesPage),
                "AuditsPage"       => typeof(Views.AuditsPage),
                "HardeningPage"    => typeof(Views.HardeningPage),
                "PrivacyPage"      => typeof(Views.PrivacyPage),
                "NetworkPage"      => typeof(Views.NetworkPage),
                "OptimPage"        => typeof(Views.OptimPage),
                "GamingPage"       => typeof(Views.GamingPage),
                "ExplorerPage"     => typeof(Views.ExplorerPage),
                "StartupPage"      => typeof(Views.StartupPage),
                "NettoyagePage"    => typeof(Views.NettoyagePage),
                "BloatwarePage"    => typeof(Views.BloatwarePage),
                "AppsPage"         => typeof(Views.AppsPage),
                "GhostServicesPage"=> typeof(Views.GhostServicesPage),
                "EtatPage"         => typeof(Views.EtatPage),
                "UpdatesPage"      => typeof(Views.UpdatesPage),
                "DriversPage"      => typeof(Views.DriversPage),
                "PlanificateurPage"=> typeof(Views.PlanificateurPage),
                "WingetPage"       => typeof(Views.WingetPage),
                "SystemToolsPage"  => typeof(Views.SystemToolsPage),
                "AProposPage"      => typeof(Views.SettingsPage),
                _                  => null
            };

            if (page != null)
                ContentFrame.Navigate(page);
            else
                ContentFrame.Content = new TextBlock
                {
                    Text = "Page : " + pageTag,
                    FontSize = 24,
                    Margin = new Thickness(40)
                };
        }

        // ─── Badge score ──────────────────────────────────────────────────────
        private void UpdateScoreBadge(int ok, int total)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                NavAccueil.InfoBadge = new InfoBadge
                {
                    Value = ok < total ? total - ok : -1,  // -1 = pas de badge si tout est ok
                    Style = ok == total
                        ? (Style)Application.Current.Resources["SuccessIconInfoBadgeStyle"]
                        : (Style)Application.Current.Resources["AttentionValueInfoBadgeStyle"]
                };
            });
        }

        // ─── Last page ────────────────────────────────────────────────────────
        private void SelectItemByTag(string tag)
        {
            foreach (var item in NavView.MenuItems)
                if (FindAndSelect(item, tag)) return;
            NavView.SelectedItem = NavView.MenuItems[0];
        }

        private bool FindAndSelect(object obj, string tag)
        {
            if (obj is not NavigationViewItem item) return false;
            if (item.Tag?.ToString() == tag)
            {
                NavView.SelectedItem = item;
                return true;
            }
            foreach (var child in item.MenuItems)
                if (FindAndSelect(child, tag)) return true;
            return false;
        }

        private static string LoadLastPage()
        {
            try { return File.Exists(LastPageFile) ? File.ReadAllText(LastPageFile).Trim() : ""; }
            catch { return ""; }
        }

        private static void SaveLastPage(string tag)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LastPageFile)!);
                File.WriteAllText(LastPageFile, tag);
            }
            catch { }
        }
    }
}
