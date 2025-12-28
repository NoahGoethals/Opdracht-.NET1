// Code-behind: bindt SettingsViewModel zodat BaseUrl + commands werken.
using WorkoutCoachV3.Maui.ViewModels;

namespace WorkoutCoachV3.Maui.Pages;

public partial class SettingsPage : ContentPage
{
    // DI: ViewModel injecteren en instellen als BindingContext.
    public SettingsPage(SettingsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
