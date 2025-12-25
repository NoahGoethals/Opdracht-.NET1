using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using WorkoutCoachV2.Model.ApiContracts;
using WorkoutCoachV3.Maui.Pages;
using WorkoutCoachV3.Maui.Services;

namespace WorkoutCoachV3.Maui.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IAuthApi _authApi;
    private readonly ITokenStore _tokenStore;
    private readonly IUserSessionStore _sessionStore;
    private readonly IServiceProvider _services;

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
        if (IsBusy) return;

        ErrorMessage = null;
        IsBusy = true;

        try
        {
            var req = new LoginRequest(Email.Trim(), Password);
            var res = await _authApi.LoginAsync(req);

            await _tokenStore.SetAsync(res.Token, res.ExpiresUtc);
            await _sessionStore.SetAsync(res.UserId, res.Email, res.DisplayName, res.Roles);

            Application.Current!.MainPage = new NavigationPage(_services.GetRequiredService<ExercisesPage>());
        }
        catch (Exception ex)
        {
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
        if (IsBusy) return;

        var page = _services.GetRequiredService<RegisterPage>();
        await Application.Current!.MainPage!.Navigation.PushAsync(page);
    }

    [RelayCommand]
    private async Task GoToSettingsAsync()
    {
        if (IsBusy) return;

        var page = _services.GetRequiredService<SettingsPage>();
        await Application.Current!.MainPage!.Navigation.PushAsync(page);
    }
}
