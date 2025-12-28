using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using WorkoutCoachV3.Maui.Data.LocalEntities;
using WorkoutCoachV3.Maui.Pages;
using WorkoutCoachV3.Maui.Services;

namespace WorkoutCoachV3.Maui.ViewModels;

public partial class WorkoutsViewModel : ObservableObject
{
    // Services: lokale DB + sync + DI voor pages + auth/session stores (logout/admin).
    private readonly LocalDatabaseService _local;
    private readonly ISyncService _sync;
    private readonly IServiceProvider _services;
    private readonly ITokenStore _tokenStore;
    private readonly IUserSessionStore _sessionStore;

    // Datasource voor WorkoutsPage (CollectionView).
    public ObservableCollection<LocalWorkout> Items { get; } = new();

    // UI state + search input + error label.
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private bool isRefreshing;
    [ObservableProperty] private string? error;
    [ObservableProperty] private string? searchText;

    // Admin knop zichtbaar/verborgen op basis van roles.
    [ObservableProperty] private bool canAccessAdmin;

    public WorkoutsViewModel(
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

        // Init admin flag.
        _ = LoadAdminFlagAsync();
    }

    private async Task LoadAdminFlagAsync()
    {
        // Admin/Moderator kunnen naar AdminPanelPage.
        CanAccessAdmin = await _sessionStore.IsInAnyRoleAsync("Admin", "Moderator");
    }

    public async Task RefreshAsyncCore()
    {
        // Refresh: admin flag updaten + local load + sync + nog eens local load.
        await LoadAdminFlagAsync();
        await LoadLocalAsync();

        try { await _sync.SyncAllAsync(); }
        catch { }

        await LoadLocalAsync();
    }

    [RelayCommand]
    public async Task LoadLocalAsync()
    {
        // Lokaal opnieuw laden (zonder verplicht te syncen).
        if (IsBusy) return;
        IsBusy = true;
        Error = null;

        try
        {
            var search = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim();
            var data = await _local.GetWorkoutsAsync(search);

            Items.Clear();
            foreach (var x in data)
                Items.Add(x);
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

    [RelayCommand]
    public async Task RefreshAsync()
    {
        // Pull-to-refresh flow.
        if (IsBusy) return;

        IsRefreshing = true;
        try
        {
            await RefreshAsyncCore();
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    public async Task RefreshAsyncCommand() => await RefreshAsync();

    [RelayCommand]
    private async Task AddAsync()
    {
        // Create flow: WorkoutEditPage openen met lege velden.
        var page = _services.GetRequiredService<WorkoutEditPage>();
        var vm = (WorkoutEditViewModel)page.BindingContext!;
        vm.InitForCreate();

        await Application.Current!.MainPage!.Navigation.PushAsync(page);
    }

    [RelayCommand]
    private async Task EditAsync(LocalWorkout? item)
    {
        // Edit flow: WorkoutEditPage openen met bestaande data.
        if (item is null) return;

        var page = _services.GetRequiredService<WorkoutEditPage>();
        var vm = (WorkoutEditViewModel)page.BindingContext!;
        await vm.InitForEditAsync(item.LocalId);

        await Application.Current!.MainPage!.Navigation.PushAsync(page);
    }

    [RelayCommand]
    private async Task DeleteAsync(LocalWorkout? item)
    {
        // Confirm + soft delete zodat sync dit kan doorgeven.
        if (item is null) return;

        var ok = await Application.Current!.MainPage!.DisplayAlert(
            "Delete workout",
            $"Delete '{item.Title}'?",
            "Delete",
            "Cancel");

        if (!ok) return;

        try
        {
            await _local.SoftDeleteWorkoutAsync(item.LocalId);
            try { await _sync.SyncAllAsync(); } catch { }
            await LoadLocalAsync();
        }
        catch (Exception ex)
        {
            Error = ex.Message;
        }
    }

    [RelayCommand]
    private async Task OpenDetailAsync(LocalWorkout? item)
    {
        // Detail flow: WorkoutDetailPage openen + VM init.
        if (item is null) return;

        var page = _services.GetRequiredService<WorkoutDetailPage>();
        var vm = (WorkoutDetailViewModel)page.BindingContext!;
        await vm.InitAsync(item.LocalId);

        await Application.Current!.MainPage!.Navigation.PushAsync(page);
    }

    [RelayCommand]
    private async Task GoExercisesAsync()
        // Navigatie tabs bovenaan.
        => await Application.Current!.MainPage!.Navigation.PushAsync(_services.GetRequiredService<ExercisesPage>());

    [RelayCommand]
    private async Task GoSessionsAsync()
        // Navigatie tabs bovenaan.
        => await Application.Current!.MainPage!.Navigation.PushAsync(_services.GetRequiredService<SessionsPage>());

    [RelayCommand]
    private async Task GoToStatsAsync()
        // Navigatie tabs bovenaan.
        => await Application.Current!.MainPage!.Navigation.PushAsync(_services.GetRequiredService<StatsPage>());

    [RelayCommand]
    private async Task GoToAdminAsync()
    {
        // Admin panel alleen openen als user de juiste role heeft.
        await LoadAdminFlagAsync();
        if (!CanAccessAdmin) return;

        var page = _services.GetRequiredService<AdminPanelPage>();
        await Application.Current!.MainPage!.Navigation.PushAsync(page);
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        // Logout: token + session wissen en terug naar login root.
        await _tokenStore.ClearAsync();
        await _sessionStore.ClearAsync();
        Application.Current!.MainPage = new NavigationPage(_services.GetRequiredService<LoginPage>());
    }
}
