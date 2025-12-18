using CommunityToolkit.Maui;
using Microsoft.EntityFrameworkCore;
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

        builder.Services.AddSingleton<ITokenStore, TokenStore>();
        builder.Services.AddTransient<AuthHeaderHandler>();

        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "workoutcoach.local.db3");

        builder.Services.AddDbContextFactory<LocalAppDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}")
        );

        builder.Services.AddSingleton<LocalDatabaseService>();
        builder.Services.AddSingleton<ISyncService, SyncService>();

        var baseUrl = ApiConfig.GetBaseUrl();
        System.Diagnostics.Debug.WriteLine($"[API] BaseUrl = {baseUrl}");

        builder.Services.AddHttpClient<IAuthApi, AuthApi>(http =>
        {
            http.BaseAddress = new Uri(baseUrl);
            http.Timeout = TimeSpan.FromSeconds(30);
        })
#if DEBUG
        .ConfigurePrimaryHttpMessageHandler(() => DevHttpHandler())
#endif
        ;

        builder.Services.AddHttpClient("Api", http =>
        {
            http.BaseAddress = new Uri(baseUrl);
            http.Timeout = TimeSpan.FromSeconds(30);
        })
        .AddHttpMessageHandler<AuthHeaderHandler>()
#if DEBUG
        .ConfigurePrimaryHttpMessageHandler(() => DevHttpHandler())
#endif
        ;

        builder.Services.AddSingleton<IExercisesApi, ExercisesApi>();

        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<ExercisesViewModel>();
        builder.Services.AddTransient<ExerciseEditViewModel>();

        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<ExercisesPage>();
        builder.Services.AddTransient<ExerciseEditPage>();

        return builder.Build();
    }

#if DEBUG
    private static HttpClientHandler DevHttpHandler()
    {
        return new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
    }
#endif
}
