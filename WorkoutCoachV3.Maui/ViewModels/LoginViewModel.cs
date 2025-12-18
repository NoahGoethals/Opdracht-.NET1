using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using WorkoutCoachV3.Maui.Pages;
using WorkoutCoachV3.Maui.Services;

namespace WorkoutCoachV3.Maui.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IAuthApi _auth;
    private readonly ITokenStore _tokens;
    private readonly IServiceProvider _services;

    [ObservableProperty] private string email = "admin@workoutcoach.local";
    [ObservableProperty] private string password = "Admin123!";
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? error;
    [ObservableProperty] private string? message;

    public LoginViewModel(IAuthApi auth, ITokenStore tokens, IServiceProvider services)
    {
        _auth = auth;
        _tokens = tokens;
        _services = services;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (IsBusy) return;

        IsBusy = true;
        Error = null;
        Message = null;

        try
        {
            var res = await _auth.LoginAsync(Email, Password);
            await _tokens.SetAsync(res.Token, res.ExpiresUtc);

            var exercisesPage = _services.GetRequiredService<ExercisesPage>();
            Application.Current!.MainPage = new NavigationPage(exercisesPage);
        }
        catch (Exception ex)
        {
            Error = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
