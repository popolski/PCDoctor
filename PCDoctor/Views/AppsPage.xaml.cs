using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using PCDoctor.ViewModels;

namespace PCDoctor.Views
{
    public sealed partial class AppsPage : Page
    {
        public AppsPage()
        {
            this.InitializeComponent();
            if (this.DataContext is AppsViewModel vm)
            {
                vm.ConfirmRequested        += ShowUninstallDialog;
                vm.ResidusConfirmRequested += ShowResidusDialog;
            }
        }

        private async Task<bool> ShowUninstallDialog(string message)
        {
            var dialog = new ContentDialog
            {
                Title             = "Confirmer la désinstallation",
                Content           = message,
                PrimaryButtonText = "Désinstaller",
                CloseButtonText   = "Annuler",
                DefaultButton     = ContentDialogButton.Close,
                XamlRoot          = this.XamlRoot
            };
            return (await dialog.ShowAsync()) == ContentDialogResult.Primary;
        }

        private async Task<bool> ShowResidusDialog(string message)
        {
            var dialog = new ContentDialog
            {
                Title             = "Confirmer la suppression des résidus",
                Content           = message,
                PrimaryButtonText = "Supprimer",
                CloseButtonText   = "Annuler",
                DefaultButton     = ContentDialogButton.Close,
                XamlRoot          = this.XamlRoot
            };
            return (await dialog.ShowAsync()) == ContentDialogResult.Primary;
        }
    }
}
