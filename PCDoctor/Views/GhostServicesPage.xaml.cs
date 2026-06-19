using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using PCDoctor.ViewModels;

namespace PCDoctor.Views
{
    public sealed partial class GhostServicesPage : Page
    {
        public GhostServicesPage()
        {
            this.InitializeComponent();
            if (this.DataContext is GhostServicesViewModel vm)
                vm.ConfirmRequested += ShowConfirmDialog;
        }

        private async Task<bool> ShowConfirmDialog(string message)
        {
            var dialog = new ContentDialog
            {
                Title             = "Supprimer les services fantômes",
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
