using Microsoft.Extensions.DependencyInjection;
using WorkoutCoachV3.Maui.Pages;
using WorkoutCoachV3.Maui.Services;

namespace WorkoutCoachV3.Maui;

public partial class App : Application
{
    // DI container om services/pages op te halen tijdens startup (token, db, sync, etc).
    private readonly IServiceProvider _services;

    public App(IServiceProvider services)
    {
        InitializeComponent();
        _services = services;

        // Tijdelijke "loading" UI terwijl async init loopt (startup voelt smooth).
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

        // Fire-and-forget startup flow: beslist LoginPage of ExercisesPage.
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        // Services ophalen uit DI (zelfde als in MauiProgram geregistreerd).
        var tokenStore = _services.GetRequiredService<ITokenStore>();
        var sessionStore = _services.GetRequiredService<IUserSessionStore>();
        var localDb = _services.GetRequiredService<LocalDatabaseService>();
        var authApi = _services.GetRequiredService<IAuthApi>();

        // Connectivity watcher: triggert sync wanneer internet terug beschikbaar is.
        var connectivitySync = _services.GetRequiredService<ConnectivitySyncService>();
        connectivitySync.Start();

        // Local DB klaarzetten + eventueel seed data (best effort, geen crash).
        try { await localDb.EnsureCreatedAndSeedAsync(); } catch { }

        try
        {
            // Check of er een token bestaat die nog niet vervallen is.
            var hasValid = await tokenStore.HasValidTokenAsync();
            if (!hasValid)
            {
                // Geen token => start in login flow.
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    MainPage = new NavigationPage(_services.GetRequiredService<LoginPage>());
                });
                return;
            }

            try
            {
                // Token bestaat => haal "me" op om roles/display info te herstellen.
                var me = await authApi.MeAsync();

                // Null-safe mapping naar session store (UI gebruikt dit voor Admin knop, naam, etc).
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
                // Als "me" faalt (bv. API tijdelijk down), laten we de app toch starten.
            }

            // Best effort: meteen syncen bij startup zodat lokale data actueel is.
            try { await connectivitySync.TriggerSyncAsync(); } catch { }

            // Startpagina na succesvolle token check: Exercises tab.
            MainThread.BeginInvokeOnMainThread(() =>
            {
                MainPage = new NavigationPage(_services.GetRequiredService<ExercisesPage>());
            });
        }
        catch
        {
            // Bij onverwachte errors: token/session resetten en terug naar login.
            await tokenStore.ClearAsync();
            await sessionStore.ClearAsync();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                MainPage = new NavigationPage(_services.GetRequiredService<LoginPage>());
            });
        }
    }
}
