using WorkoutCoachV3.Maui.Pages;

namespace WorkoutCoachV3.Maui;

public partial class App : Application
{
    public App(LoginPage loginPage)
    {
        InitializeComponent();
        MainPage = new NavigationPage(loginPage);
    }
}
