using CommunityToolkit.Maui;
using WorkoutCoachV3.Maui.Pages;
using WorkoutCoachV3.Maui.Services;
using WorkoutCoachV3.Maui.ViewModels;
using Microsoft.Extensions.DependencyInjection;


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

        var baseUrl = GetApiBaseUrl();

        builder.Services.AddHttpClient<IAuthApi, AuthApi>(http =>
        {
            http.BaseAddress = new Uri(baseUrl);
        })
#if DEBUG
        .ConfigurePrimaryHttpMessageHandler(() => DevHttpHandler())
#endif
        ;

        builder.Services.AddHttpClient("Api", http =>
        {
            http.BaseAddress = new Uri(baseUrl);
        })
        .AddHttpMessageHandler<AuthHeaderHandler>()
#if DEBUG
        .ConfigurePrimaryHttpMessageHandler(() => DevHttpHandler())
#endif
        ;

        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<LoginPage>();

        return builder.Build();
    }

    private static string GetApiBaseUrl()
    {
#if ANDROID
        return "https://10.0.2.2:7289/";
#else
        return "https://localhost:7289/";
#endif
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
