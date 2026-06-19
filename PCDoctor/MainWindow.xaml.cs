using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace PCDoctor
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
        }

        // Applique un thème à toute la fenêtre (menu inclus)
        public void ApplyTheme(ElementTheme theme)
        {
            if (this.Content is FrameworkElement root)
                root.RequestedTheme = theme;
        }

        // Au chargement : sélectionner "Accueil" par défaut
        private void NavView_Loaded(object sender, RoutedEventArgs e)
        {
            NavView.SelectedItem = NavView.MenuItems[0];
        }

        // Quand l'utilisateur clique un élément du menu
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
                if (item == null || item.Tag == null) return;
                pageTag = item.Tag.ToString();
            }

            switch (pageTag)
            {
                case "ProfilesPage":
                    ContentFrame.Navigate(typeof(Views.ProfilesPage));
                    break;
                case "AccueilPage":
                    ContentFrame.Navigate(typeof(Views.AccueilPage));
                    break;
                case "AuditsPage":
                    ContentFrame.Navigate(typeof(Views.AuditsPage));
                    break;
                case "HardeningPage":
                    ContentFrame.Navigate(typeof(Views.HardeningPage));
                    break;
                case "PrivacyPage":
                    ContentFrame.Navigate(typeof(Views.PrivacyPage));
                    break;
                case "NettoyagePage":
                    ContentFrame.Navigate(typeof(Views.NettoyagePage));
                    break;
                case "NetworkPage":
                    ContentFrame.Navigate(typeof(Views.NetworkPage));
                    break;
                case "OptimPage":
                    ContentFrame.Navigate(typeof(Views.OptimPage));
                    break;
                case "StartupPage":
                    ContentFrame.Navigate(typeof(Views.StartupPage));
                    break;
                case "BloatwarePage":
                    ContentFrame.Navigate(typeof(Views.BloatwarePage));
                    break;
                case "AppsPage":
                    ContentFrame.Navigate(typeof(Views.AppsPage));
                    break;
                case "EtatPage":
                    ContentFrame.Navigate(typeof(Views.EtatPage));
                    break;
                case "GamingPage":
                    ContentFrame.Navigate(typeof(Views.GamingPage));
                    break;
                case "UpdatesPage":
                    ContentFrame.Navigate(typeof(Views.UpdatesPage));
                    break;
                case "DriversPage":
                    ContentFrame.Navigate(typeof(Views.DriversPage));
                    break;
                case "PlanificateurPage":
                    ContentFrame.Navigate(typeof(Views.PlanificateurPage));
                    break;
                case "WingetPage":
                    ContentFrame.Navigate(typeof(Views.WingetPage));
                    break;
                case "SystemToolsPage":
                    ContentFrame.Navigate(typeof(Views.SystemToolsPage));
                    break;
                case "ExplorerPage":
                    ContentFrame.Navigate(typeof(Views.ExplorerPage));
                    break;
                case "GhostServicesPage":
                    ContentFrame.Navigate(typeof(Views.GhostServicesPage));
                    break;
                case "AProposPage":
                    ContentFrame.Navigate(typeof(Views.SettingsPage));
                    break;
                // les autres pages viendront ici
                default:
                    ContentFrame.Content = new TextBlock
                    {
                        Text = "Page : " + pageTag,
                        FontSize = 24,
                        Margin = new Thickness(40)
                    };
                    break;
            }
        }
    }
}