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