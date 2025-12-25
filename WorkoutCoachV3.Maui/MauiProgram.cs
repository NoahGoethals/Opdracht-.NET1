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
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit();

        builder.Logging.AddDebug();

        builder.Services.AddSingleton<ITokenStore, TokenStore>();
        builder.Services.AddSingleton<IUserSessionStore, UserSessionStore>();
        builder.Services.AddTransient<AuthHeaderHandler>();

        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "workoutcoach.local.db3");

        builder.Services.AddDbContextFactory<LocalAppDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}")
        );

        builder.Services.AddSingleton<LocalDatabaseService>();
        builder.Services.AddSingleton<ISyncService, SyncService>();

        builder.Services.AddHttpClient("ApiNoAuth", (sp, http) =>
        {
            var baseUrl = ApiConfig.GetBaseUrl();
            System.Diagnostics.Debug.WriteLine($"[API] ApiNoAuth BaseUrl = {baseUrl}");
            http.BaseAddress = new Uri(baseUrl);
            http.Timeout = TimeSpan.FromSeconds(30);
        })
#if DEBUG
        .ConfigurePrimaryHttpMessageHandler(() => DevHttpHandler())
#endif
        ;

        builder.Services.AddHttpClient("Api", (sp, http) =>
        {
            var baseUrl = ApiConfig.GetBaseUrl();
            System.Diagnostics.Debug.WriteLine($"[API] Api BaseUrl = {baseUrl}");
            http.BaseAddress = new Uri(baseUrl);
            http.Timeout = TimeSpan.FromSeconds(30);
        })
        .AddHttpMessageHandler<AuthHeaderHandler>()
#if DEBUG
        .ConfigurePrimaryHttpMessageHandler(() => DevHttpHandler())
#endif
        ;

        builder.Services.AddHttpClient(nameof(ApiHealthService), (sp, http) =>
        {
            var baseUrl = ApiConfig.GetBaseUrl();
            http.BaseAddress = new Uri(baseUrl);
            http.Timeout = TimeSpan.FromSeconds(10);
        })
#if DEBUG
        .ConfigurePrimaryHttpMessageHandler(() => DevHttpHandler())
#endif
        ;

        builder.Services.AddTransient<IAuthApi, AuthApi>();
        builder.Services.AddSingleton<IApiHealthService, ApiHealthService>();

        builder.Services.AddTransient<IExercisesApi, ExercisesApi>();
        builder.Services.AddTransient<IWorkoutsApi, WorkoutsApi>();
        builder.Services.AddTransient<IWorkoutExercisesApi, WorkoutExercisesApi>();
        builder.Services.AddTransient<ISessionsApi, SessionsApi>();
        builder.Services.AddTransient<IAdminApi, AdminApi>();

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

        return builder.Build();
    }

#if DEBUG
    private static HttpMessageHandler DevHttpHandler()
    {
        return new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
    }
#endif
}
