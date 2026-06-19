using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using PCDoctor.ViewModels;

namespace PCDoctor.Views
{
    public sealed partial class PlanificateurPage : Page
    {
        public PlanificateurPage()
        {
            InitializeComponent();
            if (this.DataContext is PlanificateurViewModel vm)
                vm.ConfirmRequested += ShowConfirmDialog;
        }

        private async Task<bool> ShowConfirmDialog(string message)
        {
            var dialog = new ContentDialog
            {
                Title             = "Confirmer la suppression",
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
