using Microsoft.Extensions.DependencyInjection;
using WorkoutCoachV3.Maui.Pages;
using WorkoutCoachV3.Maui.Services;

namespace WorkoutCoachV3.Maui;

public partial class App : Application
{
    private readonly IServiceProvider _services;

    public App(IServiceProvider services)
    {
        InitializeComponent();
        _services = services;

        MainPage = new ContentPage
        {
            Content = new Grid
            {
                Children =
                {
                    new ActivityIndicator
                    {
                        IsRunning = true,
                        VerticalOptions = LayoutOptions.Center,
                        HorizontalOptions = LayoutOptions.Center
                    }
                }
            }
        };

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            var tokens = _services.GetRequiredService<ITokenStore>();

            if (await tokens.HasValidTokenAsync())
            {
                var exercisesPage = _services.GetRequiredService<ExercisesPage>();
                MainPage = new NavigationPage(exercisesPage);
            }
            else
            {
                var loginPage = _services.GetRequiredService<LoginPage>();
                MainPage = new NavigationPage(loginPage);
            }
        }
        catch
        {
            var loginPage = _services.GetRequiredService<LoginPage>();
            MainPage = new NavigationPage(loginPage);
        }
    }
}
