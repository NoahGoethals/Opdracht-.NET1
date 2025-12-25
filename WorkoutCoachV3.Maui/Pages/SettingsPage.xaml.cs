using WorkoutCoachV3.Maui.ViewModels;

namespace WorkoutCoachV3.Maui.Pages;

public partial class SettingsPage : ContentPage
{
    public SettingsPage(SettingsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
