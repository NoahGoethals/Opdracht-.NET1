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
        var tokenStore = _services.GetRequiredService<ITokenStore>();
        var sessionStore = _services.GetRequiredService<IUserSessionStore>();
        var localDb = _services.GetRequiredService<LocalDatabaseService>();
        var authApi = _services.GetRequiredService<IAuthApi>();

        try { await localDb.EnsureCreatedAndSeedAsync(); } catch { }

        try
        {
            var hasValid = await tokenStore.HasValidTokenAsync();
            if (!hasValid)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    MainPage = new NavigationPage(_services.GetRequiredService<LoginPage>());
                });
                return;
            }

            try
            {
                var me = await authApi.MeAsync();

                var userId = me.UserId ?? "";
                var email = me.Email ?? "";
                var displayName = !string.IsNullOrWhiteSpace(me.DisplayName)
                    ? me.DisplayName
                    : (!string.IsNullOrWhiteSpace(email) ? email : "User");

                var roles = me.Roles ?? Array.Empty<string>();

                await sessionStore.SetAsync(userId, email, displayName, roles);
            }
            catch
            {
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                MainPage = new NavigationPage(_services.GetRequiredService<ExercisesPage>());
            });
        }
        catch
        {
            await tokenStore.ClearAsync();
            await sessionStore.ClearAsync();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                MainPage = new NavigationPage(_services.GetRequiredService<LoginPage>());
            });
        }
    }
}
