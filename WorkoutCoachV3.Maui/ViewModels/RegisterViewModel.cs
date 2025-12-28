using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using WorkoutCoachV2.Model.ApiContracts;
using WorkoutCoachV3.Maui.Pages;
using WorkoutCoachV3.Maui.Services;

namespace WorkoutCoachV3.Maui.ViewModels;

public partial class RegisterViewModel : ObservableObject
{
    // Auth API + token/session stores + DI voor navigatie (login/exercises).
    private readonly IAuthApi _authApi;
    private readonly ITokenStore _tokenStore;
    private readonly IUserSessionStore _sessionStore;
    private readonly IServiceProvider _services;

    // Registratie form fields + UI state.
    [ObservableProperty] private string displayName = "";
    [ObservableProperty] private string email = "";
    [ObservableProperty] private string password = "";
    [ObservableProperty] private string confirmPassword = "";
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? errorMessage;

    public RegisterViewModel(IAuthApi authApi, ITokenStore tokenStore, IUserSessionStore sessionStore, IServiceProvider services)
    {
        _authApi = authApi;
        _tokenStore = tokenStore;
        _sessionStore = sessionStore;
        _services = services;
    }

    [RelayCommand]
    private async Task RegisterAsync()
    {
        // Voorkom dubbele submit.
        if (IsBusy) return;

        ErrorMessage = null;

        // Trim input waar nodig (email + displayname).
        var mail = Email.Trim();
        var name = DisplayName.Trim();

        // Client-side checks voor betere UX (voor API call).
        if (string.IsNullOrWhiteSpace(name)) { ErrorMessage = "Display name is required."; return; }
        if (string.IsNullOrWhiteSpace(mail)) { ErrorMessage = "Email is required."; return; }
        if (string.IsNullOrWhiteSpace(Password)) { ErrorMessage = "Password is required."; return; }
        if (Password != ConfirmPassword) { ErrorMessage = "Passwords do not match."; return; }

        IsBusy = true;

        try
        {
            // Register request uitvoeren.
            var req = new RegisterRequest(mail, Password, name);
            var res = await _authApi.RegisterAsync(req);

            // Na registratie: direct inloggen door token/session op te slaan.
            await _tokenStore.SetAsync(res.Token, res.ExpiresUtc);
            await _sessionStore.SetAsync(res.UserId, res.Email, res.DisplayName, res.Roles);

            // Root reset naar startpagina.
            Application.Current!.MainPage = new NavigationPage(_services.GetRequiredService<ExercisesPage>());
        }
        catch (Exception ex)
        {
            // Toon server fouten (bv. email al in gebruik).
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task BackAsync()
    {
        // Terug navigeren zonder rare states tijdens een request.
        if (IsBusy) return;

        // Als er een vorige pagina is, pop; anders fallback naar Login.
        if (Application.Current?.MainPage?.Navigation?.NavigationStack?.Count > 1)
            await Application.Current!.MainPage!.Navigation.PopAsync();
        else
            Application.Current!.MainPage = new NavigationPage(_services.GetRequiredService<LoginPage>());
    }
}
