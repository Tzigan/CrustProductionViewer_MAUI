using CrustProductionViewer_MAUI.Models;
using CrustProductionViewer_MAUI.PageModels;

namespace CrustProductionViewer_MAUI.Pages
{
    public partial class MainPage : ContentPage
    {
        public MainPage(MainPageModel model)
        {
            InitializeComponent();
            BindingContext = model;
        }
    }
}