using WorkoutCoachV2.Maui.ViewModels;

namespace WorkoutCoachV2.Maui.Pages;

public partial class LoginPage : ContentPage
{
    public LoginPage(LoginViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
