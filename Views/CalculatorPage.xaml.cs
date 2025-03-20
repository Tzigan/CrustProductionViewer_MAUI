using Microsoft.Maui.Controls;

namespace CrustProductionViewer_MAUI.Views
{
    public partial class CalculatorPage : ContentPage
    {
        public CalculatorPage()
        {
            InitializeComponent();
        }

        private async void OnBackToMainClicked(object sender, System.EventArgs e)
        {
            await Shell.Current.GoToAsync("//main");
        }
    }
}
