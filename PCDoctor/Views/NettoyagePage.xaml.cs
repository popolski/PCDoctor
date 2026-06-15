using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using PCDoctor.ViewModels;

namespace PCDoctor.Views
{
    public sealed partial class NettoyagePage : Page
    {
        public NettoyagePage()
        {
            this.InitializeComponent();
            // Brancher l'événement de confirmation du ViewModel sur un ContentDialog
            if (this.DataContext is NettoyageViewModel vm)
            {
                vm.ConfirmRequested += ShowConfirmDialog;
            }
        }

        private async Task<bool> ShowConfirmDialog(string message)
        {
            var dialog = new ContentDialog
            {
                Title = "Confirmer le nettoyage",
                Content = message,
                PrimaryButtonText = "Nettoyer",
                CloseButtonText = "Annuler",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };
            var result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary;
        }
    }
}