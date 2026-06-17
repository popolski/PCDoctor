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
                vm.ConfirmRequested += ShowConfirmDialog;
        }

        private async Task<bool> ShowConfirmDialog(string message)
        {
            var dialog = new ContentDialog
            {
                Title = "Confirmer la désinstallation",
                Content = message,
                PrimaryButtonText = "Désinstaller",
                CloseButtonText = "Annuler",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };
            return (await dialog.ShowAsync()) == ContentDialogResult.Primary;
        }
    }
}