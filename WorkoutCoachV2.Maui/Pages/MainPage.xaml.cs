using WorkoutCoachV2.Maui.Models;
using WorkoutCoachV2.Maui.PageModels;

namespace WorkoutCoachV2.Maui.Pages
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