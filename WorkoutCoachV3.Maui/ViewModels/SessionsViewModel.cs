using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using WorkoutCoachV3.Maui.Pages;
using WorkoutCoachV3.Maui.Services;

namespace WorkoutCoachV3.Maui.ViewModels;

public partial class SessionsViewModel : ObservableObject
{
    private readonly LocalDatabaseService _local;
    private readonly ISyncService _sync;
    private readonly IServiceProvider _services;
    private readonly ITokenStore _tokenStore;
    private readonly IUserSessionStore _sessionStore;

    public ObservableCollection<LocalDatabaseService.SessionListDisplay> Items { get; } = new();

    [ObservableProperty] private string? search;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private bool isRefreshing;
    [ObservableProperty] private string? error;

    // Admin button
    [ObservableProperty] private bool canAccessAdmin;

    public SessionsViewModel(
        LocalDatabaseService local,
        ISyncService sync,
        IServiceProvider services,
        ITokenStore tokenStore,
        IUserSessionStore sessionStore)
    {
        _local = local;
        _sync = sync;
        _services = services;
        _tokenStore = tokenStore;
        _sessionStore = sessionStore;

        _ = LoadAdminFlagAsync();
    }

    private async Task LoadAdminFlagAsync()
    {
        CanAccessAdmin = await _sessionStore.IsInAnyRoleAsync("Admin", "Moderator");
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        Error = null;

        try
        {
            var data = await _local.GetSessionsAsync(Search);
            Items.Clear();
            foreach (var s in data)
                Items.Add(s);
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

    public async Task RefreshAsyncCore()
    {
        await LoadAdminFlagAsync();

        try { await _sync.SyncAllAsync(); } catch { }

        var data = await _local.GetSessionsAsync(Search);
        Items.Clear();
        foreach (var s in data)
            Items.Add(s);
    }

    // ✅ Verb voor RefreshView: RefreshCommand
    [RelayCommand]
    public async Task RefreshAsync()
    {
        if (IsBusy) return;

        IsRefreshing = true;
        IsBusy = true;
        Error = null;

        try
        {
            await RefreshAsyncCore();
        }
        catch (Exception ex)
        {
            Error = ex.Message;
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        var page = _services.GetRequiredService<SessionEditPage>();
        var vm = (SessionEditViewModel)page.BindingContext!;
        await vm.InitForCreateAsync();

        await Application.Current!.MainPage!.Navigation.PushAsync(page);
    }

    [RelayCommand]
    private async Task EditAsync(Guid sessionLocalId)
    {
        var page = _services.GetRequiredService<SessionEditPage>();
        var vm = (SessionEditViewModel)page.BindingContext!;
        await vm.InitForEditAsync(sessionLocalId);

        await Application.Current!.MainPage!.Navigation.PushAsync(page);
    }

    [RelayCommand]
    private async Task DeleteAsync(Guid sessionLocalId)
    {
        try
        {
            await _local.SoftDeleteSessionAsync(sessionLocalId);
            try { await _sync.SyncAllAsync(); } catch { }
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            Error = ex.Message;
        }
    }

    [RelayCommand]
    private async Task OpenAsync(Guid sessionLocalId)
    {
        var page = _services.GetRequiredService<SessionDetailPage>();
        var vm = (SessionDetailViewModel)page.BindingContext!;
        await vm.InitAsync(sessionLocalId);

        await Application.Current!.MainPage!.Navigation.PushAsync(page);
    }

    [RelayCommand]
    private async Task GoExercisesAsync()
        => await Application.Current!.MainPage!.Navigation.PushAsync(_services.GetRequiredService<ExercisesPage>());

    [RelayCommand]
    private async Task GoWorkoutsAsync()
        => await Application.Current!.MainPage!.Navigation.PushAsync(_services.GetRequiredService<WorkoutsPage>());

    [RelayCommand]
    private async Task GoToStatsAsync()
        => await Application.Current!.MainPage!.Navigation.PushAsync(_services.GetRequiredService<StatsPage>());

    [RelayCommand]
    private async Task GoToAdminAsync()
    {
        await LoadAdminFlagAsync();
        if (!CanAccessAdmin) return;

        var page = _services.GetRequiredService<AdminPanelPage>();
        await Application.Current!.MainPage!.Navigation.PushAsync(page);
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        await _tokenStore.ClearAsync();
        await _sessionStore.ClearAsync();
        Application.Current!.MainPage = new NavigationPage(_services.GetRequiredService<LoginPage>());
    }
}
