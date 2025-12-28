using CommunityToolkit.Maui;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkoutCoachV3.Maui.Data;
using WorkoutCoachV3.Maui.Pages;
using WorkoutCoachV3.Maui.Services;
using WorkoutCoachV3.Maui.ViewModels;

namespace WorkoutCoachV3.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        // Builder = centrale plek waar MAUI + DI + logging geconfigureerd wordt.
        var builder = MauiApp.CreateBuilder();

        builder
            // Koppelt de root Application class (App.xaml/App.xaml.cs).
            .UseMauiApp<App>()
            // CommunityToolkit: extra MVVM helpers, UI controls, behaviors, etc.
            .UseMauiCommunityToolkit();

        // Debug logging zichtbaar in Output window.
        builder.Logging.AddDebug();


        // Token store: bewaart JWT + expiry (bijv. in Preferences/SecureStorage, afhankelijk van implementatie).
        builder.Services.AddSingleton<ITokenStore, TokenStore>();
        // Session store: bewaart user context (id/email/roles) voor UI (Admin knop, display name, etc).
        builder.Services.AddSingleton<IUserSessionStore, UserSessionStore>();
        // HTTP handler die Authorization header toevoegt op requests voor "Api" client.
        builder.Services.AddTransient<AuthHeaderHandler>();


        // SQLite DB path in app data folder (platform-afhankelijk veilige opslag).
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "workoutcoach.local.db3");

        // DbContextFactory: maakt DbContext instances on-demand (veilig met DI + async).
        builder.Services.AddDbContextFactory<LocalAppDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}")
        );

        // Service wrapper rond EF Core: centraliseert lokale queries + upserts + soft deletes.
        builder.Services.AddSingleton<LocalDatabaseService>();

        // Sync service: push/pull tussen lokale DB en ASP.NET API.
        builder.Services.AddSingleton<ISyncService, SyncService>();

        // Service die connectivity events luistert en sync triggert wanneer internet terugkomt.
        builder.Services.AddSingleton<ConnectivitySyncService>();


        // HttpClient voor endpoints die géén auth vereisen (login/register).
        builder.Services.AddHttpClient("ApiNoAuth", (sp, http) =>
        {
            // Base URL komt uit ApiConfig (Preferences override of default Azure URL).
            var baseUrl = ApiConfig.GetBaseUrl();
            System.Diagnostics.Debug.WriteLine($"[API] ApiNoAuth BaseUrl = {baseUrl}");

            http.BaseAddress = new Uri(baseUrl);
            http.Timeout = TimeSpan.FromSeconds(30);
        })
#if DEBUG
        // DEBUG: accepteer self-signed certificates (handig voor local https).
        .ConfigurePrimaryHttpMessageHandler(() => DevHttpHandler())
#endif
        ;

        // HttpClient voor endpoints die auth nodig hebben (voegt Bearer token toe via AuthHeaderHandler).
        builder.Services.AddHttpClient("Api", (sp, http) =>
        {
            var baseUrl = ApiConfig.GetBaseUrl();
            System.Diagnostics.Debug.WriteLine($"[API] Api BaseUrl = {baseUrl}");

            http.BaseAddress = new Uri(baseUrl);
            http.Timeout = TimeSpan.FromSeconds(30);
        })
        // Pipeline: elke request krijgt Authorization header als er een token is.
        .AddHttpMessageHandler<AuthHeaderHandler>()
#if DEBUG
        .ConfigurePrimaryHttpMessageHandler(() => DevHttpHandler())
#endif
        ;

        // Kleine client voor "ping" / health check (snellere timeout).
        builder.Services.AddHttpClient(nameof(ApiHealthService), (sp, http) =>
        {
            var baseUrl = ApiConfig.GetBaseUrl();
            System.Diagnostics.Debug.WriteLine($"[API] ApiHealthService BaseUrl = {baseUrl}");

            http.BaseAddress = new Uri(baseUrl);
            http.Timeout = TimeSpan.FromSeconds(10);
        })
#if DEBUG
        .ConfigurePrimaryHttpMessageHandler(() => DevHttpHandler())
#endif
        ;

        // Auth API wrapper: login/register/me calls.
        builder.Services.AddTransient<IAuthApi, AuthApi>();
        // Health check wrapper: test of API bereikbaar is.
        builder.Services.AddSingleton<IApiHealthService, ApiHealthService>();

        // CRUD wrappers per domein (oefeningen/workouts/sessions/admin).
        builder.Services.AddTransient<IExercisesApi, ExercisesApi>();
        builder.Services.AddTransient<IWorkoutsApi, WorkoutsApi>();
        builder.Services.AddTransient<IWorkoutExercisesApi, WorkoutExercisesApi>();
        builder.Services.AddTransient<ISessionsApi, SessionsApi>();
        builder.Services.AddTransient<IAdminApi, AdminApi>();

        // ViewModels: transient zodat elke page een "fresh" VM krijgt bij navigatie.
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<RegisterViewModel>();

        builder.Services.AddTransient<ExercisesViewModel>();
        builder.Services.AddTransient<ExerciseEditViewModel>();

        builder.Services.AddTransient<WorkoutsViewModel>();
        builder.Services.AddTransient<WorkoutEditViewModel>();

        builder.Services.AddTransient<WorkoutDetailViewModel>();
        builder.Services.AddTransient<WorkoutExercisesManageViewModel>();

        builder.Services.AddTransient<SessionsViewModel>();
        builder.Services.AddTransient<SessionEditViewModel>();
        builder.Services.AddTransient<SessionDetailViewModel>();

        builder.Services.AddTransient<StatsViewModel>();
        builder.Services.AddTransient<AdminPanelViewModel>();
        builder.Services.AddTransient<SettingsViewModel>();

        // Pages: transient zodat navigation altijd een nieuwe instance krijgt.
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<RegisterPage>();

        builder.Services.AddTransient<ExercisesPage>();
        builder.Services.AddTransient<ExerciseEditPage>();

        builder.Services.AddTransient<WorkoutsPage>();
        builder.Services.AddTransient<WorkoutEditPage>();

        builder.Services.AddTransient<WorkoutDetailPage>();
        builder.Services.AddTransient<WorkoutExercisesManagePage>();

        builder.Services.AddTransient<SessionsPage>();
        builder.Services.AddTransient<SessionEditPage>();
        builder.Services.AddTransient<SessionDetailPage>();

        builder.Services.AddTransient<StatsPage>();
        builder.Services.AddTransient<AdminPanelPage>();
        builder.Services.AddTransient<SettingsPage>();

        // Build = maakt de app + DI container aan.
        return builder.Build();
    }

#if DEBUG
    private static HttpMessageHandler DevHttpHandler()
    {
        // Debug handler om local/self-signed SSL certs te accepteren (NIET gebruiken in release).
        return new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
    }
#endif
}
