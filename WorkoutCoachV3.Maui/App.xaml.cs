using Microsoft.Extensions.DependencyInjection;
using WorkoutCoachV3.Maui.Pages;
using WorkoutCoachV3.Maui.Services;

namespace WorkoutCoachV3.Maui;

public partial class App : Application
{
    private readonly IServiceProvider _services;

    public App(
        LocalDatabaseService localDb,
        ITokenStore tokenStore,
        IUserSessionStore sessionStore,
        IAuthApi authApi,
        IServiceProvider services)
    {
        InitializeComponent();
        _services = services;

        MainPage = new ContentPage
        {
            Content = new Grid
            {
                Padding = 24,
                Children =
                {
                    new VerticalStackLayout
                    {
                        Spacing = 12,
                        VerticalOptions = LayoutOptions.Center,
                        HorizontalOptions = LayoutOptions.Center,
                        Children =
                        {
                            new Label
                            {
                                Text = "WorkoutCoach",
                                FontSize = 28,
                                FontAttributes = FontAttributes.Bold,
                                HorizontalTextAlignment = TextAlignment.Center
                            },
                            new ActivityIndicator { IsRunning = true }
                        }
                    }
                }
            }
        };

        _ = InitializeAsync(localDb, tokenStore, sessionStore, authApi);
    }

    private async Task InitializeAsync(
        LocalDatabaseService localDb,
        ITokenStore tokenStore,
        IUserSessionStore sessionStore,
        IAuthApi authApi)
    {
        try
        {
            await localDb.EnsureCreatedAndSeedAsync();

            var hasToken = await tokenStore.HasValidTokenAsync();

            if (hasToken && Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
            {
                try
                {
                    var me = await authApi.MeAsync();
                    await sessionStore.SetAsync(me.UserId, me.Email, me.DisplayName, me.Roles);
                }
                catch
                {
                    await tokenStore.ClearAsync();
                    await sessionStore.ClearAsync();
                    hasToken = false;
                }
            }

            Page root = hasToken
                ? _services.GetRequiredService<ExercisesPage>()
                : _services.GetRequiredService<LoginPage>();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                Application.Current!.MainPage = new NavigationPage(root);
            });
        }
        catch
        {
            var login = _services.GetRequiredService<LoginPage>();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                Application.Current!.MainPage = new NavigationPage(login);
            });
        }
    }
}
