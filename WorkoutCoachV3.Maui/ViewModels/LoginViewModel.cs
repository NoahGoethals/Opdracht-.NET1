using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using WorkoutCoachV2.Model.ApiContracts;
using WorkoutCoachV3.Maui.Pages;
using WorkoutCoachV3.Maui.Services;

namespace WorkoutCoachV3.Maui.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    // Auth API + opslag voor token en user session + DI voor navigatie naar pages.
    private readonly IAuthApi _authApi;
    private readonly ITokenStore _tokenStore;
    private readonly IUserSessionStore _sessionStore;
    private readonly IServiceProvider _services;

    // Form fields + UI state (busy + foutmelding).
    [ObservableProperty] private string email = "";
    [ObservableProperty] private string password = "";
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? errorMessage;

    public LoginViewModel(IAuthApi authApi, ITokenStore tokenStore, IUserSessionStore sessionStore, IServiceProvider services)
    {
        _authApi = authApi;
        _tokenStore = tokenStore;
        _sessionStore = sessionStore;
        _services = services;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        // Voorkom dubbele login requests (knop spam).
        if (IsBusy) return;

        ErrorMessage = null;
        IsBusy = true;

        try
        {
            // Login request bouwen en uitvoeren.
            var req = new LoginRequest(Email.Trim(), Password);
            var res = await _authApi.LoginAsync(req);

            // Token + user info bewaren zodat auth header later gezet kan worden.
            await _tokenStore.SetAsync(res.Token, res.ExpiresUtc);
            await _sessionStore.SetAsync(res.UserId, res.Email, res.DisplayName, res.Roles);

            // Na login: reset root naar de startpagina (Exercises).
            Application.Current!.MainPage = new NavigationPage(_services.GetRequiredService<ExercisesPage>());
        }
        catch (Exception ex)
        {
            // Toon API/validation errors in de UI.
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task GoToRegisterAsync()
    {
        // Navigatie blokkeren zolang er een request bezig is.
        if (IsBusy) return;

        var page = _services.GetRequiredService<RegisterPage>();
        await Application.Current!.MainPage!.Navigation.PushAsync(page);
    }

    [RelayCommand]
    private async Task GoToSettingsAsync()
    {
        // Navigatie naar API Settings om BaseUrl in te stellen.
        if (IsBusy) return;

        var page = _services.GetRequiredService<SettingsPage>();
        await Application.Current!.MainPage!.Navigation.PushAsync(page);
    }
}
