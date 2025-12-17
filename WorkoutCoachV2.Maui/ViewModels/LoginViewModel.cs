using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WorkoutCoachV2.Maui.Services;

namespace WorkoutCoachV2.Maui.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IAuthApi _auth;
    private readonly ITokenStore _tokens;

    [ObservableProperty] private string email = "";
    [ObservableProperty] private string password = "";
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? error;

    public LoginViewModel(IAuthApi auth, ITokenStore tokens)
    {
        _auth = auth;
        _tokens = tokens;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        Error = null;

        try
        {
            var res = await _auth.LoginAsync(Email, Password);

            await _tokens.SetTokenAsync(res.Token, res.ExpiresUtc);

           
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
