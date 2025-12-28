// Code-behind: eenvoudige pagina die enkel de LoginViewModel bindt.
using WorkoutCoachV3.Maui.ViewModels;

namespace WorkoutCoachV3.Maui.Pages;

public partial class LoginPage : ContentPage
{
    // DI: ViewModel injecteren zodat commands + properties via binding werken.
    public LoginPage(LoginViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
