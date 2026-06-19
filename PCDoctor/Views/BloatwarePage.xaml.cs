using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using PCDoctor.ViewModels;

namespace PCDoctor.Views
{
    public sealed partial class BloatwarePage : Page
    {
        public BloatwarePage()
        {
            this.InitializeComponent();
            if (this.DataContext is BloatwareViewModel vm)
            {
                vm.ConfirmRequested        += ShowConfirmDialog;
                vm.OneDriveConfirmRequested += ShowOneDriveDialog;
                vm.EdgeConfirmRequested     += ShowEdgeDialog;
            }
        }

        private async Task<bool> ShowConfirmDialog(string message)
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

        private async Task<bool> ShowOneDriveDialog(string message)
        {
            var dialog = new ContentDialog
            {
                Title             = "Supprimer OneDrive",
                Content           = message,
                PrimaryButtonText = "Supprimer OneDrive",
                CloseButtonText   = "Annuler",
                DefaultButton     = ContentDialogButton.Close,
                XamlRoot          = this.XamlRoot
            };
            return (await dialog.ShowAsync()) == ContentDialogResult.Primary;
        }

        private async Task<bool> ShowEdgeDialog(string message)
        {
            var dialog = new ContentDialog
            {
                Title             = "Bloquer raccourcis Edge",
                Content           = message,
                PrimaryButtonText = "Appliquer",
                CloseButtonText   = "Annuler",
                DefaultButton     = ContentDialogButton.Close,
                XamlRoot          = this.XamlRoot
            };
            return (await dialog.ShowAsync()) == ContentDialogResult.Primary;
        }
    }
}