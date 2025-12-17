using WorkoutCoachV2.Maui.Pages;

namespace WorkoutCoachV2.Maui;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(nameof(LoginPage), typeof(LoginPage));
    }
}
