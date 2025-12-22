using WorkoutCoachV3.Maui.ViewModels;

namespace WorkoutCoachV3.Maui.Pages;

public partial class RegisterPage : ContentPage
{
    public RegisterPage(RegisterViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
