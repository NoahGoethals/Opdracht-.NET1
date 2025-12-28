// Code-behind: bindt RegisterViewModel zodat properties/commands werken in XAML.
using WorkoutCoachV3.Maui.ViewModels;

namespace WorkoutCoachV3.Maui.Pages;

public partial class RegisterPage : ContentPage
{
    // DI: ViewModel injecteren en instellen als BindingContext.
    public RegisterPage(RegisterViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
