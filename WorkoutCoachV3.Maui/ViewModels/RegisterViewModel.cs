using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using WorkoutCoachV3.Maui.Pages;
using WorkoutCoachV3.Maui.Services;

namespace WorkoutCoachV3.Maui.ViewModels;

public partial class RegisterViewModel : ObservableObject
{
    private readonly IAuthApi _authApi;
    private readonly ITokenStore _tokenStore;
    private readonly IServiceProvider _services;

    [ObservableProperty] private string displayName = "";
    [ObservableProperty] private string email = "";
    [ObservableProperty] private string password = "";
    [ObservableProperty] private string confirmPassword = "";
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? errorMessage;

    public RegisterViewModel(IAuthApi authApi, ITokenStore tokenStore, IServiceProvider services)
    {
        _authApi = authApi;
        _tokenStore = tokenStore;
        _services = services;
    }

    [RelayCommand]
    private async Task RegisterAsync()
    {
        if (IsBusy) return;

        ErrorMessage = null;

        var mail = Email.Trim();
        var name = DisplayName.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            ErrorMessage = "Display name is required.";
            return;
        }

        if (string.IsNullOrWhiteSpace(mail))
        {
            ErrorMessage = "Email is required.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Password is required.";
            return;
        }

        if (Password != ConfirmPassword)
        {
            ErrorMessage = "Passwords do not match.";
            return;
        }

        IsBusy = true;

        try
        {
            var res = await _authApi.RegisterAsync(mail, Password, name);
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
    private async Task BackAsync()
    {
        if (IsBusy) return;

        if (Application.Current?.MainPage?.Navigation?.NavigationStack?.Count > 1)
            await Application.Current!.MainPage!.Navigation.PopAsync();
        else
            Application.Current!.MainPage = new NavigationPage(_services.GetRequiredService<LoginPage>());
    }
}
