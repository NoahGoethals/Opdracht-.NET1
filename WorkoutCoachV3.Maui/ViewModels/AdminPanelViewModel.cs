using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using WorkoutCoachV3.Maui.Pages;
using WorkoutCoachV3.Maui.Services;

namespace WorkoutCoachV3.Maui.ViewModels;

public partial class AdminPanelViewModel : ObservableObject
{
    private readonly IAdminApi _adminApi;
    private readonly IUserSessionStore _sessionStore;
    private readonly ITokenStore _tokenStore;
    private readonly IServiceProvider _services;

    public ObservableCollection<string> RoleOptions { get; } =
        new(new[] { "User", "Moderator", "Admin" });

    public ObservableCollection<AdminUserRow> Users { get; } = new();

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? errorMessage;
    [ObservableProperty] private bool canAccessAdmin;

    public AdminPanelViewModel(
        IAdminApi adminApi,
        IUserSessionStore sessionStore,
        ITokenStore tokenStore,
        IServiceProvider services)
    {
        _adminApi = adminApi;
        _sessionStore = sessionStore;
        _tokenStore = tokenStore;
        _services = services;

        _ = LoadRoleFlagsAsync();
    }

    private async Task LoadRoleFlagsAsync()
    {
        CanAccessAdmin = await _sessionStore.IsInAnyRoleAsync("Admin", "Moderator");
    }

    public async Task LoadAsync()
    {
        if (IsBusy) return;

        ErrorMessage = null;
        IsBusy = true;

        try
        {
            if (!await _sessionStore.IsInAnyRoleAsync("Admin", "Moderator"))
            {
                ErrorMessage = "Not authorized.";
                await Application.Current!.MainPage!.Navigation.PopAsync();
                return;
            }

            var list = await _adminApi.GetUsersAsync();

            Users.Clear();
            foreach (var u in list)
            {
                var role = (u.Roles != null && u.Roles.Length > 0) ? u.Roles[0] : "User";
                Users.Add(new AdminUserRow(u.Id, u.Email, u.DisplayName, u.IsBlocked, role));
            }
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
    private async Task SaveRoleAsync(AdminUserRow? row)
    {
        if (row == null || IsBusy) return;

        ErrorMessage = null;
        IsBusy = true;

        try
        {
            await _adminApi.SetRoleAsync(row.Id, row.SelectedRole);
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
    private async Task ToggleBlockAsync(AdminUserRow? row)
    {
        if (row == null || IsBusy) return;

        ErrorMessage = null;
        IsBusy = true;

        try
        {
            await _adminApi.ToggleBlockAsync(row.Id);
            row.IsBlocked = !row.IsBlocked;
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
    private async Task GoToExercisesAsync()
        => await Application.Current!.MainPage!.Navigation.PushAsync(_services.GetRequiredService<ExercisesPage>());

    [RelayCommand]
    private async Task GoToWorkoutsAsync()
        => await Application.Current!.MainPage!.Navigation.PushAsync(_services.GetRequiredService<WorkoutsPage>());

    [RelayCommand]
    private async Task GoToSessionsAsync()
        => await Application.Current!.MainPage!.Navigation.PushAsync(_services.GetRequiredService<SessionsPage>());

    [RelayCommand]
    private async Task GoToStatsAsync()
        => await Application.Current!.MainPage!.Navigation.PushAsync(_services.GetRequiredService<StatsPage>());

    [RelayCommand]
    private async Task LogoutAsync()
    {
        await _tokenStore.ClearAsync();
        await _sessionStore.ClearAsync();
        Application.Current!.MainPage = new NavigationPage(_services.GetRequiredService<LoginPage>());
    }
}
