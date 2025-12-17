using Microsoft.Extensions.DependencyInjection;
using WorkoutCoachV3.Maui.Pages;

namespace WorkoutCoachV3.Maui;

public partial class App : Application
{
    private readonly IServiceProvider _services;

    public App(IServiceProvider services)
    {
        InitializeComponent(); 
        _services = services;

        var loginPage = _services.GetRequiredService<LoginPage>();
        MainPage = new NavigationPage(loginPage);
    }
}
