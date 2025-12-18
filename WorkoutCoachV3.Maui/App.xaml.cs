using WorkoutCoachV3.Maui.Pages;
using WorkoutCoachV3.Maui.Services;

namespace WorkoutCoachV3.Maui;

public partial class App : Application
{
    public App(LocalDatabaseService localDb, LoginPage loginPage)
    {
        InitializeComponent();

        _ = Task.Run(async () => await localDb.EnsureCreatedAndSeedAsync());

        MainPage = new NavigationPage(loginPage);
    }
}
