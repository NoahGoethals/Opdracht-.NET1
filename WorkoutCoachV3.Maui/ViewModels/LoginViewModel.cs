using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using WorkoutCoachV3.Maui.Pages;
using WorkoutCoachV3.Maui.Services;

namespace WorkoutCoachV3.Maui.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IAuthApi _authApi;
    private readonly ITokenStore _tokenStore;
    private readonly IServiceProvider _services;

    [ObservableProperty] private string email = "";
    [ObservableProperty] private string password = "";
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? errorMessage;

    public LoginViewModel(IAuthApi authApi, ITokenStore tokenStore, IServiceProvider services)
    {
        _authApi = authApi;
        _tokenStore = tokenStore;
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
            var res = await _authApi.LoginAsync(Email.Trim(), Password);
            await _tokenStore.SetAsync(res.Token, res.ExpiresUtc);

            var exercisesPage = _services.GetRequiredService<ExercisesPage>();
            Application.Current!.MainPage = new NavigationPage(exercisesPage);
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
}
